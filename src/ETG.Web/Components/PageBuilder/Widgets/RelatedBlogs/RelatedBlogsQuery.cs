using CMS.ContentEngine;
using CMS.Websites;
using ETG.Core.Content;
using ETG.Core.Operations;

namespace ETG.Web.Components.PageBuilder.Widgets.RelatedBlogs;

// ── Latest posts query ────────────────────────────────────────────────────────

/// <summary>
/// Fetches the latest N published blog post pages for the given channel, ordered
/// newest-first. Ordering is done in-memory because BlogPostContentPublishedDate
/// lives on the linked BlogPostContent item and cannot be used in SQL ORDER BY.
/// </summary>
public record RelatedBlogsQuery(
    string ChannelName,
    int Count,
    Guid? ExcludePageGuid = null)
    : IQuery<RelatedBlogsQueryResponse>, ICacheByValueQuery, IChannelContentQuery
{
    public string CacheValueKey => $"{ChannelName}|{Count}|{ExcludePageGuid}";
}

public record RelatedBlogsQueryResponse(IReadOnlyList<BlogPostPage> Items);

public class RelatedBlogsQueryHandler(WebPageQueryTools tools)
    : WebPageQueryHandler<RelatedBlogsQuery, RelatedBlogsQueryResponse>(tools)
{
    public override async Task<RelatedBlogsQueryResponse> Handle(
        RelatedBlogsQuery request,
        CancellationToken cancellationToken = default)
    {
        var query = new ContentItemQueryBuilder()
            .ForContentType(
                BlogPostPage.CONTENT_TYPE_NAME,
                q => q
                    .ForWebsite(request.ChannelName)
                    // Depth 4: BlogPostPage → BlogPostContent → AuthorContent → AuthorPhoto (ImageContent)
                    //          BlogPostPage → BlogPostContent → TeaserImage (ImageContent) [depth 3]
                    .WithLinkedItems(4));

        var allPages = await Executor.GetMappedWebPageResult<BlogPostPage>(
            query, DefaultQueryOptions, cancellationToken);

        var filtered = allPages
            .Where(p => request.ExcludePageGuid == null
                        || p.SystemFields.WebPageItemGUID != request.ExcludePageGuid)
            // Sort newest-first in-memory — published date is on the linked BlogPostContent item
            .OrderByDescending(p =>
                p.BlogPostPageBlogPostContent.FirstOrDefault()?.BlogPostContentPublishedDate
                ?? DateTime.MinValue)
            .Take(request.Count)
            .ToList();

        return new RelatedBlogsQueryResponse(filtered);
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(
        RelatedBlogsQuery query,
        RelatedBlogsQueryResponse result,
        ICacheDependencyKeysBuilder builder) =>
        builder
            .AllContentItems(BlogPostContent.CONTENT_TYPE_NAME)
            .Collection(result.Items, (page, b) =>
                b.WebPage(page.SystemFields.WebPageItemID)
                 .Collection(page.BlogPostPageBlogPostContent, (content, b2) =>
                     b2.ContentItem(content)));
}

// ── Manual selection query ────────────────────────────────────────────────────

/// <summary>
/// Fetches specific blog post pages by their ContentItem GUIDs (as stored in
/// <see cref="CMS.ContentEngine.ContentItemReference.Identifier"/>).
///
/// IMPORTANT: ContentItemReference.Identifier is the ContentItemGUID, NOT the
/// WebPageItemGUID. These are different identifiers — this query filters on
/// ContentItemGUID via the WebPageItemContentItemID join.
/// </summary>
public record RelatedBlogsManualQuery(
    string ChannelName,
    IList<Guid> ContentItemGuids)
    : IQuery<RelatedBlogsQueryResponse>, ICacheByValueQuery, IChannelContentQuery
{
    public string CacheValueKey =>
        $"{ChannelName}|{string.Join(",", ContentItemGuids.OrderBy(g => g))}";
}

public class RelatedBlogsManualQueryHandler(WebPageQueryTools tools)
    : WebPageQueryHandler<RelatedBlogsManualQuery, RelatedBlogsQueryResponse>(tools)
{
    public override async Task<RelatedBlogsQueryResponse> Handle(
        RelatedBlogsManualQuery request,
        CancellationToken cancellationToken = default)
    {
        if (request.ContentItemGuids.Count == 0)
        {
            return new RelatedBlogsQueryResponse([]);
        }

        var query = new ContentItemQueryBuilder()
            .ForContentType(
                BlogPostPage.CONTENT_TYPE_NAME,
                q => q
                    .ForWebsite(request.ChannelName)
                    .WithLinkedItems(4)
                    // Filter to only the selected content items by their ContentItemGUID
                    .Where(w => w.WhereIn(
                        nameof(ContentItemFields.ContentItemGUID),
                        request.ContentItemGuids.ToArray())));

        var pages = await Executor.GetMappedWebPageResult<BlogPostPage>(
            query, DefaultQueryOptions, cancellationToken);

        // Restore the editor-defined order (DB returns rows in arbitrary order)
        var ordered = pages
            .OrderBy(p => request.ContentItemGuids.IndexOf(p.SystemFields.ContentItemGUID))
            .ToList();

        return new RelatedBlogsQueryResponse(ordered);
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(
        RelatedBlogsManualQuery query,
        RelatedBlogsQueryResponse result,
        ICacheDependencyKeysBuilder builder) =>
        builder
            .AllContentItems(BlogPostContent.CONTENT_TYPE_NAME)
            .Collection(result.Items, (page, b) =>
                b.WebPage(page.SystemFields.WebPageItemID)
                 .Collection(page.BlogPostPageBlogPostContent, (content, b2) =>
                     b2.ContentItem(content)));
}
