using CMS.ContentEngine;
using CMS.Websites;
using ETG.Core.Content;
using ETG.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace ETG.Web.Features.DefaultPage;

public record DefaultPageQuery(RoutedWebPage Page)
    : WebPageRoutedQuery<Default>(Page);

public sealed class DefaultPageQueryHandler(WebPageQueryTools tools)
    : WebPageQueryHandler<DefaultPageQuery, Default>(tools)
{
    public override async Task<Default> Handle(
        DefaultPageQuery request,
        CancellationToken cancellationToken = default)
    {
        var query = new ContentItemQueryBuilder()
            .ForWebPage(
                request.Page,
                queryParams => queryParams
                    .WithLinkedItems(2)
            );

        var pages = await Executor
            .GetMappedWebPageResult<Default>(
                query,
                DefaultQueryOptions,
                cancellationToken);

        return pages.FirstOrDefault();
    }
}
