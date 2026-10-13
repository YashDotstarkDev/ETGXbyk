using CMS.ContentEngine;
using CMS.Websites;
using ETG.Core.Operations;
using Kentico.Content.Web.Mvc;

using ETG.Core.Content;

namespace ETG.Web.Features.GenericPage;

public record GenericPageQuery(RoutedWebPage Page)
    : WebPageRoutedQuery<Container>(Page);

public sealed class GenericPageQueryHandler(WebPageQueryTools tools)
    : WebPageQueryHandler<GenericPageQuery, Container>(tools)
{
    public override async Task<Container> Handle(
        GenericPageQuery request,
        CancellationToken cancellationToken = default)
    {
        var query = new ContentItemQueryBuilder()
            .ForWebPage(request.Page);

        var pages = await Executor
            .GetMappedWebPageResult<Container>(
                query,
                DefaultQueryOptions,
                cancellationToken);

        return pages.FirstOrDefault();
    }
}