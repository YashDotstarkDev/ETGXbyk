using CMS.ContentEngine;
using CMS.Websites;
using ETG.Core.Content;
using ETG.Core.Operations;
using Kentico.Content.Web.Mvc;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ETG.Web.Features.TourPackage.Operations;

// 1. Inherit from custom base query
public record TourLandingQuery(RoutedWebPage Page)
    : WebPageRoutedQuery<Page>(Page);

// 2. Inherit from custom base handler
public class TourLandingQueryHandler(WebPageQueryTools tools)
    : WebPageQueryHandler<TourLandingQuery, Page>(tools)
{
    public override async Task<Page> Handle(
        TourLandingQuery request,
        CancellationToken cancellationToken = default)
    {
        // Use ForWebPage helper just like your senior did
        var query = new ContentItemQueryBuilder()
            .ForWebPage(request.Page);

        var result = await Executor.GetMappedWebPageResult<Page>(
            query,
            DefaultQueryOptions,
            cancellationToken);

        return result.FirstOrDefault();
    }
}
