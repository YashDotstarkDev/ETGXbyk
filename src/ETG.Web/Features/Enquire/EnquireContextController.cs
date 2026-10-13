using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Content;
using Kentico.Content.Web.Mvc;

namespace ETG.Web.Features.Enquire;

public class EnquireContextController
{
    private readonly IContentQueryExecutor contentQueryExecutor;
    private readonly IWebsiteChannelContext websiteChannelContext;

    public EnquireContextController(
        IContentQueryExecutor contentQueryExecutor,
        IWebsiteChannelContext websiteChannelContext)
    {
        this.contentQueryExecutor = contentQueryExecutor;
        this.websiteChannelContext = websiteChannelContext;
    }

    public async Task<Destination?> GetDestinationByAlias(
        string alias,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(alias))
        {
            return null;
        }

        var query = new ContentItemQueryBuilder()
            .ForContentType(
                Destination.CONTENT_TYPE_NAME,
                config =>
                {
                    config.ForWebsite(
                        websiteChannelContext.WebsiteChannelName);
                });

        var destinations =
            await contentQueryExecutor.GetMappedWebPageResult<Destination>(
                query,
                new ContentQueryExecutionOptions
                {
                    ForPreview = false
                },
                cancellationToken: cancellationToken);

        var requestedAlias = NormalizeAlias(alias);

        return destinations.FirstOrDefault(destination =>
            string.Equals(
                NormalizeAlias(destination.DestinationName),
                requestedAlias,
                StringComparison.OrdinalIgnoreCase));
    }

    public async Task<ETG.Core.Content.Experience?> GetExperienceByAlias(
        string alias,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(alias))
        {
            return null;
        }

        var query = new ContentItemQueryBuilder()
            .ForContentType(
                ETG.Core.Content.Experience.CONTENT_TYPE_NAME,
                config =>
                {
                    config.ForWebsite(
                        websiteChannelContext.WebsiteChannelName);
                });

        var experiences =
            await contentQueryExecutor.GetMappedWebPageResult<ETG.Core.Content.Experience>(
                query,
                new ContentQueryExecutionOptions
                {
                    ForPreview = false
                },
                cancellationToken: cancellationToken);

        var requestedAlias = NormalizeAlias(alias);

        return experiences.FirstOrDefault(experience =>
            string.Equals(
                NormalizeAlias(experience.ExperienceName),
                requestedAlias,
                StringComparison.OrdinalIgnoreCase));
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