using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Operations;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ETG.Web.Features.Article;

public record ArticlePostPageToursQuery(List<string> Destinations) : IRequest<ArticlePostPageToursResult>;
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

        
        var destBuilder = new ContentItemQueryBuilder()
            .ForContentType(ETG.Core.Content.Destination.CONTENT_TYPE_NAME, q =>
            {
                // We know it's a GUID, so we search the GUID column!
                q.Where(w => w.WhereIn("ContentItemGUID", request.Destinations));
            });

        var destinations = await tools.Executor.GetMappedWebPageResult<ETG.Core.Content.Destination>(
            destBuilder,
            cancellationToken: cancellationToken);

      
        var featureTourIdentifiers = new List<string>();

        foreach (var dest in destinations)
        {
            if (dest.DestinationFeatureTours != null && dest.DestinationFeatureTours.Any())
            {
                var tours = dest.DestinationFeatureTours.Select(t => t.WebPageGuid.ToString());
                featureTourIdentifiers.AddRange(tours);
            }

        }


        if (!featureTourIdentifiers.Any())
        {
            return new ArticlePostPageToursResult([]);
        }

      
        var tourBuilder = new ContentItemQueryBuilder()
            .ForContentType(ETG.Core.Content.Tour.CONTENT_TYPE_NAME, q =>
            {
                
                q.WithLinkedItems(3);

               
                if (Guid.TryParse(featureTourIdentifiers.First(), out _))
                {
                    q.Where(w => w.WhereIn("ContentItemGUID", featureTourIdentifiers));
                }
                else
                {
                    q.Where(w => w.WhereIn("TourCode", featureTourIdentifiers));
                }

             
            });

        var results = await tools.Executor.GetMappedWebPageResult<ETG.Core.Content.Tour>(
            tourBuilder,
            cancellationToken: cancellationToken);

        return new ArticlePostPageToursResult(results.ToList());
    }
}
