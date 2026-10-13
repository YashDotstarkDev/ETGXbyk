using CMS.ContentEngine;
using CMS.Websites;
using ETG.Core.Content;
using ETG.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace ETG.Web.Features.Blog.Operations;

public record BlogLandingPageQuery(RoutedWebPage Page, string ChannelName) : IQuery<BlogLandingPageQueryResponse>, ICacheByValueQuery, IChannelContentQuery
{
    public string CacheValueKey => Page.WebPageItemID.ToString();
}

public record BlogLandingPageQueryResponse(BlogLandingPage LandingPage, IReadOnlyList<BlogPostPage> BlogPosts);

public class BlogLandingPageQueryHandler(WebPageQueryTools tools) : WebPageQueryHandler<BlogLandingPageQuery, BlogLandingPageQueryResponse>(tools)
{
    public override async Task<BlogLandingPageQueryResponse> Handle(BlogLandingPageQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForWebPage(request.Page, c => c.WithLinkedItems(4));
        var r = await Executor.GetMappedWebPageResult<BlogLandingPage>(b, DefaultQueryOptions, cancellationToken);
        var landingPage = r.First();

        var postsQuery = new ContentItemQueryBuilder().ForContentType(BlogPostPage.CONTENT_TYPE_NAME, queryParams =>
        {
            _ = queryParams
                .ForWebsite(request.ChannelName)
                .WithLinkedItems(3);
        });

        var blogPostsResult = await Executor.GetMappedWebPageResult<BlogPostPage>(postsQuery, DefaultQueryOptions, cancellationToken);

        return new(landingPage, blogPostsResult.ToList());
    }
}