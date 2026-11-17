using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Content;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;

namespace ETG.Web.Features.TourPackage;

public sealed class UnpublishedTourRedirectStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
        app =>
        {
            app.UseMiddleware<UnpublishedTourRedirectMiddleware>();
            next(app);
        };
}

public sealed class UnpublishedTourRedirectMiddleware(
    IMemoryCache cache,
    ILogger<UnpublishedTourRedirectMiddleware> logger) : IMiddleware
{
    private const string FallbackPath = "/destinations";
    private const string LogPrefix = "[UnpublishedTourRedirect]";

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var path = context.Request.Path.Value?.Trim('/');
        var query = context.Request.QueryString;

        var isCandidate =
            HttpMethods.IsGet(context.Request.Method)
            && !string.IsNullOrEmpty(path)
            && !path.Contains('.')
            && path.Split('/').Length == 2;

        if (isCandidate)
        {
            context.Response.OnStarting(async () =>
            {
                logger.LogInformation("{Prefix} Response starting for '{Path}' with status {Status}",
                    LogPrefix, path, context.Response.StatusCode);

                if (context.Response.StatusCode != StatusCodes.Status404NotFound)
                {
                    return;
                }

                try
                {
                    var services = context.RequestServices;
                    var key = $"unpublished-tour:{path!.ToLowerInvariant()}";

                    if (!cache.TryGetValue(key, out string? target))
                    {
                        target = await ResolveTargetAsync(
                            path,
                            services.GetRequiredService<IContentQueryExecutor>(),
                            services.GetRequiredService<IContentRetriever>(),
                            services.GetRequiredService<IWebsiteChannelContext>(),
                            services.GetRequiredService<IWebPageUrlRetriever>(),
                            services.GetRequiredService<IPreferredLanguageRetriever>(),
                            CancellationToken.None);

                        cache.Set(key, target, target is null
                            ? TimeSpan.FromMinutes(1)
                            : TimeSpan.FromMinutes(5));
                    }
                    else
                    {
                        logger.LogInformation("{Prefix} Cache hit for '{Path}' -> {Target}",
                            LogPrefix, path, target ?? "(no redirect)");
                    }

                    if (target is null)
                    {
                        return;
                    }

                    context.Response.StatusCode = StatusCodes.Status302Found;
                    context.Response.Headers.Location = target + query;

                    logger.LogInformation("{Prefix} Redirecting '{Path}' -> {Target}",
                        LogPrefix, path, target + query);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "{Prefix} Failed to resolve redirect for '{Path}'", LogPrefix, path);
                }
            });
        }
        else
        {
            logger.LogDebug("{Prefix} Skipped '{Path}' (not a 2-segment GET path)", LogPrefix, path);
        }

        await next(context);
    }

    private async Task<string?> ResolveTargetAsync(
        string urlPath,
        IContentQueryExecutor executor,
        IContentRetriever contentRetriever,
        IWebsiteChannelContext channelContext,
        IWebPageUrlRetriever urlRetriever,
        IPreferredLanguageRetriever languageRetriever,
        CancellationToken cancellationToken)
    {
        var language = languageRetriever.Get();

        async Task<Tour?> FindTourAsync(ContentQueryExecutionOptions? options)
        {
            var query = new ContentItemQueryBuilder()
                .ForContentType(
                    Tour.CONTENT_TYPE_NAME,
                    config => config
                        .ForWebsite(channelContext.WebsiteChannelName, includeUrlPath: true)
                        .Where(where => where.WhereEquals(
                            nameof(WebPageFields.WebPageUrlPath),
                            urlPath))
                        .TopN(1))
                .InLanguage(language);

            var result = await executor.GetMappedWebPageResult<Tour>(
                query,
                options,
                cancellationToken);

            return result.FirstOrDefault();
        }

        if (await FindTourAsync(null) is not null)
        {
            logger.LogInformation("{Prefix} '{Path}': published tour found, not redirecting", LogPrefix, urlPath);
            return null;
        }

        var tour = await FindTourAsync(new ContentQueryExecutionOptions { ForPreview = true });

        if (tour is null)
        {
            logger.LogInformation(
                "{Prefix} '{Path}': no tour found by WebPageUrlPath (language '{Lang}', channel '{Channel}')",
                LogPrefix, urlPath, language, channelContext.WebsiteChannelName);
            return null;
        }

        logger.LogInformation("{Prefix} '{Path}': unpublished tour found ({Code})",
            LogPrefix, urlPath, tour.TourCode);

        IWebPageFieldsSource? targetPage = null;

        var countryGuid = tour.TourPrimaryCountry?
            .FirstOrDefault(x => x != null && x.WebPageGuid != Guid.Empty)?
            .WebPageGuid;

        if (countryGuid is { } destinationGuid)
        {
            targetPage = (await contentRetriever
                .RetrievePagesByGuids<Destination>(new[] { destinationGuid }))
                .FirstOrDefault();
        }

        if (targetPage is null)
        {
            var experienceGuid = tour.TourPrimaryExperience?
                .FirstOrDefault(x => x != null && x.WebPageGuid != Guid.Empty)?
                .WebPageGuid;

            if (experienceGuid is { } primaryExperienceGuid)
            {
                targetPage = (await contentRetriever
                    .RetrievePagesByGuids<ETG.Core.Content.Experience>(new[] { primaryExperienceGuid }))
                    .FirstOrDefault();
            }
        }

        if (targetPage is not null)
        {
            var url = await urlRetriever.Retrieve(targetPage);

            if (!string.IsNullOrWhiteSpace(url?.RelativePath))
            {
                return "/" + url.RelativePath.TrimStart('~', '/');
            }
        }

        logger.LogInformation("{Prefix} '{Path}': no destination/experience page, using fallback", LogPrefix, urlPath);
        return FallbackPath;
    }
}