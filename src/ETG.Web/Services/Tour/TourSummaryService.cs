using ETG.Core.Content;
using ETG.Core.Models.Tour;
using System.Globalization;
using TourContent = ETG.Core.Content.Tour;

namespace ETG.Web.Services.Tour;

public sealed class TourSummaryService(
    ITourPricingService pricingService)
    : ITourSummaryService
{
    public async Task<TourSummaryViewModel> MapToViewModelAsync(
        TourContent tour,
        string path,
        TourPricingResult? priceInfo,
        string? primaryCountryName = null)
    {
        var heroImage = tour.TourHeroImage?
            .OfType<MediaFile>()
            .FirstOrDefault();

        if (heroImage == null)
        {
            heroImage = tour.SelectedImageFolders?
                .SelectMany(folder =>
                    folder.SelectedImages ?? [])
                .SelectMany(image =>
                    image.ImageFilePath ?? [])
                .OfType<MediaFile>()
                .FirstOrDefault();
        }


        var resolvedPrimaryCountryName =
            primaryCountryName ?? string.Empty;

        var badge = !string.IsNullOrWhiteSpace(primaryCountryName)
        ? primaryCountryName.ToUpperInvariant()
        : string.Empty;

        var daysLabel =
            tour.TourNights > 0
                ? $"{tour.TourNights + 1} DAYS"
                : string.Empty;

 
        var price = priceInfo ??
            await pricingService.GetPriceAsync(tour);

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

        var displayOriginalPrice = string.Empty;

        if (price.CurrentPrice > 0 &&
            price.OriginalPrice > 0 &&
            price.CurrentPrice < price.OriginalPrice)
        {
            displayOriginalPrice = originalPriceText;
        }

        return new TourSummaryViewModel
        {
            Name = tour.TourName ?? string.Empty,

            TourCode = tour.TourCode ?? string.Empty,

            Badge = badge,

            Path = path,

            Description = tour.TourSummary ?? string.Empty,

            HeroImageUrl =
                heroImage?.LegacyMediaFileAsset?.Url
                ?? string.Empty,

            HeroImageAlt =
                heroImage?.LegacyMediaFileTitle
                ?? string.Empty,

            DaysFromLabel = daysLabel,

            OriginalPrice = displayOriginalPrice,

            CurrentPrice = currentPriceText,

            CurrencySymbol = price.CurrencySymbol,

            Currency = price.CurrencyCode,

            PriceTypeLabel =
                tour.TourPriceTypeLabel
                ?? string.Empty,

            InclusionIcons =
                TourSummaryViewModel.ParseInclusions(
                    tour.TourPriceInclusions)
        };
    }
}