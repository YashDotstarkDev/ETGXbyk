using CMS.ContentEngine;
using CMS.Websites;
using ETG.Core.Content;
using ETG.Core.Operations;

namespace ETG.Web.Features.News.Operations;
public record NewsPostPagesLatestQuery(NewsPostContent post, string ChannelName) : IQuery<NewsPostPagesLatestQueryResponse>, ICacheByValueQuery, IChannelContentQuery
{
    public string CacheValueKey => post.ToString();
}
public record NewsPostPagesLatestQueryResponse(IReadOnlyList<NewsPostPage> Items);
public class NewsPostPagesLatestQueryHandler(WebPageQueryTools tools)
    : WebPageQueryHandler<NewsPostPagesLatestQuery, NewsPostPagesLatestQueryResponse>(tools)
{
    public override async Task<NewsPostPagesLatestQueryResponse> Handle(NewsPostPagesLatestQuery request, CancellationToken cancellationToken = default)
    {
        // Retrieve the IDs of the related news from the current post
        var relatedNewsIDs = request.post.NewsPostContentRelatedNews.Select(rn => rn.SystemFields.ContentItemID).ToList();

        if (!relatedNewsIDs.Any())
        {
            return new NewsPostPagesLatestQueryResponse(new List<NewsPostPage>());
        }

        // Query to retrieve the content items for the related news only
        var postsQuery = new ContentItemQueryBuilder()
            .ForContentType(NewsPostPage.CONTENT_TYPE_NAME, queryParams =>
            {
                queryParams
                    .ForWebsite(request.ChannelName)
                    .Linking(nameof(NewsPostPage.NewsPostPageNewsPostContent), relatedNewsIDs)
                    .WithLinkedItems(4);
            });

        var pages = await Executor.GetMappedWebPageResult<NewsPostPage>(postsQuery, DefaultQueryOptions, cancellationToken);

        // Order pages by the position of their content item IDs in the original related news list
        var orderedPages = pages.OrderBy(p => relatedNewsIDs.IndexOf(p.NewsPostPageNewsPostContent.FirstOrDefault()?.SystemFields.ContentItemID ?? 0)).ToList();

        return new NewsPostPagesLatestQueryResponse(orderedPages);
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(NewsPostPagesLatestQuery query, NewsPostPagesLatestQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder
            .AllContentItems(NewsPostContent.CONTENT_TYPE_NAME)
            .Collection(result.Items, (page, builder) =>
                builder.WebPage(page.SystemFields.WebPageItemID)
                    .Collection(page.NewsPostPageNewsPostContent, (content, builder) => builder.ContentItem(content)));
}
