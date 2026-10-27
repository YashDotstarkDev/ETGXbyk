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
    /// Final prices for a tour, already converted to the current currency.
    /// </summary>
    public record TourPricingResult(
        int CurrentPrice,
        int OriginalPrice,
        string CurrencyCode = "AUD",
        string CurrencySymbol = "$")
{
    public bool HasDiscount => CurrentPrice > 0 && CurrentPrice < OriginalPrice;

    // Discount amount in the selected currency, whole number
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
    /// Evaluates the price for a single tour.
    /// </summary>
    public async Task<TourPricingResult> GetPriceAsync(TourContent tour)
    {
        if (tour == null || string.IsNullOrWhiteSpace(tour.TourCode))
            return Build(0, 0);

        int cmsPriceAud = GetFallbackCmsPrice(tour);
        int bookingPriceAud = await GetLowestBookingPriceAsync(tour.TourCode);

        // Original = the fetched price (booking price, else CMS price)
        int originalPriceAud = bookingPriceAud > 0 ? bookingPriceAud : cmsPriceAud;

        // Current = original minus the best active promotion
        var promotions = await GetActivePromotionsAsync();
        int currentPriceAud = DiscountedPriceAud(tour.TourCode, originalPriceAud, promotions);

        return Build(currentPriceAud, originalPriceAud);
    }

    /// <summary>
    /// Bulk fetcher for tiled tours (e.g., ProductsWidget).
    /// Makes ONE database call to the booking module regardless of how many tours exist.
    /// </summary>
    public async Task<Dictionary<string, TourPricingResult>> GetPricesBulkAsync(IEnumerable<TourContent> tours)
        {
            var results = new Dictionary<string, TourPricingResult>(StringComparer.OrdinalIgnoreCase);

            var tourList = tours
                .Where(t => t != null && !string.IsNullOrWhiteSpace(t.TourCode))
                .ToList();

            if (!tourList.Any()) return results;

            var tourCodes = tourList.Select(t => t.TourCode).Distinct().ToList();

            var bulkBookingPrices = await GetLowestBookingPricesBulkAsync(tourCodes);
            var promotions = await GetActivePromotionsAsync();

        foreach (var tour in tourList)
            {
            int cmsPriceAud = GetFallbackCmsPrice(tour);

            int originalPriceAud =
                bulkBookingPrices.TryGetValue(tour.TourCode, out var bookingPriceAud) && bookingPriceAud > 0
                    ? bookingPriceAud
                    : cmsPriceAud;

            int currentPriceAud = DiscountedPriceAud(tour.TourCode, originalPriceAud, promotions);

            results[tour.TourCode] = Build(currentPriceAud, originalPriceAud);
        }

            return results;
        }

        // ==========================================
        // PRIVATE HELPERS
        // ==========================================

        /// <summary>
        /// Converts AUD amounts to the current currency and attaches its code and symbol.
        /// </summary>
        private TourPricingResult Build(int currentPriceAud, int originalPriceAud) =>
            new(
                ToCurrentCurrency(currentPriceAud),
                ToCurrentCurrency(originalPriceAud),
                currencyPricing.CurrentCurrency,
                currencyPricing.CurrentCurrencySymbol);

        private int ToCurrentCurrency(int audAmount) =>
            audAmount > 0 ? currencyPricing.ConvertAUDToCurrentCurrency(audAmount) : 0;
    // ==========================================
    // UPDATED PRICE FETCHING METHODS
    // ==========================================

    /// <summary>
    /// Extracts the fallback CMS price (AUD) from the linked items in the Tour object.
    /// Matches ModelFactory.CreatePricingModel and PricingModel.LowestPrice in ETG.Data.
    /// </summary>
    private int GetFallbackCmsPrice(TourContent tour)
    {
        var allPricings = tour.SelectedTourPricingFolders?
            .Where(folder => folder.SelectedTourPricings != null)
            .SelectMany(folder => folder.SelectedTourPricings)
            .ToList();

        if (allPricings == null || allPricings.Count == 0)
        {
            return 0;
        }

        var validPrices = new List<int>();

        foreach (var pricing in allPricings)
        {
            // 1. If override price exists, it takes precedence
            if (pricing.TourPricingOverridePrice > 0)
            {
                validPrices.Add(pricing.TourPricingOverridePrice);
                continue;
            }

            // 2. Otherwise extract all cabin classes across departure cities 1 to 4
            // (Mirrors ModelFactory.CreatePricingModel)
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

            var lowestCabinPrice = classPrices.Where(p => p > 0).DefaultIfEmpty(0).Min();
            if (lowestCabinPrice > 0)
            {
                validPrices.Add(lowestCabinPrice);
            }
        }

        // Return the lowest price among all available pricing periods
        return validPrices.Count > 0 ? validPrices.Min() : 0;
    }

    /// <summary>
    /// Lowest active twin-share price (AUD) for one tour code, or 0 if none.
    /// Matches BookingPriceRepository.GetLowestPriceFromBookNowPricing.
    /// </summary>
    private async Task<int> GetLowestBookingPriceAsync(string tourCode)
    {
        if (string.IsNullOrWhiteSpace(tourCode))
        {
            return 0;
        }

        var prices = await bookingPriceProvider.Get()
            .WhereEquals(nameof(BookingPriceInfo.BookingPriceTourCode), tourCode)
            .WhereEqualsOrNull(nameof(BookingPriceInfo.IsEnabled), true) // Allow NULL or true
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
    /// Lowest active twin-share price (AUD) per tour code, in one query.
    /// Matches BookingPriceRepository.GetLowestPricesFromBookNowPricing.
    /// </summary>
    private async Task<Dictionary<string, int>> GetLowestBookingPricesBulkAsync(IEnumerable<string> tourCodes)
    {
        var codesArray = tourCodes.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToArray();
        if (codesArray.Length == 0)
        {
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        var rows = await bookingPriceProvider.Get()
            .Columns(
                nameof(BookingPriceInfo.BookingPriceTourCode),
                nameof(BookingPriceInfo.BookingPriceTwinSharePrice))
            .WhereIn(nameof(BookingPriceInfo.BookingPriceTourCode), codesArray)
            .WhereEqualsOrNull(nameof(BookingPriceInfo.IsEnabled), true) // Allow NULL or true
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

    private sealed record ActivePromotion(HashSet<string> PackageCodes, double PercentDiscount, int DollarDiscount);

    /// <summary>
    /// Promotions that are active today and apply to specific package codes.
    /// </summary>
    private async Task<List<ActivePromotion>> GetActivePromotionsAsync()
    {
        var today = DateTime.Today;

        // Coarse DB filter (a day of slack on each side), exact date check below
        var rows = await promotionProvider.Get()
            .WhereLessThan(nameof(PromotionInfo.PromotionFromDate), today.AddDays(2))
            .WhereGreaterThan(nameof(PromotionInfo.PromotionToDate), today.AddDays(-2))
            .GetEnumerableTypedResultAsync();

        return rows
            .Where(p => p.PromotionFromDate.Date <= today && today <= p.PromotionToDate.Date)
            .Where(p => IsBlank(p.PromotionPromoCode)
                     && IsBlank(p.PromotionEmailDomains)
                     && !IsBlank(p.PromotionPackageCodes))
            .Select(p => new ActivePromotion(
                ParseCodes(p.PromotionPackageCodes),
                p.PromotionPercentDiscount,
                p.PromotionDollarDiscount))
            .Where(p => p.PercentDiscount > 0 || p.DollarDiscount > 0)
            .ToList();
    }

    private static HashSet<string> ParseCodes(string codes) =>
        codes.Split(new[] { ',', ';', '|', '\r', '\n', '\t', ' ' },
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static bool IsBlank(string? value) =>
    string.IsNullOrWhiteSpace(value) || value.Trim() == "-";

    private int DiscountedPriceAud(string tourCode, int originalPriceAud, List<ActivePromotion> promotions)
    {
        if (!currencyPricing.CurrencyAppliesDiscounts)
            return originalPriceAud;

        return originalPriceAud - GetBestDiscountAud(tourCode, originalPriceAud, promotions);
    }
    /// <summary>
    /// Best discount amount (whole AUD) for a tour. Percent discounts are converted
    /// to a dollar amount on the original price. Returns 0 if no promotion applies.
    /// </summary>
    private int GetBestDiscountAud(string tourCode, int originalPriceAud, List<ActivePromotion> promotions)
    {
        if (originalPriceAud <= 0)
            return 0;

        int bestDiscount = 0;

        foreach (var promo in promotions.Where(p => p.PackageCodes.Contains(tourCode)))
        {
            // Percent -> dollar amount (rounded down, so the customer never gets more than the real discount)
            int percentAmount = promo.PercentDiscount > 0
                ? (int)Math.Floor(originalPriceAud * promo.PercentDiscount / 100d)
                : 0;

            int dollarAmount = promo.DollarDiscount > 0 ? promo.DollarDiscount : 0;

            // If one promotion has both, use the larger amount
            int promoDiscount = Math.Max(percentAmount, dollarAmount);

            if (promoDiscount > bestDiscount)
                bestDiscount = promoDiscount;
        }

        // A discount can never reduce the price to zero or below
        return bestDiscount >= originalPriceAud ? 0 : bestDiscount;
    }

}