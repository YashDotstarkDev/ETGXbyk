using CMS.ContentEngine;
using CMS.Websites;
using ETG.Core.Content;
using ETG.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace ETG.Web.Features.DestinationPostPage;

public record DestinationPostPageQuery(RoutedWebPage Page)
    : WebPageRoutedQuery<Destination>(Page);

public class DestinationPostPageQueryHandler(WebPageQueryTools tools)
    : WebPageQueryHandler<DestinationPostPageQuery, Destination>(tools)
{
    public override async Task<Destination> Handle(
        DestinationPostPageQuery request,
        CancellationToken cancellationToken = default)
    {
        var destinationQuery = new ContentItemQueryBuilder()
            .ForWebPage(request.Page, c => c.WithLinkedItems(4));

        var result = await Executor.GetMappedWebPageResult<Destination>(
            destinationQuery,
            DefaultQueryOptions,
            cancellationToken);

        return result.First();
    }
}