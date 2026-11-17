using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Content;
using ETG.Core.Operations;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ETG.Web.Features.Article;

public record ArticlePostPageToursQuery(List<string> Destinations, string ChannelName = "") : IRequest<ArticlePostPageToursResult>;
public record ArticlePostPageToursResult(IReadOnlyList<ETG.Core.Content.Tour> Items);

public class ArticlePostPageToursQueryHandler(WebPageQueryTools tools)
    : IRequestHandler<ArticlePostPageToursQuery, ArticlePostPageToursResult>
{
    public async Task<ArticlePostPageToursResult> Handle(
        ArticlePostPageToursQuery request,
        CancellationToken cancellationToken = default)
    {
        if (request.Destinations == null || !request.Destinations.Any())
        {
            return new ArticlePostPageToursResult([]);
        }

        // 1. Get valid destination GUIDs from the article
        var validDestinationGuids = request.Destinations
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Select(d => Guid.TryParse(d, out var g) ? g : Guid.Empty)
            .Where(g => g != Guid.Empty)
            .ToList();

        if (!validDestinationGuids.Any())
        {
            return new ArticlePostPageToursResult([]);
        }

        // 2. Fetch tours with linked items matching any article destination in a single combined query
        // Exactly like ExperiencePageTemplates: query.ForWebsite(channelName) with IncludeWebPageData()
        var tourBuilder = new ContentItemQueryBuilder()
            .ForContentType(ETG.Core.Content.Tour.CONTENT_TYPE_NAME, q =>
            {
                q.ForWebsite(request.ChannelName);
                q.WithLinkedItems(4, options => options.IncludeWebPageData());
                q.Where(w =>
                {
                    w.WhereLike(nameof(ETG.Core.Content.Tour.TourPrimaryCountry), $"%{validDestinationGuids[0]}%");
                    for (int i = 1; i < validDestinationGuids.Count; i++)
                    {
                        var extraGuid = validDestinationGuids[i];
                        w.Or().WhereLike(nameof(ETG.Core.Content.Tour.TourPrimaryCountry), $"%{extraGuid}%");
                    }
                });
            });

        var allTours = await tools.Executor.GetMappedWebPageResult<ETG.Core.Content.Tour>(
            tourBuilder,
            cancellationToken: cancellationToken);

        // 3. Filter out hidden upgrade tours (TourIsUpgrade == "2")
        var validTours = allTours
            .Where(tour =>
                string.IsNullOrEmpty(tour.TourIsUpgrade) || tour.TourIsUpgrade != "2"
            )
            .ToList();

        // 4. Preserve destination priority ranking and deduplication:
        // Primary destination tours come first. If fewer than 8 tours and multiple destinations exist,
        // fill from subsequent destinations until at least 8 tours (or all available destinations exhausted).
        var matchedTours = new List<ETG.Core.Content.Tour>();
        var existingGuids = new HashSet<Guid>();

        var firstGuid = validDestinationGuids[0];
        foreach (var tour in validTours)
        {
            if (tour.TourPrimaryCountry != null && tour.TourPrimaryCountry.Any(c => c.WebPageGuid == firstGuid))
            {
                if (existingGuids.Add(tour.SystemFields.WebPageItemGUID))
                {
                    matchedTours.Add(tour);
                }
            }
        }

        if (matchedTours.Count < 8 && validDestinationGuids.Count > 1)
        {
            for (int i = 1; i < validDestinationGuids.Count; i++)
            {
                var extraGuid = validDestinationGuids[i];
                foreach (var tour in validTours)
                {
                    if (tour.TourPrimaryCountry != null && tour.TourPrimaryCountry.Any(c => c.WebPageGuid == extraGuid))
                    {
                        if (existingGuids.Add(tour.SystemFields.WebPageItemGUID))
                        {
                            matchedTours.Add(tour);
                        }
                    }
                }

                if (matchedTours.Count >= 8)
                {
                    break;
                }
            }
        }

        return new ArticlePostPageToursResult(matchedTours);
    }
}
