using CMS.Scheduler;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: RegisterScheduledTask("ETG.Admin.Scheduler.GetGoogleReviewsTask", typeof(ETG.Admin.Scheduler.GetGoogleReviewsTask))]

namespace ETG.Admin.Scheduler;

/// <summary>
/// Scheduled task wrapper that delegates Google Reviews refresh to the application service.
/// The task does not implement API logic itself.
/// </summary>
public class GetGoogleReviewsTask(IServiceProvider serviceProvider, ILogger<GetGoogleReviewsTask> logger) : IScheduledTask
{
    private readonly IServiceProvider serviceProvider = serviceProvider;
    private readonly ILogger<GetGoogleReviewsTask> logger = logger;

    public async Task<ScheduledTaskExecutionResult> Execute(ScheduledTaskConfigurationInfo task, CancellationToken cancellationToken)
    {
        try
        {
            var svcType = Type.GetType("ETG.Web.Services.GoogleReviews.IGoogleReviewsService, ETG.Web");

            if (svcType is null)
            {
                var msg = "GetGoogleReviewsTask: IGoogleReviewsService type not found. Ensure ETG.Web is loaded.";
                logger.LogError(msg);
                return new ScheduledTaskExecutionResult(msg);
            }

            using var scope = serviceProvider.CreateScope();
            var svc = scope.ServiceProvider.GetService(svcType);

            if (svc is null)
            {
                var msg = "GetGoogleReviewsTask: IGoogleReviewsService not registered in DI.";
                logger.LogWarning(msg);
                return new ScheduledTaskExecutionResult(msg);
            }

            // Use dynamic invocation to avoid compile-time dependency
            dynamic dyn = svc;
            bool ok = await dyn.RefreshFromApiAsync(cancellationToken);

            if (ok)
            {
                logger.LogInformation("GetGoogleReviewsTask: refresh completed successfully.");
                return ScheduledTaskExecutionResult.Success;
            }

            var msgFail = "GetGoogleReviewsTask: refresh did not complete. Check logs for details.";
            logger.LogWarning(msgFail);
            return new ScheduledTaskExecutionResult(msgFail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetGoogleReviewsTask failed.");
            return new ScheduledTaskExecutionResult($"Error: {ex.Message}");
        }
    }
}
