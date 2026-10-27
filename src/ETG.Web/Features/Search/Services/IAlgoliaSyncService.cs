using System.Threading;
using System.Threading.Tasks;

namespace ETG.Web.Features.Search.Services;

/// <summary>
/// Service contract for synchronizing and rebuilding Algolia search indexes.
/// </summary>
public interface IAlgoliaSyncService
{
    /// <summary>
    /// Triggers a full rebuild of the configured Tour Algolia search index.
    /// Refreshes dynamic pricing, active promotions, and cleans expired departure dates.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the rebuild completed successfully; otherwise false.</returns>
    Task<bool> RebuildTourIndexAsync(CancellationToken cancellationToken = default);
}
