using CMS.ContentEngine;
using CMS.Websites;
using ETG;
using ETG.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace ETG.Web.Features.Career.Operations;

public record CareerRoleDetailPageQuery(
    RoutedWebPage Page,
    string ChannelName
) : WebPageRoutedQuery<CareerRoleDetailPageQueryResponse>(Page),
    IChannelContentQuery
{
    public override string CacheValueKey => $"{Page.WebPageItemID}:{ChannelName}";
}

public record CareerRoleDetailPageQueryResponse(
    CareerRole Role
);

public class CareerRoleDetailPageQueryHandler(
    WebPageQueryTools tools
) : WebPageQueryHandler<
    CareerRoleDetailPageQuery,
    CareerRoleDetailPageQueryResponse>(tools)
{
    public override async Task<CareerRoleDetailPageQueryResponse?> Handle(
        CareerRoleDetailPageQuery request,
        CancellationToken cancellationToken = default)
    {
        var roleQuery = new ContentItemQueryBuilder()
            .ForWebPage(
                request.Page,
                query => query.WithLinkedItems(4)
            );

        var role = (
            await Executor.GetMappedWebPageResult<CareerRole>(
                roleQuery,
                DefaultQueryOptions,
                cancellationToken
            )
        ).FirstOrDefault();

        if (role is null)
        {
            return null;
        }

        return new CareerRoleDetailPageQueryResponse(role);
    }
}
