using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Content;
using MediatR;

namespace ETG.Web.Features.TourPackage.Operations;

public sealed record GetRelatedToursQuery(Tour CurrentTour)
    : IRequest<List<Tour>>;

public sealed class GetRelatedToursQueryHandler(
    IContentQueryExecutor executor,
    IWebsiteChannelContext channelContext)
    : IRequestHandler<GetRelatedToursQuery, List<Tour>>
{
    public async Task<List<Tour>> Handle(GetRelatedToursQuery request, CancellationToken cancellationToken)
    {
        var currentTour = request.CurrentTour;
        if (currentTour?.SystemFields == null)
        {
            return [];
        }
        var parentPageId =
            currentTour.SystemFields.WebPageItemParentID;

        var currentPageId =
            currentTour.SystemFields.WebPageItemID;

        var query = new ContentItemQueryBuilder()
            .ForContentType(
                Tour.CONTENT_TYPE_NAME,
                config => config
                    .ForWebsite(
                        channelContext.WebsiteChannelName)
                    .WithLinkedItems(4)
                    .Where(where => where
                        .WhereEquals(
                            "WebPageItemParentID",
                            parentPageId)));

        var results = await executor.GetMappedWebPageResult<Tour>(query, cancellationToken: cancellationToken);

        var relatedTours = results
            .Where(tour =>
                tour?.SystemFields != null &&
                tour.SystemFields.WebPageItemID != currentPageId &&
                !string.IsNullOrWhiteSpace(tour.TourCode))
            .GroupBy(
                tour => tour.TourCode,
                StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Take(8)
            .ToList();

        return relatedTours;
    }
}