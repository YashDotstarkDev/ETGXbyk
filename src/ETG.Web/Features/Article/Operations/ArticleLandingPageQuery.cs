using CMS.ContentEngine;
using ETG.Core.Operations;
using MediatR;
using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Content;
using ETG.Core.Operations;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ETG.Web.Features.Article;

// 1. Article Query
public record ArticleLandingPageQuery(string WebsiteChannelName) : IRequest<ArticleLandingPageResult>;
public record ArticleLandingPageResult(IReadOnlyList<ETG.Core.Content.Article> Items);

public class ArticleLandingPageQueryHandler(WebPageQueryTools tools)
    : IRequestHandler<ArticleLandingPageQuery, ArticleLandingPageResult>
{
    public async Task<ArticleLandingPageResult> Handle(
        ArticleLandingPageQuery request,
        CancellationToken cancellationToken = default)
    {
        var builder = new ContentItemQueryBuilder()
            .ForContentType(ETG.Core.Content.Article.CONTENT_TYPE_NAME, q => q
                // FIX: Place .ForWebsite() INSIDE the ContentType configuration
                .ForWebsite(request.WebsiteChannelName)
                .WithLinkedItems(1)
                .OrderBy(new OrderByColumn(nameof(ETG.Core.Content.Article.ArticleDate), OrderDirection.Descending))
            );

        var results = await tools.Executor.GetMappedWebPageResult<ETG.Core.Content.Article>(
            builder,
            cancellationToken: cancellationToken);

        return new ArticleLandingPageResult(results.ToList());
    }
}

// 2. Popular Destinations Query
public record PopularDestinationsQuery(string WebsiteChannelName, int TopN = 7) : IRequest<PopularDestinationsResult>;
public record PopularDestinationsResult(IReadOnlyList<ETG.Core.Content.Destination> Items);

public class PopularDestinationsQueryHandler(WebPageQueryTools tools)
    : IRequestHandler<PopularDestinationsQuery, PopularDestinationsResult>
{
    public async Task<PopularDestinationsResult> Handle(
        PopularDestinationsQuery request,
        CancellationToken cancellationToken = default)
    {
        var builder = new ContentItemQueryBuilder()
            .ForContentType(ETG.Core.Content.Destination.CONTENT_TYPE_NAME, q => q
                // FIX: Place .ForWebsite() INSIDE the ContentType configuration
                .ForWebsite(request.WebsiteChannelName)
                .TopN(request.TopN)
            );

        var results = await tools.Executor.GetMappedWebPageResult<ETG.Core.Content.Destination>(
            builder,
            cancellationToken: cancellationToken);

        return new PopularDestinationsResult(results.ToList());
    }
}
