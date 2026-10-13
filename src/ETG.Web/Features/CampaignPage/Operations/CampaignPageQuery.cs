using CMS.ContentEngine;
using CMS.Websites;
using ETG.Core.Operations;
using Kentico.Content.Web.Mvc;

using ContainerContent = ETG.Core.Content.Container;

namespace ETG.Web.Features.CampaignPage;

public record CampaignPageQuery(RoutedWebPage Page)
    : WebPageRoutedQuery<ContainerContent>(Page);

public sealed class CampaignPageQueryHandler(WebPageQueryTools tools)
    : WebPageQueryHandler<CampaignPageQuery, ContainerContent>(tools)
{
    public override async Task<ContainerContent> Handle(
        CampaignPageQuery request,
        CancellationToken cancellationToken = default)
    {
        var query = new ContentItemQueryBuilder()
            .ForWebPage(request.Page);

        var pages = await Executor
            .GetMappedWebPageResult<ContainerContent>(
                query,
                DefaultQueryOptions,
                cancellationToken);

        return pages.FirstOrDefault();
    }
}