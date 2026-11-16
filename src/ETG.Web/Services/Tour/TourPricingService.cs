using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CMS.DataEngine;
using CMS.Helpers;
using ETG.Booking.Pricing.Classes.Info;
using ETG.Web.Services.Currency;
using TourContent = ETG.Core.Content.Tour;
using ETGBooking;

namespace ETG.Web.Services.Tour;

/// <summary>
/// Final prices for a tour, already converted to the active currency.
/// </summary>
public record TourPricingResult(
    int CurrentPrice,
    int OriginalPrice,
    string CurrencyCode = "AUD",
    string CurrencySymbol = "$")
{
    public bool HasDiscount => CurrentPrice > 0 && CurrentPrice < OriginalPrice;
    // Automatically calculated discount amount
    public int DiscountAmount => HasDiscount ? OriginalPrice - CurrentPrice : 0;
}
public interface ITourPricingService
{
    Task<TourPricingResult> GetPriceAsync(TourContent tour);
    Task<Dictionary<string, TourPricingResult>> GetPricesBulkAsync(IEnumerable<TourContent> tours);
}

public class TourPricingService(
    IInfoProvider<BookingPriceInfo> bookingPriceProvider,
    IInfoProvider<PromotionInfo> promotionProvider,
    CurrentCurrencyPricing currencyPricing) : ITourPricingService
{
    /// <summary>
    /// Evaluates the starting ("From") price and active promotion for a single tour.
    /// </summary>
    public async Task<TourPricingResult> GetPriceAsync(TourContent tour)
    {
        if (tour == null || string.IsNullOrWhiteSpace(tour.TourCode))
            return Build(0, 0);

        int bookingPriceAud = await GetLowestBookingPriceAsync(tour.TourCode);
        int originalPriceAud = bookingPriceAud > 0 ? bookingPriceAud : GetFallbackCmsPrice(tour);

        if (originalPriceAud <= 0)
            return Build(0, 0);

        var promotions = await GetActivePromotionsAsync();
        var (currentPriceAud, _) = ApplyPromotion(tour, originalPriceAud, promotions);

        return Build(currentPriceAud, originalPriceAud);
    }
    

    /// <summary>
    /// Bulk fetcher for tour listings and widgets.
    /// Executes one query to the Booking Price module and one query to the Promotion module.
    /// </summary>
    public async Task<Dictionary<string, TourPricingResult>> GetPricesBulkAsync(IEnumerable<TourContent> tours)
    {
        var results = new Dictionary<string, TourPricingResult>(StringComparer.OrdinalIgnoreCase);

        var tourList = tours?
            .Where(t => t != null && !string.IsNullOrWhiteSpace(t.TourCode))
            .ToList();

        if (tourList == null || tourList.Count == 0)
            return results;

        var tourCodes = tourList.Select(t => t.TourCode).Distinct().ToList();

        // 1. Batch load Book Now lowest prices & active promotions
        var bulkBookingPrices = await GetLowestBookingPricesBulkAsync(tourCodes);
        var promotions = await GetActivePromotionsAsync();

        // 2. Evaluate pricing for each tour
        foreach (var tour in tourList)
        {
            int originalPriceAud =
                bulkBookingPrices.TryGetValue(tour.TourCode, out var bookingPriceAud) && bookingPriceAud > 0
                    ? bookingPriceAud
                    : GetFallbackCmsPrice(tour);

            if (originalPriceAud <= 0)
            {
                results[tour.TourCode] = Build(0, 0);
                continue;
            }

            var (currentPriceAud, _) = ApplyPromotion(tour, originalPriceAud, promotions);
            results[tour.TourCode] = Build(currentPriceAud, originalPriceAud);
        }

        return results;
    }

    /// <summary>
    /// Lowest active twin-share price (AUD) for one tour code.
    /// Matches BookingPriceRepository.GetLowestPriceFromBookNowPricing.
    /// </summary>
    private async Task<int> GetLowestBookingPriceAsync(string tourCode)
    {
        if (string.IsNullOrWhiteSpace(tourCode))
            return 0;

        var prices = await bookingPriceProvider.Get()
            .WhereEquals(nameof(BookingPriceInfo.BookingPriceTourCode), tourCode)
            .WhereEqualsOrNull(nameof(BookingPriceInfo.IsEnabled), true) // Allow true or NULL
            .WhereGreaterThan(nameof(BookingPriceInfo.BookingPriceEndDate), DateTime.Today)
            .WhereGreaterThan(nameof(BookingPriceInfo.BookingPriceTwinSharePrice), 0)
            .Column(nameof(BookingPriceInfo.BookingPriceTwinSharePrice))
            .GetEnumerableTypedResultAsync();

        var priceList = prices.ToList();

        return priceList.Count > 0
            ? (int)Math.Round(priceList.Min(x => x.BookingPriceTwinSharePrice))
            : 0;
    }

    /// <summary>
    /// Lowest active twin-share price (AUD) across multiple tour codes in one DB query.
    /// Matches BookingPriceRepository.GetLowestPricesFromBookNowPricing.
    /// </summary>
    private async Task<Dictionary<string, int>> GetLowestBookingPricesBulkAsync(IEnumerable<string> tourCodes)
    {
        var codesArray = tourCodes.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToArray();
        if (codesArray.Length == 0)
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        var rows = await bookingPriceProvider.Get()
            .Columns(
                nameof(BookingPriceInfo.BookingPriceTourCode),
                nameof(BookingPriceInfo.BookingPriceTwinSharePrice))
            .WhereIn(nameof(BookingPriceInfo.BookingPriceTourCode), codesArray)
            .WhereEqualsOrNull(nameof(BookingPriceInfo.IsEnabled), true) // Allow true or NULL
            .WhereGreaterThan(nameof(BookingPriceInfo.BookingPriceEndDate), DateTime.Today)
            .WhereGreaterThan(nameof(BookingPriceInfo.BookingPriceTwinSharePrice), 0)
            .GetEnumerableTypedResultAsync();

        return rows
            .Where(r => !string.IsNullOrEmpty(r.BookingPriceTourCode))
            .GroupBy(r => r.BookingPriceTourCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => (int)Math.Round(g.Min(r => r.BookingPriceTwinSharePrice)),
                StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Extracts fallback CMS price when Book Now pricing is absent.
    /// Checks OverridePrice first, then scans all 4 cabin classes across departure cities 1 to 4.
    /// Matches ModelFactory.CreatePricingModel and PricingModel.LowestPrice.
    /// </summary>
    private int GetFallbackCmsPrice(TourContent tour)
    {
        var allPricings = tour.SelectedTourPricingFolders?
            .Where(folder => folder.SelectedTourPricings != null)
            .SelectMany(folder => folder.SelectedTourPricings)
            .ToList();

        if (allPricings == null || allPricings.Count == 0)
            return 0;

        var validPrices = new List<int>();

        foreach (var pricing in allPricings)
        {
            if (pricing.TourPricingOverridePrice > 0)
            {
                validPrices.Add(pricing.TourPricingOverridePrice);
                continue;
            }

            var classPrices = new[]
            {
                pricing.TourPricingEconomyPrice1,
                pricing.TourPricingPremiumPrice1,
                pricing.TourPricingBusinessPrice1,
                pricing.TourPricingFirstClassPrice1,

                pricing.TourPricingEconomyPrice2,
                pricing.TourPricingPremiumPrice2,
                pricing.TourPricingBusinessPrice2,
                pricing.TourPricingFirstClassPrice2,

                pricing.TourPricingEconomyPrice3,
                pricing.TourPricingPremiumPrice3,
                pricing.TourPricingBusinessPrice3,
                pricing.TourPricingFirstClassPrice3,

                pricing.TourPricingEconomyPrice4,
                pricing.TourPricingPremiumPrice4,
                pricing.TourPricingBusinessPrice4,
                pricing.TourPricingFirstClassPrice4
            };

            var lowestCabin = classPrices.Where(p => p > 0).DefaultIfEmpty(0).Min();
            if (lowestCabin > 0)
                validPrices.Add(lowestCabin);
        }

        return validPrices.Count > 0 ? validPrices.Min() : 0;
    }

    // =========================================================================
    // 2. PROMOTIONS MODULE 
    // =========================================================================

    private sealed record ActivePromotion(
        HashSet<string> PackageCodes,
        HashSet<string> Destinations,
        double PercentDiscount,
        int DollarDiscount)
    {
        /// <summary>
        /// Matches by Tour/Package Code OR by Destination (from form fields).
        /// </summary>
        public bool Matches(string tourCode, string? destinationId)
        {
            if (!string.IsNullOrWhiteSpace(tourCode) && PackageCodes.Contains(tourCode))
                return true;

            if (!string.IsNullOrWhiteSpace(destinationId) && Destinations.Contains(destinationId))
                return true;

            return false;
        }

        /// <summary>
        /// Applies Kentico 13 discount priority:
        /// 1. Dollar discount takes precedence if set.
        /// 2. Percent discount uses Math.Round().
        /// </summary>
        public (int CurrentPriceAud, int DiscountAud) Calculate(int grossPriceAud)
        {
            if (DollarDiscount > 0)
            {
                int discounted = grossPriceAud - DollarDiscount;
                return (discounted > 0 ? discounted : grossPriceAud, DollarDiscount);
            }

            if (PercentDiscount > 0)
            {
                int discounted = (int)Math.Round(grossPriceAud * (1.0 - (PercentDiscount * 0.01)));
                int discount = grossPriceAud - discounted;
                return (discounted > 0 ? discounted : grossPriceAud, discount);
            }

            return (grossPriceAud, 0);
        }
    }

    /// <summary>
    /// Loads public promotions active today.
    /// Excludes promo-code gated and agent-specific (email domain) promotions.
    /// Matches PromotionRepository.GetAllPromotions.
    /// </summary>
    private async Task<List<ActivePromotion>> GetActivePromotionsAsync()
    {
        var today = DateTime.Today;

        var rows = await promotionProvider.Get()
            .WhereEmpty(nameof(PromotionInfo.PromotionPromoCode)) // Empty promo code for public catalog
            .WhereLessOrEquals(nameof(PromotionInfo.PromotionFromDate), today)
            .WhereGreaterOrEquals(nameof(PromotionInfo.PromotionToDate), today)
            .GetEnumerableTypedResultAsync();

        return rows
            .Where(p => IsBlank(p.PromotionEmailDomains)) // Exclude agency-specific email restrictions
            .Where(p => !IsBlank(p.PromotionPackageCodes) || !IsBlank(p.PromotionDestinations))
            .Select(p => new ActivePromotion(
                ParseCodes(p.PromotionPackageCodes),
                ParseCodes(p.PromotionDestinations),
                p.PromotionPercentDiscount,
                p.PromotionDollarDiscount))
            .Where(p => p.PercentDiscount > 0 || p.DollarDiscount > 0)
            .ToList();
    }

    /// <summary>
    ///  Rule: Finds the FIRST matching promotion for the tour.
    /// </summary>
    private (int CurrentPriceAud, int DiscountAud) ApplyPromotion(
        TourContent tour,
        int originalPriceAud,
        List<ActivePromotion> promotions)
    {
        //  Promotional discounts only apply to AUD and NZD currencies
        if (originalPriceAud <= 0 || !currencyPricing.CurrencyAppliesDiscounts)
            return (originalPriceAud, 0);

        string? destinationId = GetTourDestinationId(tour);

        // FirstOrDefault ensures exact parity with Kentico 13 TourService.GetTourPromotion
        var matchedPromo = promotions.FirstOrDefault(p => p.Matches(tour.TourCode, destinationId));
        if (matchedPromo == null)
            return (originalPriceAud, 0);

        return matchedPromo.Calculate(originalPriceAud);
    }

    /// <summary>
    /// Safely resolves the destination identifier (Country GUID or Code) from TourContent.
    /// </summary>
    private static string? GetTourDestinationId(TourContent tour)
    {
        // Adjust property name to match your TourContent destination field if different
        try
        {
            var prop = tour.GetType().GetProperty("TourPrimaryCountry")
                    ?? tour.GetType().GetProperty("PrimaryCountryGuid")
                    ?? tour.GetType().GetProperty("DestinationGuid");
            return prop?.GetValue(tour)?.ToString();
        }
        catch
        {
            return null;
        }
    }

    // =========================================================================
    // 3. CURRENCY CONVERSION & HELPERS
    // =========================================================================

    private TourPricingResult Build(int currentPriceAud, int originalPriceAud) =>
      new(
          ToCurrentCurrency(currentPriceAud),
          ToCurrentCurrency(originalPriceAud),
          currencyPricing.CurrentCurrency,
          currencyPricing.CurrentCurrencySymbol);

    private int ToCurrentCurrency(int audAmount) =>
        audAmount > 0 ? currencyPricing.ConvertAUDToCurrentCurrency(audAmount) : 0;

    private static HashSet<string> ParseCodes(string? codes)
    {
        if (string.IsNullOrWhiteSpace(codes))
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return codes.Split(new[] { ',', ';', '|', '\r', '\n', '\t', ' ' },
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) || value.Trim() == "-";
}