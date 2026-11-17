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
) : WebPageRoutedQuery<CareerLandingPageQueryResponse>(Page),
    IChannelContentQuery
{
    public override string CacheValueKey => $"{Page.WebPageItemID}:{ChannelName}";
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
        var landingPageQuery = new ContentItemQueryBuilder()
            .ForWebPage(
                request.Page,
                query => query.WithLinkedItems(4)
            );

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

        // Execute both queries concurrently
        var landingPageTask = Executor.GetMappedWebPageResult<Page>(
            landingPageQuery,
            DefaultQueryOptions,
            cancellationToken);

        var careerRolesTask = Executor.GetMappedWebPageResult<CareerRole>(
            careerRolesQuery,
            DefaultQueryOptions,
            cancellationToken);

        await Task.WhenAll(landingPageTask, careerRolesTask);

        var landingPage = (await landingPageTask).FirstOrDefault();
        if (landingPage is null)
        {
            return null;
        }

        var careerRoles = (await careerRolesTask).ToList();

        return new CareerLandingPageQueryResponse(
            landingPage,
            careerRoles
        );
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(
        CareerLandingPageQuery query,
        CareerLandingPageQueryResponse result,
        ICacheDependencyKeysBuilder builder) =>
        base.AddDependencyKeys(query, result, builder)
            .AllContentItems(CareerRole.CONTENT_TYPE_NAME);
}
