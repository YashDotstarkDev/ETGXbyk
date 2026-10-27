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
        var currentCategories = request.CurrentArticle.ArticleCategories?
            .Select(x => x.ToString().ToLower())
            .ToList() ?? new List<string>();

        var currentDestinations = request.CurrentArticle.ArticleDestinations?
            .Select(x => x.WebPageGuid.ToString().ToLower())
            .ToList() ?? new List<string>();

        // Query all published articles without arbitrary TopN(50) cutoff
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
            );

        var results = await tools.Executor
            .GetMappedWebPageResult<ETG.Core.Content.Article>(
                b,
                cancellationToken: cancellationToken);

        // Exclude current article from candidates
        var otherArticles = results
            .Where(a => a.SystemFields.WebPageItemID != request.CurrentArticle.SystemFields.WebPageItemID)
            .ToList();

        var selectedArticles = new List<ETG.Core.Content.Article>();
        var selectedItemIds = new HashSet<int>();

        void TryAdd(ETG.Core.Content.Article article)
        {
            if (selectedArticles.Count < 3 && selectedItemIds.Add(article.SystemFields.WebPageItemID))
            {
                selectedArticles.Add(article);
            }
        }

        // TIER 1: Matches BOTH Category AND Destination
        if (currentCategories.Any() && currentDestinations.Any())
        {
            foreach (var a in otherArticles)
            {
                var cats = a.ArticleCategories?.Select(x => x.ToString().ToLower()) ?? Enumerable.Empty<string>();
                var dests = a.ArticleDestinations?.Select(x => x.WebPageGuid.ToString().ToLower()) ?? Enumerable.Empty<string>();

                if (currentCategories.Intersect(cats).Any() && currentDestinations.Intersect(dests).Any())
                {
                    TryAdd(a);
                    if (selectedArticles.Count >= 3) break;
                }
            }
        }

        // TIER 2: Matches Same Destination
        if (selectedArticles.Count < 3 && currentDestinations.Any())
        {
            foreach (var a in otherArticles)
            {
                var dests = a.ArticleDestinations?.Select(x => x.WebPageGuid.ToString().ToLower()) ?? Enumerable.Empty<string>();
                if (currentDestinations.Intersect(dests).Any())
                {
                    TryAdd(a);
                    if (selectedArticles.Count >= 3) break;
                }
            }
        }

        // TIER 3: Matches Same Category
        if (selectedArticles.Count < 3 && currentCategories.Any())
        {
            foreach (var a in otherArticles)
            {
                var cats = a.ArticleCategories?.Select(x => x.ToString().ToLower()) ?? Enumerable.Empty<string>();
                if (currentCategories.Intersect(cats).Any())
                {
                    TryAdd(a);
                    if (selectedArticles.Count >= 3) break;
                }
            }
        }

        // FALLBACK: If fewer than 3, fill remaining slots with latest general articles
        if (selectedArticles.Count < 3)
        {
            foreach (var a in otherArticles)
            {
                TryAdd(a);
                if (selectedArticles.Count >= 3) break;
            }
        }

        return new ArticlePostPagesLatestResult(selectedArticles);
    }
}
