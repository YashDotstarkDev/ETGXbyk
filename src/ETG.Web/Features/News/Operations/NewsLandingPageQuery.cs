using CMS.ContentEngine;
using CMS.Websites;
using ETG.Core.Content;
using ETG.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace ETG.Web.Features.News.Operations;

public record NewsLandingPageQuery(RoutedWebPage Page, string ChannelName) : IQuery<NewsLandingPageQueryResponse>, ICacheByValueQuery, IChannelContentQuery
{
    public string CacheValueKey => Page.WebPageItemID.ToString();
}

public record NewsLandingPageQueryResponse(NewsLandingPage LandingPage, IReadOnlyList<NewsPostPage> NewsPosts);

public class NewsLandingPageQueryHandler(WebPageQueryTools tools) : WebPageQueryHandler<NewsLandingPageQuery, NewsLandingPageQueryResponse>(tools)
{
    public override async Task<NewsLandingPageQueryResponse> Handle(NewsLandingPageQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForWebPage(request.Page, c => c.WithLinkedItems(4));
        var r = await Executor.GetMappedWebPageResult<NewsLandingPage>(b, DefaultQueryOptions, cancellationToken);
        var landingPage = r.First();

        var postsQuery = new ContentItemQueryBuilder().ForContentType(NewsPostPage.CONTENT_TYPE_NAME, queryParams =>
        {
            _ = queryParams
                .ForWebsite(request.ChannelName)
                .WithLinkedItems(3);
        });

        var newsPostsResult = await Executor.GetMappedWebPageResult<NewsPostPage>(postsQuery, DefaultQueryOptions, cancellationToken);

        return new(landingPage, newsPostsResult.ToList());
    }
}
