using CMS.ContentEngine;
using CMS.Websites;
using ETG.Core.Content;
using ETG.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace ETG.Web.Features.AgentIncentive.Operations;

public record AgentIncentivePageQuery(
    RoutedWebPage Page,
    bool ForPreview)
    : IQuery<Incentive>, ICacheByValueQuery
{
    public string CacheValueKey =>
        $"{Page.WebPageItemID}:{ForPreview}";
}

public class AgentIncentivePageQueryHandler(WebPageQueryTools tools)
    : WebPageQueryHandler<AgentIncentivePageQuery, Incentive>(tools)
{
    public override async Task<Incentive> Handle(
        AgentIncentivePageQuery request,
        CancellationToken cancellationToken = default)
    {
        var query = new ContentItemQueryBuilder()
            .ForWebPage(
                request.Page,
                parameters => parameters.WithLinkedItems(1));

        var pages = await Executor.GetMappedWebPageResult<Incentive>(
            query,
            new ContentQueryExecutionOptions
            {
                ForPreview = request.ForPreview
            },
            cancellationToken);

        return pages.FirstOrDefault()
            ?? throw new InvalidOperationException(
                $"Incentive page {request.Page.WebPageItemID} was not found.");
    }
}