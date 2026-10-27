using System;
using System.Threading;
using System.Threading.Tasks;
using CMS.Core;
using Kentico.Xperience.Algolia.Indexing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ETG.Web.Features.Search.Services;

/// <summary>
/// Service responsible for executing Algolia search index rebuilds and updates.
/// </summary>
public class AlgoliaSyncService(
    IAlgoliaClient algoliaClient,
    IConfiguration configuration,
    IEventLogService eventLogService,
    ILogger<AlgoliaSyncService> logger) : IAlgoliaSyncService
{
    private const string EVENT_LOG_SOURCE = "AlgoliaSearch";

    /// <inheritdoc />
    public async Task<bool> RebuildTourIndexAsync(CancellationToken cancellationToken = default)
    {
        // Read configured Tour index name from appsettings.json (defaults to "TourPricing-Live")
        string indexName = configuration["CMSAlgoliaSearch:SearchIndexTour"] ?? "TourPricing-Live";

        try
        {
            logger.LogInformation("Starting Algolia Tour index rebuild for '{IndexName}'", indexName);
            eventLogService.LogInformation(
                EVENT_LOG_SOURCE,
                "REBUILD_START",
                $"Triggering Algolia index rebuild for '{indexName}'."
            );

            var index = AlgoliaIndexStore.Instance.GetIndex(indexName);
            if (index is null)
            {
                string errorMsg = $"Algolia index '{indexName}' was not found in AlgoliaIndexStore. Please verify that the index name exists in appsettings.json and is configured in the Kentico Admin Search application.";
                logger.LogError(errorMsg);
                eventLogService.LogError(EVENT_LOG_SOURCE, "INDEX_NOT_FOUND", errorMsg);
                return false;
            }

            await algoliaClient.Rebuild(index.IndexName, cancellationToken);

            logger.LogInformation("Algolia Tour index rebuild completed successfully for '{IndexName}'", index.IndexName);
            eventLogService.LogInformation(
                EVENT_LOG_SOURCE,
                "REBUILD_SUCCESS",
                $"Algolia Tour index rebuild completed successfully for '{index.IndexName}'."
            );

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to rebuild Algolia Tour index for '{IndexName}'", indexName);
            eventLogService.LogException(
                EVENT_LOG_SOURCE,
                "REBUILD_ERROR",
                ex,
                $"An error occurred while rebuilding Algolia Tour index '{indexName}'."
            );
            return false;
        }
    }
}
