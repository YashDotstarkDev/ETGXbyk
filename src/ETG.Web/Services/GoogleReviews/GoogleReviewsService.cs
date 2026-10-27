using CMS.ContentEngine;
using CMS.ContentEngine.Internal;
using CMS.DataEngine;
using CMS.Helpers;
using CMS.Membership;
using ETG.Core.Content;
using ETG.Core.Models.GoogleReviews;
using ETG.Web.Infrastructure;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace ETG.Web.Services.GoogleReviews;

public class GoogleReviewsService : IGoogleReviewsService
{
    private readonly IContentQueryExecutor executor;
    private readonly IProgressiveCache cache;
    private readonly ILogger<GoogleReviewsService> logger;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly IContentItemManagerFactory contentItemManagerFactory;
    private readonly IMediator mediator;
    private readonly IInfoProvider<UserInfo> userInfoProvider;

    public GoogleReviewsService(
        IContentQueryExecutor executor,
        IProgressiveCache cache,
        ILogger<GoogleReviewsService> logger,
        IHttpClientFactory httpClientFactory,
        IInfoProvider<UserInfo> userInfoProvider,
        IContentItemManagerFactory contentItemManagerFactory,
        IMediator mediator)
    {
        this.executor = executor;
        this.cache = cache;
        this.logger = logger;
        this.httpClientFactory = httpClientFactory;
        this.contentItemManagerFactory = contentItemManagerFactory;
        this.userInfoProvider = userInfoProvider;
        this.mediator = mediator;
    }
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const string ItemName = "GoogleReviewsCache";
    private const string ItemDisplayName = "Google Reviews Cache";

    public Task<ReviewListingResponse?> GetReviewsAsync() =>
        cache.LoadAsync(
            cs =>
            {
                cs.GetCacheDependency = () =>
                    CacheHelper.GetCacheDependency("contentitem|bycontenttype|" + global::ETG.GoogleReviews.CONTENT_TYPE_NAME);

                return FetchFromStoreAsync();
            },
            new CacheSettings(60, nameof(GoogleReviewsService), nameof(GetReviewsAsync)));

    private async Task<ReviewListingResponse?> FetchFromStoreAsync()
    {
        var query = new ContentItemQueryBuilder()
            .ForContentType(global::ETG.GoogleReviews.CONTENT_TYPE_NAME, q => q.TopN(1));

        var items = await executor.GetMappedResult<global::ETG.GoogleReviews>(query);

        var item = items.FirstOrDefault();

        if (item is null || string.IsNullOrWhiteSpace(item.Reviews))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ReviewListingResponse>(item.Reviews, JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize Google Reviews JSON.");
            return null;
        }
    }

    public async Task<bool> RefreshFromApiAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await mediator.Send(new WebsiteSettingsQuery(), cancellationToken);

            string placeId = settings?.GlobalContent?.GooglePlaceId ?? string.Empty;
            string authToken = settings?.GlobalContent?.WextractorAuthToken ?? string.Empty;

            if (string.IsNullOrWhiteSpace(placeId) || string.IsNullOrWhiteSpace(authToken))
            {
                logger.LogWarning("Google Reviews refresh skipped: Google Place ID or Auth Token is empty.");
                return false;
            }

            string json;
            try
            {
                var client = httpClientFactory.CreateClient("Wextractor");

                // Fetch first batch (offset = 0)
                var response = await client.GetAsync(
                    $"reviews?id={Uri.EscapeDataString(placeId)}&auth_token={Uri.EscapeDataString(authToken)}&offset=0&sort=highest_rating",
                    cancellationToken);
                response.EnsureSuccessStatusCode();
                var firstBatchJson = await response.Content.ReadAsStringAsync(cancellationToken);

                var combinedResponse = JsonSerializer.Deserialize<ReviewListingResponse>(firstBatchJson, JsonOptions);
                if (combinedResponse != null && combinedResponse.Reviews != null && combinedResponse.Reviews.Count > 0)
                {
                    int currentOffset = 10;
                    int consecutiveEmptyBatches = 0;

                    while (currentOffset < 500 && consecutiveEmptyBatches < 2)
                    {
                        var nextResponse = await client.GetAsync(
                            $"reviews?id={Uri.EscapeDataString(placeId)}&auth_token={Uri.EscapeDataString(authToken)}&offset={currentOffset}&sort=highest_rating",
                            cancellationToken);

                        if (!nextResponse.IsSuccessStatusCode)
                        {
                            break;
                        }

                        var nextJson = await nextResponse.Content.ReadAsStringAsync(cancellationToken);
                        var nextBatch = JsonSerializer.Deserialize<ReviewListingResponse>(nextJson, JsonOptions);

                        if (nextBatch?.Reviews == null || nextBatch.Reviews.Count == 0)
                        {
                            consecutiveEmptyBatches++;
                        }
                        else
                        {
                            consecutiveEmptyBatches = 0;
                            combinedResponse.Reviews.AddRange(nextBatch.Reviews);
                        }

                        currentOffset += 10;
                    }

                    json = JsonSerializer.Serialize(combinedResponse, JsonOptions);
                }
                else
                {
                    json = firstBatchJson;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to fetch Google Reviews from Wextractor API.");
                return false;
            }

            //var manager = contentItemManagerFactory.Create(userId: 0);
            var user = userInfoProvider
    .Get()
    .WhereEquals(
        nameof(UserInfo.UserName),
        UserInfoProvider.DEFAULT_ADMIN_USERNAME)
    .FirstOrDefault();

            if (user == null)
            {
                logger.LogError(
                    "Unable to find the default administrator user. " +
                    "Google Reviews content cannot be created or updated.");

                return false;
            }

            var manager = contentItemManagerFactory.Create(user.UserID);

            var fieldData = new ContentItemData(new Dictionary<string, object?>
            {
                [nameof(global::ETG.GoogleReviews.Reviews)] = json,
                [nameof(global::ETG.GoogleReviews.DateExtracted)] = DateTime.UtcNow
            });

            var existing = (await executor.GetMappedResult<global::ETG.GoogleReviews>(
                new ContentItemQueryBuilder()
                    .ForContentType(global::ETG.GoogleReviews.CONTENT_TYPE_NAME, q => q.TopN(1)))).FirstOrDefault();

            if (existing is not null)
            {
                int id = existing.SystemFields.ContentItemID;
                await manager.TryCreateDraft(id, "en", cancellationToken);
                await manager.TryUpdateDraft(id, "en", fieldData, cancellationToken);
                await manager.TryPublish(id, "en", cancellationToken);
            }
            else
            {
                await manager.Create(
                    new CreateContentItemParameters(
                        global::ETG.GoogleReviews.CONTENT_TYPE_NAME,
                        ItemName,
                        ItemDisplayName,
                        "en",
                        workspaceName: "KenticoDefault"),
                    fieldData,
                    cancellationToken);
            }

            logger.LogInformation("Google Reviews data refreshed successfully.");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to refresh and store Google Reviews data.");
            return false;
        }
    }

    // DateTime extension moved here so no separate Extensions folder/file is required
}

public static class DateTimeDisplayExtensions
{
    /// <summary>
    /// Returns a human-readable relative time string (e.g. "3 days ago", "Just now").
    /// </summary>
    public static string ToRelativeText(this DateTime dt)
    {
        var diff = DateTime.UtcNow - dt.ToUniversalTime();

        return diff.TotalSeconds switch
        {
            < 60 => "Just now",
            < 3600 => $"{(int)diff.TotalMinutes} minute{Plural((int)diff.TotalMinutes)} ago",
            < 86400 => $"{(int)diff.TotalHours} hour{Plural((int)diff.TotalHours)} ago",
            < 7 * 86400 => $"{(int)diff.TotalDays} day{Plural((int)diff.TotalDays)} ago",
            < 30 * 86400 => $"{(int)(diff.TotalDays / 7)} week{Plural((int)(diff.TotalDays / 7))} ago",
            < 365 * 86400 => $"{(int)(diff.TotalDays / 30)} month{Plural((int)(diff.TotalDays / 30))} ago",
            _ => $"{(int)(diff.TotalDays / 365)} year{Plural((int)(diff.TotalDays / 365))} ago",
        };
    }

    private static string Plural(int n) => n == 1 ? "" : "s";
}
