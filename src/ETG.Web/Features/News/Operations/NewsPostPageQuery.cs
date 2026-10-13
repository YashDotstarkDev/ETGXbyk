using CMS.ContentEngine;
using CMS.Websites;
using ETG.Core.Content;
using ETG.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace ETG.Web.Features.News;

public record NewsPostPageQuery(RoutedWebPage Page) : WebPageRoutedQuery<NewsPostPage>(Page);
public class NewsPostPageQueryHandler(WebPageQueryTools tools) : WebPageQueryHandler<NewsPostPageQuery, NewsPostPage>(tools)
{
    public override async Task<NewsPostPage> Handle(NewsPostPageQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForWebPage(request.Page, c => c.WithLinkedItems(4));

        var r = await Executor.GetMappedWebPageResult<NewsPostPage>(b, DefaultQueryOptions, cancellationToken);

        return r.First();
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(NewsPostPageQuery query, NewsPostPage result, ICacheDependencyKeysBuilder builder) =>
    base.AddDependencyKeys(query, result, builder)
        .Collection(
            result.NewsPostPageNewsPostContent,
            (content, builder) => builder
                .ContentItem(content)
                .Collection(
                    content.NewsPostContentRelatedNews,
                    (relatedNews, builder) => builder
                        .ContentItem(relatedNews)));
}
