using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Websites;
using ETG.Core.Content;
using ETG.Core.Operations;
using Kentico.Content.Web.Mvc;
using MediatR;

namespace ETG.Web.Features.Article;

public record ArticlePostPagesLatestQuery(
    ETG.Core.Content.Article CurrentArticle,
    string ChannelName) : IRequest<ArticlePostPagesLatestResult>;

public record ArticlePostPagesLatestResult(
    IReadOnlyList<ETG.Core.Content.Article> Items);

public class ArticlePostPagesLatestQueryHandler(
    WebPageQueryTools tools,
    IInfoProvider<TagInfo> tagProvider)
    : IRequestHandler<ArticlePostPagesLatestQuery, ArticlePostPagesLatestResult>
{
    public async Task<ArticlePostPagesLatestResult> Handle(
        ArticlePostPagesLatestQuery request,
        CancellationToken cancellationToken = default)
    {
        // MIGRATION FIX: Safely parse new WebPageRelatedItem and Guid types
        var currentCategories = request.CurrentArticle.ArticleCategories?
            .Select(x => x.ToString().ToLower())
            .ToList() ?? new List<string>();

        var currentDestinations = request.CurrentArticle.ArticleDestinations?
            .Select(x => x.WebPageGuid.ToString().ToLower())
            .ToList() ?? new List<string>();

        var b = new ContentItemQueryBuilder()
            .ForContentType(
                ETG.Core.Content.Article.CONTENT_TYPE_NAME,
                q => q
                    .ForWebsite(request.ChannelName)
                    .WithLinkedItems(1)
                    .OrderBy(
                        new OrderByColumn(
                            nameof(ETG.Core.Content.Article.ArticleDate),
                            OrderDirection.Descending))
                    .TopN(50)
            );

        var results = await tools.Executor
            .GetMappedWebPageResult<ETG.Core.Content.Article>(
                b,
                cancellationToken: cancellationToken);

        var otherArticles = results
            .Where(a => a.SystemFields.WebPageItemID != request.CurrentArticle.SystemFields.WebPageItemID)
            .ToList();

        var trulyRelatedArticles = otherArticles
            .Where(a =>
            {
                var relatedCats = a.ArticleCategories?
                    .Select(x => x.ToString().ToLower()) ?? Enumerable.Empty<string>();

                var relatedDests = a.ArticleDestinations?
                    .Select(x => x.WebPageGuid.ToString().ToLower()) ?? Enumerable.Empty<string>();

                bool sharesCategory = currentCategories.Intersect(relatedCats).Any();
                bool sharesDestination = currentDestinations.Intersect(relatedDests).Any();

                return sharesCategory || sharesDestination;
            })
            .ToList();

        var finalArticles = trulyRelatedArticles.Take(3).ToList();

        return new ArticlePostPagesLatestResult(finalArticles);
    }
}
