using CMS.ContentEngine;
using CMS.Websites;
using ETG;
using ETG.Core.Content;
using ETG.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace ETG.Web.Features.TravelType.Operations;

public record TravelTypeDetailPageQuery(
    RoutedWebPage Page,
    string ChannelName
) : IQuery<TravelTypeDetailPageQueryResponse>,
    ICacheByValueQuery,
    IChannelContentQuery
{
    public string CacheValueKey => Page.WebPageItemID.ToString();
}

public record TravelTypeDetailPageQueryResponse(
    TravelTypeDetail Page,
    string ThemedPackagesHeading,
    string ThemedPackagesUrl
);

public class TravelTypeDetailPageQueryHandler(
    WebPageQueryTools tools
) : WebPageQueryHandler<
    TravelTypeDetailPageQuery,
    TravelTypeDetailPageQueryResponse>(tools)
{
    public override async Task<TravelTypeDetailPageQueryResponse> Handle(
        TravelTypeDetailPageQuery request,
        CancellationToken cancellationToken = default)
    {
        var query = new ContentItemQueryBuilder()
            .ForWebPage(
                request.Page,
                pageQuery => pageQuery.WithLinkedItems(4)
            );

        var page = (
            await Executor.GetMappedWebPageResult<TravelTypeDetail>(
                query,
                DefaultQueryOptions,
                cancellationToken
            )
        ).FirstOrDefault();

        if (page is null)
            return null;

        return new TravelTypeDetailPageQueryResponse(
            page,
            page.ThemedPackagesHeading ?? "",
            page.ThemedPackagesUrl ?? ""
        );
    }
}