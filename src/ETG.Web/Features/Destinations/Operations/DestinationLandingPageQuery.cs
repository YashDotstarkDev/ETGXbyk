using CMS.ContentEngine;
using CMS.Websites;
using ETG.Core.Content;
using ETG.Core.Content.Pages;
using ETG.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace ETG.Web.Features.Destinations.Operations;

public record DestinationLandingPageQuery(RoutedWebPage Page)
    : WebPageRoutedQuery<Container>(Page);

public class DestinationLandingPageQueryHandler(WebPageQueryTools tools)
    : WebPageQueryHandler<DestinationLandingPageQuery, Container>(tools)
{
    public override async Task<Container> Handle(
        DestinationLandingPageQuery request,
        CancellationToken cancellationToken = default)
    {
        var query = new ContentItemQueryBuilder()
            .ForWebPage(request.Page);

        var result = await Executor.GetMappedWebPageResult<Container>(
            query,
            DefaultQueryOptions,
            cancellationToken);

        return result.First();
    }
}