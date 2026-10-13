using CMS.ContentEngine;
using CMS.Websites;
using ETG.Core.Content;
using ETG.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace ETG.Web.Features.Article;

public record ArticlePostPageQuery(RoutedWebPage Page) : WebPageRoutedQuery<ETG.Core.Content.Article>(Page);

public class ArticlePageQueryHandler(WebPageQueryTools tools)
    : WebPageQueryHandler<ArticlePostPageQuery, ETG.Core.Content.Article>(tools)
{
    public override async Task<ETG.Core.Content.Article> Handle(
        ArticlePostPageQuery request,
        CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder()
            .ForWebPage(request.Page, c => c.WithLinkedItems(2));

        var r = await Executor.GetMappedWebPageResult<ETG.Core.Content.Article>(
            b, DefaultQueryOptions, cancellationToken);

        return r.First();
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(
        ArticlePostPageQuery query,
        ETG.Core.Content.Article result,
        ICacheDependencyKeysBuilder builder) =>
        base.AddDependencyKeys(query, result, builder)
            .ContentItem(result);
}