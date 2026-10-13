using CMS.ContentEngine;
using CMS.Websites;
using ETG.Core.Content;
using ETG.Core.Operations;

namespace ETG.Web.Features.Blog.Operations;
public record BlogPostPagesLatestQuery(BlogPostContent post, string ChannelName) : IQuery<BlogPostPagesLatestQueryResponse>, ICacheByValueQuery, IChannelContentQuery
{
    public string CacheValueKey => post.ToString();
}
public record BlogPostPagesLatestQueryResponse(IReadOnlyList<BlogPostPage> Items);
public class BlogPostPagesLatestQueryHandler(WebPageQueryTools tools)
    : WebPageQueryHandler<BlogPostPagesLatestQuery, BlogPostPagesLatestQueryResponse>(tools)
{
    public override async Task<BlogPostPagesLatestQueryResponse> Handle(BlogPostPagesLatestQuery request, CancellationToken cancellationToken = default)
    {
        // Retrieve the IDs of the related blogs from the current post
        var relatedBlogIDs = request.post.BlogPostContentRelatedBlogs.Select(rb => rb.SystemFields.ContentItemID).ToList();

        if (!relatedBlogIDs.Any())
        {
            return new BlogPostPagesLatestQueryResponse(new List<BlogPostPage>());
        }

        // Query to retrieve the content items for the related blogs only
        var postsQuery = new ContentItemQueryBuilder()
            .ForContentType(BlogPostPage.CONTENT_TYPE_NAME, queryParams =>
            {
                queryParams
                    .ForWebsite(request.ChannelName)
                    .Linking(nameof(BlogPostPage.BlogPostPageBlogPostContent), relatedBlogIDs)
                    .WithLinkedItems(4);
            });

        var pages = await Executor.GetMappedWebPageResult<BlogPostPage>(postsQuery, DefaultQueryOptions, cancellationToken);

        // Order pages by the position of their content item IDs in the original related blogs list
        var orderedPages = pages.OrderBy(p => relatedBlogIDs.IndexOf(p.BlogPostPageBlogPostContent.FirstOrDefault()?.SystemFields.ContentItemID ?? 0)).ToList();

        return new BlogPostPagesLatestQueryResponse(orderedPages);
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(BlogPostPagesLatestQuery query, BlogPostPagesLatestQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder
            .AllContentItems(BlogPostContent.CONTENT_TYPE_NAME)
            .Collection(result.Items, (page, builder) =>
                builder.WebPage(page.SystemFields.WebPageItemID)
                    .Collection(page.BlogPostPageBlogPostContent, (content, builder) => builder.ContentItem(content)));
}
