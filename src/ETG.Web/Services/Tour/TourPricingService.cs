using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CMS.DataEngine;
using CMS.Helpers;
using ETG.Booking.Pricing.Classes.Info;
using ETG.Web.Services.Currency;
using TourContent = ETG.Core.Content.Tour;

namespace ETG.Web.Services.Tour;

/// <summary>
/// Final prices for a tour, already converted to the current currency.
/// </summary>
public record TourPricingResult(
    int CurrentPrice,
    int OriginalPrice,
    string CurrencyCode = "AUD",
    string CurrencySymbol = "$");

public interface ITourPricingService
{
    Task<TourPricingResult> GetPriceAsync(TourContent tour);
    Task<Dictionary<string, TourPricingResult>> GetPricesBulkAsync(IEnumerable<TourContent> tours);
}

public class TourPricingService(
    IInfoProvider<BookingPriceInfo> bookingPriceProvider,
    CurrentCurrencyPricing currencyPricing) : ITourPricingService
{
    /// <summary>
    /// Evaluates the price for a single tour.
    /// </summary>
    public async Task<TourPricingResult> GetPriceAsync(TourContent tour)
    {
        if (tour == null || string.IsNullOrWhiteSpace(tour.TourCode))
            return Build(0, 0);

        // 1. Fallback CMS price (AUD)
        int originalPriceAud = GetFallbackCmsPrice(tour);

        // 2. Active "Book Now" price from the booking module (AUD)
        int bookingPriceAud = await GetLowestBookingPriceAsync(tour.TourCode);

        // 3. Booking price wins if it exists; otherwise fallback
        int currentPriceAud = bookingPriceAud > 0 ? bookingPriceAud : originalPriceAud;

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

        foreach (var tour in tourList)
        {
            int originalPriceAud = GetFallbackCmsPrice(tour);

            int currentPriceAud =
                bulkBookingPrices.TryGetValue(tour.TourCode, out var bookingPriceAud) && bookingPriceAud > 0
                    ? bookingPriceAud
                    : originalPriceAud;

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

    /// <summary>
    /// Extracts the fallback CMS price (AUD) from the linked items in the Tour object.
    /// </summary>
    private int GetFallbackCmsPrice(TourContent tour)
    {
        var pricing = tour.SelectedTourPricingFolders?
            .Where(folder => folder.SelectedTourPricings != null)
            .SelectMany(folder => folder.SelectedTourPricings)
            .FirstOrDefault(); // You can change this to .OrderBy(x => x.TourPricingOverridePrice) if needed

        return pricing?.TourPricingOverridePrice ?? 0;
    }

    /// <summary>
    /// Lowest active twin-share price (AUD) for one tour code, or 0 if none.
    /// </summary>
    private async Task<int> GetLowestBookingPriceAsync(string tourCode)
    {
        var prices = await bookingPriceProvider.Get()
            .WhereEquals(nameof(BookingPriceInfo.BookingPriceTourCode), tourCode)
            .WhereEquals(nameof(BookingPriceInfo.IsEnabled), true)
            .WhereGreaterThan(nameof(BookingPriceInfo.BookingPriceEndDate), DateTime.Today)
            .WhereGreaterThan(nameof(BookingPriceInfo.BookingPriceTwinSharePrice), 0)
            .Column(nameof(BookingPriceInfo.BookingPriceTwinSharePrice))
            .GetEnumerableTypedResultAsync();

        var priceList = prices.ToList();

        return priceList.Count > 0
            ? (int)Math.Ceiling(priceList.Min(x => x.BookingPriceTwinSharePrice))
            : 0;
    }

    /// <summary>
    /// Lowest active twin-share price (AUD) per tour code, in one query.
    /// </summary>
    private async Task<Dictionary<string, int>> GetLowestBookingPricesBulkAsync(IEnumerable<string> tourCodes)
    {
        var codesArray = tourCodes.ToArray();

        var rows = await bookingPriceProvider.Get()
            .Columns(
                nameof(BookingPriceInfo.BookingPriceTourCode),
                nameof(BookingPriceInfo.BookingPriceTwinSharePrice))
            .WhereIn(nameof(BookingPriceInfo.BookingPriceTourCode), codesArray)
            .WhereEquals(nameof(BookingPriceInfo.IsEnabled), true)
            .WhereGreaterThan(nameof(BookingPriceInfo.BookingPriceEndDate), DateTime.Today)
            .WhereGreaterThan(nameof(BookingPriceInfo.BookingPriceTwinSharePrice), 0)
            .GetEnumerableTypedResultAsync();

        return rows
            .Where(r => !string.IsNullOrEmpty(r.BookingPriceTourCode))
            .GroupBy(r => r.BookingPriceTourCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => (int)Math.Ceiling(g.Min(r => r.BookingPriceTwinSharePrice)),
                StringComparer.OrdinalIgnoreCase);
    }
}