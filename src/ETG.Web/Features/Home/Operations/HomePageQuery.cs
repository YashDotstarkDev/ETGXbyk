using CMS.ContentEngine;
using CMS.Websites;
using ETG.Core.Content;
using ETG.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace ETG.Web.Features.Home;

public record HomePageQuery(RoutedWebPage Page)
    : WebPageRoutedQuery<Homepage>(Page);

public sealed class HomePageQueryHandler(WebPageQueryTools tools)
    : WebPageQueryHandler<HomePageQuery, Homepage>(tools)
{
    public override async Task<Homepage> Handle(
        HomePageQuery request,
        CancellationToken cancellationToken = default)
    {
        var query = new ContentItemQueryBuilder()
            .ForWebPage(
                request.Page,
                config => config.WithLinkedItems(1));

        var pages = await Executor.GetMappedWebPageResult<Homepage>(
            query,
            DefaultQueryOptions,
            cancellationToken);

        return pages.FirstOrDefault();
    }
}