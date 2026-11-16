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

        var selectedArticles = new List<ETG.Core.Content.Article>();
        var selectedItemIds = new HashSet<int> { request.CurrentArticle.SystemFields.WebPageItemID };

        // TIER 1: Matches BOTH Category AND Destination
        if (selectedArticles.Count < 3 && currentCategories.Any() && currentDestinations.Any())
        {
            var tier1Articles = await QueryArticlesByFilters(
                request.ChannelName,
                currentCategories,
                currentDestinations,
                bothFilters: true,
                topN: 3,
                cancellationToken);

            AddArticles(tier1Articles, selectedArticles, selectedItemIds);
        }

        // TIER 2: Matches Same Destination
        if (selectedArticles.Count < 3 && currentDestinations.Any())
        {
            var tier2Articles = await QueryArticlesByFilters(
                request.ChannelName,
                categories: null,
                currentDestinations,
                bothFilters: false,
                topN: 3 - selectedArticles.Count,
                cancellationToken);

            AddArticles(tier2Articles, selectedArticles, selectedItemIds);
        }

        // TIER 3: Matches Same Category
        if (selectedArticles.Count < 3 && currentCategories.Any())
        {
            var tier3Articles = await QueryArticlesByFilters(
                request.ChannelName,
                currentCategories,
                destinations: null,
                bothFilters: false,
                topN: 3 - selectedArticles.Count,
                cancellationToken);

            AddArticles(tier3Articles, selectedArticles, selectedItemIds);
        }

        // FALLBACK: If fewer than 3, fill remaining slots with latest general articles
        if (selectedArticles.Count < 3)
        {
            var fallbackArticles = await QueryLatestArticles(
                request.ChannelName,
                topN: 3 - selectedArticles.Count,
                cancellationToken);

            AddArticles(fallbackArticles, selectedArticles, selectedItemIds);
        }

        return new ArticlePostPagesLatestResult(selectedArticles);
    }

    private async Task<List<ETG.Core.Content.Article>> QueryArticlesByFilters(
        string channelName,
        List<string>? categories,
        List<string>? destinations,
        bool bothFilters,
        int topN,
        CancellationToken cancellationToken)
    {
        if (topN <= 0)
        {
            return [];
        }

        var builder = new ContentItemQueryBuilder()
            .ForContentType(
                ETG.Core.Content.Article.CONTENT_TYPE_NAME,
                q => q
                    .ForWebsite(channelName)
                    .WithLinkedItems(1)
                    .TopN(topN + 10) // Get extra to account for current article filtering
                    .OrderBy(
                        new OrderByColumn(
                            nameof(ETG.Core.Content.Article.ArticleDate),
                            OrderDirection.Descending))
                    .Where(where =>
                    {
                        if (bothFilters && categories?.Any() == true && destinations?.Any() == true)
                        {
                            // Both category AND destination
                            where.WhereIn(nameof(ETG.Core.Content.Article.ArticleCategories), categories.ToArray());
                            
                            foreach (var destGuid in destinations)
                            {
                                where.Or().WhereLike(
                                    nameof(ETG.Core.Content.Article.ArticleDestinations),
                                    $"%{destGuid}%");
                            }
                        }
                        else if (destinations?.Any() == true)
                        {
                            // Destination only
                            foreach (var destGuid in destinations)
                            {
                                where.Or().WhereLike(
                                    nameof(ETG.Core.Content.Article.ArticleDestinations),
                                    $"%{destGuid}%");
                            }
                        }
                        else if (categories?.Any() == true)
                        {
                            // Category only
                            where.WhereIn(nameof(ETG.Core.Content.Article.ArticleCategories), categories.ToArray());
                        }
                    })
            );

        var results = await tools.Executor.GetMappedWebPageResult<ETG.Core.Content.Article>(
            builder,
            cancellationToken: cancellationToken);

        return results.ToList();
    }

    private async Task<List<ETG.Core.Content.Article>> QueryLatestArticles(
        string channelName,
        int topN,
        CancellationToken cancellationToken)
    {
        if (topN <= 0)
        {
            return [];
        }

        var builder = new ContentItemQueryBuilder()
            .ForContentType(
                ETG.Core.Content.Article.CONTENT_TYPE_NAME,
                q => q
                    .ForWebsite(channelName)
                    .WithLinkedItems(1)
                    .TopN(topN + 5) // Get extra to account for current article filtering
                    .OrderBy(
                        new OrderByColumn(
                            nameof(ETG.Core.Content.Article.ArticleDate),
                            OrderDirection.Descending))
            );

        var results = await tools.Executor.GetMappedWebPageResult<ETG.Core.Content.Article>(
            builder,
            cancellationToken: cancellationToken);

        return results.ToList();
    }

    private static void AddArticles(
        List<ETG.Core.Content.Article> candidates,
        List<ETG.Core.Content.Article> selected,
        HashSet<int> selectedItemIds)
    {
        foreach (var article in candidates)
        {
            if (selected.Count >= 3)
            {
                break;
            }

            if (selectedItemIds.Add(article.SystemFields.WebPageItemID))
            {
                selected.Add(article);
            }
        }
    }
}
