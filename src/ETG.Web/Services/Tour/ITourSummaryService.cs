using ETG.Core.Models.Tour;

using TourContent = ETG.Core.Content.Tour;

namespace ETG.Web.Services.Tour;

public interface ITourSummaryService
{
    Task<TourSummaryViewModel> MapToViewModelAsync(
        TourContent tour,
        string path, TourPricingResult? priceInfo);
}