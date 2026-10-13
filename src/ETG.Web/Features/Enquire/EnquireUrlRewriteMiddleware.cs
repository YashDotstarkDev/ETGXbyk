using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Content;
using Kentico.Content.Web.Mvc;

namespace ETG.Web.Features.Enquire;

public class EnquireUrlRewriteMiddleware
{
    private const string WebsiteChannelName = "ETG";

    private readonly RequestDelegate next;

    public EnquireUrlRewriteMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IContentQueryExecutor contentQueryExecutor)
    {
        var path = context.Request.Path.Value;

        if (string.IsNullOrWhiteSpace(path))
        {
            await next(context);
            return;
        }

        if (path.StartsWith(
                "/destinations/enquire/",
                StringComparison.OrdinalIgnoreCase))
        {
            var alias = path[
                "/destinations/enquire/".Length..]
                .Trim('/');

            if (!string.IsNullOrWhiteSpace(alias))
            {
                var destination =
                    await GetDestinationByAlias(
                        alias,
                        contentQueryExecutor,
                        context.RequestAborted);

                if (destination != null)
                {
                    context.Request.Path = "/enquire";

                    context.Request.QueryString =
                        new QueryString(
                            "?destination=" +
                            Uri.EscapeDataString(
                                destination.DestinationName));
                }
            }
        }

        else if (path.StartsWith(
                     "/experiences/enquire/",
                     StringComparison.OrdinalIgnoreCase))
        {
            var alias = path[
                "/experiences/enquire/".Length..]
                .Trim('/');

            if (!string.IsNullOrWhiteSpace(alias))
            {
                var experience =
                    await GetExperienceByAlias(
                        alias,
                        contentQueryExecutor,
                        context.RequestAborted);

                if (experience != null)
                {
                    context.Request.Path = "/enquire";

                    context.Request.QueryString =
                        new QueryString(
                            "?experience=" +
                            Uri.EscapeDataString(
                                experience.ExperienceName));
                }
            }
        }

        else if (path.StartsWith(
                     "/tour-types/enquire/",
                     StringComparison.OrdinalIgnoreCase))
        {
            var alias = path[
                "/tour-types/enquire/".Length..]
                .Trim('/');

            if (!string.IsNullOrWhiteSpace(alias))
            {
                var tourType = GetTourTypeByAlias(alias);

                if (!string.IsNullOrWhiteSpace(tourType))
                {
                    context.Request.Path = "/enquire";

                    context.Request.QueryString =
                        new QueryString(
                            "?tourType=" +
                            Uri.EscapeDataString(tourType));
                }
            }
        }

        await next(context);
    }

    private static async Task<Destination?> GetDestinationByAlias(
        string alias,
        IContentQueryExecutor contentQueryExecutor,
        CancellationToken cancellationToken)
    {
        var query = new ContentItemQueryBuilder()
            .ForContentType(
                Destination.CONTENT_TYPE_NAME,
                config =>
                {
                    config.ForWebsite(
                        WebsiteChannelName);
                });

        var destinations =
            await contentQueryExecutor.GetMappedWebPageResult<Destination>(
                query,
                new ContentQueryExecutionOptions
                {
                    ForPreview = false
                },
                cancellationToken: cancellationToken);

        var requestedAlias =
            NormalizeAlias(alias);

        return destinations.FirstOrDefault(destination =>
            string.Equals(
                NormalizeAlias(
                    destination.DestinationName),
                requestedAlias,
                StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<ETG.Core.Content.Experience?> GetExperienceByAlias(
        string alias,
        IContentQueryExecutor contentQueryExecutor,
        CancellationToken cancellationToken)
    {
        var query = new ContentItemQueryBuilder()
            .ForContentType(
                ETG.Core.Content.Experience.CONTENT_TYPE_NAME,
                config =>
                {
                    config.ForWebsite(
                        WebsiteChannelName);
                });

        var experiences =
            await contentQueryExecutor
                .GetMappedWebPageResult<ETG.Core.Content.Experience>(
                    query,
                    new ContentQueryExecutionOptions
                    {
                        ForPreview = false
                    },
                    cancellationToken: cancellationToken);

        var requestedAlias =
            NormalizeAlias(alias);

        return experiences.FirstOrDefault(experience =>
            string.Equals(
                NormalizeAlias(
                    experience.ExperienceName),
                requestedAlias,
                StringComparison.OrdinalIgnoreCase));
    }

    private static string GetTourTypeByAlias(string alias)
    {
        var tourTypes = new[]
        {
            "Small group tours",
            "Escorted group tours",
            "Private tours",
            "Independent tours",
            "Packaged tours",
            "Day tours",
            "Rail Journeys",
            "Cruise Journeys",
            "Group Journeys"
        };

        var requestedAlias =
            NormalizeAlias(alias);

        return tourTypes.FirstOrDefault(
            tourType =>
                string.Equals(
                    NormalizeAlias(tourType),
                    requestedAlias,
                    StringComparison.OrdinalIgnoreCase))
            ?? string.Empty;
    }

    private static string NormalizeAlias(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }
        return value
            .Trim()
            .Trim('/')
            .Replace(" ", "-")
            .ToLowerInvariant();
    }
}