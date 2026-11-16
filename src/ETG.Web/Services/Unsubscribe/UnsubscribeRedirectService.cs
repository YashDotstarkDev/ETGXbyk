using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using Kentico.Content.Web.Mvc;
using ContainerContent = ETG.Core.Content.Container;

namespace ETG.Web.Services.Unsubscribe;

public sealed class UnsubscribeRedirectService
{
    private readonly IContentQueryExecutor executor;
    private readonly IWebPageUrlRetriever urlRetriever;
    private readonly IWebsiteChannelContext channelContext;

    public UnsubscribeRedirectService(
        IContentQueryExecutor executor,
        IWebPageUrlRetriever urlRetriever,
        IWebsiteChannelContext channelContext)
    {
        this.executor = executor;
        this.urlRetriever = urlRetriever;
        this.channelContext = channelContext;
    }

    public async Task<string> GetUrlAsync(
        string treePath,
        string languageName,
        CancellationToken cancellationToken)
    {
        var query = new ContentItemQueryBuilder()
            .ForContentType(
                ContainerContent.CONTENT_TYPE_NAME,
                parameters =>
                {
                    parameters.ForWebsite(
                        channelContext.WebsiteChannelName,
                        PathMatch.Single(treePath));
                })
            .InLanguage(languageName);

        var pages = await executor
            .GetMappedWebPageResult<ContainerContent>(
                query,
                new ContentQueryExecutionOptions
                {
                    ForPreview = false
                },
                cancellationToken);

        var page = pages.FirstOrDefault();

        if (page is null)
        {
            throw new InvalidOperationException(
                $"Published Container page not found at '{treePath}' " +
                $"for language '{languageName}'.");
        }

        var url = await urlRetriever.Retrieve(page);

        return url.RelativePath;
    }
}