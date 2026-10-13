using ETG.Core.Content;
using ETG.Core.Models.Tour;
using Kentico.Content.Web.Mvc;
using System.Globalization;
using TourContent = ETG.Core.Content.Tour;

namespace ETG.Web.Services.Tour;

public sealed class TourSummaryService(
    IContentRetriever contentRetriever, ITourPricingService pricingService)
    : ITourSummaryService
{
    public async Task<TourSummaryViewModel> MapToViewModelAsync(
        TourContent tour,
        string path, TourPricingResult? priceInfo)
    {
        
        var heroImage = tour.TourHeroImage?
            .OfType<MediaFile>()
            .FirstOrDefault();

        var primaryCountryGuid =
            tour.TourPrimaryCountry?
                .FirstOrDefault()?
                .WebPageGuid;

        var primaryCountryName = string.Empty;

        if (primaryCountryGuid is Guid countryGuid &&
            countryGuid != Guid.Empty)
        {
            var countries =
                await contentRetriever
                    .RetrievePagesByGuids<Destination>(
                        [countryGuid]);

            var country = countries.FirstOrDefault();

            primaryCountryName =
                country?.DestinationName ?? string.Empty;
        }

        var badge =
            !string.IsNullOrWhiteSpace(primaryCountryName)
                ? primaryCountryName.ToUpperInvariant()
                : !string.IsNullOrWhiteSpace(
                    tour.TourDestinationCity)
                    ? tour.TourDestinationCity
                        .ToUpperInvariant()
                    : !string.IsNullOrWhiteSpace(
                        tour.TourDepartureCity)
                        ? tour.TourDepartureCity
                            .ToUpperInvariant()
                        : string.Empty;

        var daysLabel =
            tour.TourNights > 0
                ? $"{tour.TourNights + 1} DAYS FROM"
                : string.Empty;

        var price = priceInfo ?? await pricingService.GetPriceAsync(tour);

        var originalPriceText =
      price.OriginalPrice > 0
          ? price.OriginalPrice.ToString(
              "N0",
              CultureInfo.InvariantCulture)
          : string.Empty;

        var currentPriceText =
            price.CurrentPrice > 0
                ? price.CurrentPrice.ToString(
                    "N0",
                    CultureInfo.InvariantCulture)
                : string.Empty;

        return new TourSummaryViewModel
        {
            Name = tour.TourName ?? "",
            TourCode = tour.TourCode ?? "",
            Badge = badge,
            Path = path,
            Description = tour.TourSummary ?? "",
            HeroImageUrl =
                heroImage?.LegacyMediaFileAsset?.Url
                ?? "",
            HeroImageAlt =
                heroImage?.LegacyMediaFileTitle
                ?? "",
            DaysFromLabel = daysLabel,
            OriginalPrice = originalPriceText,
            CurrentPrice = string.IsNullOrEmpty(currentPriceText)
                            ? ""
                            : $"{price.CurrencySymbol}{currentPriceText}",
            Currency = price.CurrencyCode,
            PriceTypeLabel =
                tour.TourPriceTypeLabel ?? "",
            InclusionIcons =
                TourSummaryViewModel.ParseInclusions(
                    tour.TourPriceInclusions)
        };
    }
}