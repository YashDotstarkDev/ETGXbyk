using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CMS.ContentEngine;
using CMS.Websites;
using ETG.Core.Content;
using ETG.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace ETG.Web.Features.AgentPortal;

public record AgentPortalPageQuery(RoutedWebPage Page)
    : WebPageRoutedQuery<AgentPortalMainPage>(Page);

public class AgentPortalPageQueryHandler(WebPageQueryTools tools)
    : WebPageQueryHandler<AgentPortalPageQuery, AgentPortalMainPage>(tools)
{
    public override async Task<AgentPortalMainPage> Handle(
        AgentPortalPageQuery request,
        CancellationToken cancellationToken = default)
    {
        var query = new ContentItemQueryBuilder()
            .ForWebPage(
                request.Page,
                parameters => parameters.WithLinkedItems(
                    4,
                    options => options.IncludeWebPageData()));

        var pages = await Executor.GetMappedWebPageResult<AgentPortalMainPage>(
            query,
            new ContentQueryExecutionOptions
            {
                ForPreview = true
            },
            cancellationToken);

        return pages.First();
    }
}