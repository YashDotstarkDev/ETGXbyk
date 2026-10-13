using CMS.ContentEngine;
using CMS.Websites;
using ETG;
using ETG.Core.Content;
using ETG.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace ETG.Web.Features.TravelType.Operations;


public record TravelTypeLandingPageQuery(
    RoutedWebPage Page,
    string ChannelName
) : IQuery<TravelTypeLandingPageQueryResponse>,
    ICacheByValueQuery,
    IChannelContentQuery
{
    public string CacheValueKey => Page.WebPageItemID.ToString();
}

public record TravelTypeLandingPageQueryResponse(
    TravelTypeLanding Page,
    IReadOnlyList<TravelTypeDetail> Items
);

public class TravelTypeLandingPageQueryHandler(
    WebPageQueryTools tools
) : WebPageQueryHandler<
    TravelTypeLandingPageQuery,
    TravelTypeLandingPageQueryResponse>(tools)
{
    public override async Task<TravelTypeLandingPageQueryResponse> Handle(
        TravelTypeLandingPageQuery request,
        CancellationToken cancellationToken = default)
    {
        var landingPageQuery = new ContentItemQueryBuilder()
            .ForWebPage(
                request.Page,
                query => query.WithLinkedItems(4)
            );

        var landingPage = (
            await Executor.GetMappedWebPageResult<TravelTypeLanding>(
                landingPageQuery,
                DefaultQueryOptions,
                cancellationToken
            )
        ).FirstOrDefault();

        if (landingPage is null)
        {
            return null;
        }

        var travelTypeDetailsQuery = new ContentItemQueryBuilder()
            .ForContentType(
                TravelTypeDetail.CONTENT_TYPE_NAME,
                query => query
                    .ForWebsite(
                        request.ChannelName,
                        PathMatch.Children(
                            landingPage.SystemFields.WebPageItemTreePath,
                            nestingLevel: 1
                        )
                    )
                    .WithLinkedItems(4)
                    .OrderBy(nameof(WebPageFields.WebPageItemOrder))
            );

        var travelTypeDetails = (
     await Executor.GetMappedWebPageResult<TravelTypeDetail>(
         travelTypeDetailsQuery,
         new ContentQueryExecutionOptions { ForPreview = false }, 
         cancellationToken
     )
 ).ToList();


        return new TravelTypeLandingPageQueryResponse(
            landingPage,
            travelTypeDetails
        );
    }
}