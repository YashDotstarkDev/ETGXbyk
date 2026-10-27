using CMS.Scheduler;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: RegisterScheduledTask("ETG.Admin.Scheduler.SearchTaskExecutor", typeof(ETG.Admin.Scheduler.SearchTaskExecutor))]

namespace ETG.Admin.Scheduler;

/// <summary>
/// Scheduled task that executes Algolia search index tasks (TourPricing-Live rebuild).
/// Matches the Kentico 13 naming convention 'Search.Algolia.TaskExecutor' / 'SearchTaskExecutor'.
/// Refreshes dynamic tour pricing, applies active promotions, and clears expired departure dates.
/// </summary>
public class SearchTaskExecutor(IServiceProvider serviceProvider, ILogger<SearchTaskExecutor> logger) : IScheduledTask
{
    private readonly IServiceProvider serviceProvider = serviceProvider;
    private readonly ILogger<SearchTaskExecutor> logger = logger;

    public async Task<ScheduledTaskExecutionResult> Execute(ScheduledTaskConfigurationInfo task, CancellationToken cancellationToken)
    {
        try
        {
            var svcType = Type.GetType("ETG.Web.Features.Search.Services.IAlgoliaSyncService, ETG.Web");

            if (svcType is null)
            {
                var msg = "SearchTaskExecutor: IAlgoliaSyncService type not found. Ensure ETG.Web is loaded.";
                logger.LogError(msg);
                return new ScheduledTaskExecutionResult(msg);
            }

            using var scope = serviceProvider.CreateScope();
            var svc = scope.ServiceProvider.GetService(svcType);

            if (svc is null)
            {
                var msg = "SearchTaskExecutor: IAlgoliaSyncService is not registered in DI.";
                logger.LogWarning(msg);
                return new ScheduledTaskExecutionResult(msg);
            }

            // Use dynamic invocation to avoid compile-time dependency from ETG.Admin to ETG.Web
            dynamic dyn = svc;
            bool ok = await dyn.RebuildTourIndexAsync(cancellationToken);

            if (ok)
            {
                logger.LogInformation("SearchTaskExecutor: Algolia search task execution completed successfully.");
                return ScheduledTaskExecutionResult.Success;
            }

            var msgFail = "SearchTaskExecutor: Tour index rebuild did not complete successfully. Check Kentico Event Log for details.";
            logger.LogWarning(msgFail);
            return new ScheduledTaskExecutionResult(msgFail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SearchTaskExecutor failed with exception.");
            return new ScheduledTaskExecutionResult($"Error: {ex.Message}");
        }
    }
}
