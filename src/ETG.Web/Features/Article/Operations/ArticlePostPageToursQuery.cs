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

        var firstDestinationGuid = validDestinationGuids.First();

        // 2. Fetch tours with linked items matching the article destination
        // Exactly like ExperiencePageTemplates: query.ForWebsite(channelName) with IncludeWebPageData()
        var tourBuilder = new ContentItemQueryBuilder()
            .ForContentType(ETG.Core.Content.Tour.CONTENT_TYPE_NAME, q =>
            {
                q.ForWebsite(request.ChannelName);
                q.WithLinkedItems(4, options => options.IncludeWebPageData());
                q.Where(w =>
                    w.WhereLike(nameof(ETG.Core.Content.Tour.TourPrimaryCountry), $"%{firstDestinationGuid}%")
                );
            });

        var allTours = await tools.Executor.GetMappedWebPageResult<ETG.Core.Content.Tour>(
            tourBuilder,
            cancellationToken: cancellationToken);

        // 3. Filter out hidden upgrade tours (TourIsUpgrade == "2")
        var matchedTours = allTours
            .Where(tour =>
                (string.IsNullOrEmpty(tour.TourIsUpgrade) || tour.TourIsUpgrade != "2")
            )
            .ToList();

        // If fewer than 8 tours and there are other destinations, also fetch for remaining destinations
        if (matchedTours.Count < 8 && validDestinationGuids.Count > 1)
        {
            var existingGuids = matchedTours.Select(t => t.SystemFields.WebPageItemGUID).ToHashSet();
            foreach (var extraGuid in validDestinationGuids.Skip(1))
            {
                var extraBuilder = new ContentItemQueryBuilder()
                    .ForContentType(ETG.Core.Content.Tour.CONTENT_TYPE_NAME, q =>
                    {
                        q.ForWebsite(request.ChannelName);
                        q.WithLinkedItems(4, options => options.IncludeWebPageData());
                        q.Where(w =>
                            w.WhereLike(nameof(ETG.Core.Content.Tour.TourPrimaryCountry), $"%{extraGuid}%")
                        );
                    });

                var extraTours = await tools.Executor.GetMappedWebPageResult<ETG.Core.Content.Tour>(
                    extraBuilder,
                    cancellationToken: cancellationToken);

                foreach (var et in extraTours)
                {
                    if ((string.IsNullOrEmpty(et.TourIsUpgrade) || et.TourIsUpgrade != "2")
                        && existingGuids.Add(et.SystemFields.WebPageItemGUID))
                    {
                        matchedTours.Add(et);
                    }
                }

                if (matchedTours.Count >= 8) break;
            }
        }

        return new ArticlePostPageToursResult(matchedTours);
    }
}
