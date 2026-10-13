using CMS.ContentEngine;
using CMS.Websites;
using ETG;
using ETG.Core.Content;
using ETG.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace ETG.Web.Features.Career.Operations;

public record CareerLandingPageQuery(
    RoutedWebPage Page,
    string ChannelName
) : IQuery<CareerLandingPageQueryResponse>,
    ICacheByValueQuery,
    IChannelContentQuery
{
    public string CacheValueKey => Page.WebPageItemID.ToString();
}

public record CareerLandingPageQueryResponse(
    Page Page,
    IReadOnlyList<CareerRole> Items
);

public class CareerLandingPageQueryHandler(
    WebPageQueryTools tools
) : WebPageQueryHandler<
    CareerLandingPageQuery,
    CareerLandingPageQueryResponse>(tools)
{
    public override async Task<CareerLandingPageQueryResponse> Handle(
        CareerLandingPageQuery request,
        CancellationToken cancellationToken = default)
    {
        // 1. Query the landing page (ETG.Page)
        var landingPageQuery = new ContentItemQueryBuilder()
            .ForWebPage(
                request.Page,
                query => query.WithLinkedItems(4)
            );

        var landingPage = (
            await Executor.GetMappedWebPageResult<Page>(
                landingPageQuery,
                DefaultQueryOptions,
                cancellationToken
            )
        ).FirstOrDefault();

        if (landingPage is null)
        {
            return null;
        }

        // 2. Query published CareerRole items from the /career-roles folder
        var careerRolesQuery = new ContentItemQueryBuilder()
            .ForContentType(
                CareerRole.CONTENT_TYPE_NAME,
                query => query
                    .ForWebsite(
                        request.ChannelName,
                        PathMatch.Children("/career-roles")
                    )
                    .WithLinkedItems(4)
                    .OrderBy(nameof(WebPageFields.WebPageItemOrder))
            );

        var careerRoles = (
            await Executor.GetMappedWebPageResult<CareerRole>(
                careerRolesQuery,
                DefaultQueryOptions,
                cancellationToken
            )
        ).ToList();

        return new CareerLandingPageQueryResponse(
            landingPage,
            careerRoles
        );
    }
}
