using ETG.Core.Models.GoogleReviews;

namespace ETG.Web.Services.GoogleReviews;

public interface IGoogleReviewsService
{
    /// <summary>
    /// Returns the latest deserialized review data, or <see langword="null"/> if no data has been fetched yet.
    /// </summary>
    Task<ReviewListingResponse?> GetReviewsAsync();

    /// <summary>
    /// Fetches reviews from the configured Google/Wextractor API and stores them into the reusable content item.
    /// Returns true when the operation completed successfully.
    /// </summary>
    Task<bool> RefreshFromApiAsync(CancellationToken cancellationToken = default);
}
