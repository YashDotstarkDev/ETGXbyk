using CMS.ContentEngine;
using CMS.Websites;
using ETG.Core.Content;
using ETG.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace ETG.Web.Features.Destinations.Operations;

public record MainDestinationsQuery(
    RoutedWebPage Page,
    string WebsiteChannelName,
    string ParentPath)
    : WebPageRoutedQuery<IReadOnlyList<Destination>>(Page);

public class MainDestinationsQueryHandler(WebPageQueryTools tools)
    : WebPageQueryHandler<
        MainDestinationsQuery,
        IReadOnlyList<Destination>>(tools)
{
    private static readonly ContentQueryExecutionOptions PublishedOnlyQueryOptions =
        new()
        {
            ForPreview = false
        };

    public override async Task<IReadOnlyList<Destination>> Handle(
        MainDestinationsQuery request,
        CancellationToken cancellationToken = default)
    {
        var query = new ContentItemQueryBuilder();

        query.ForContentType(
            Destination.CONTENT_TYPE_NAME,
            builder =>
            {
                builder.ForWebsite(
                    request.WebsiteChannelName,
                    PathMatch.Children(
                        request.ParentPath,
                        nestingLevel: 1));

                builder.WithLinkedItems(1);
            });

        var result = await Executor.GetMappedWebPageResult<Destination>(
            query,
            PublishedOnlyQueryOptions,
            cancellationToken);

        return result
            .OrderBy(destination => destination.SystemFields.WebPageItemOrder)
            .ToList();
    }
}