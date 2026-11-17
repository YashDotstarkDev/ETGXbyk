using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Websites;
using ETG.Core.Modules;
using ETG.Web.Rendering;
using Kentico.Content.Web.Mvc;
using System.Text.RegularExpressions;

namespace ETG.Web.Features;

public class ArticleViewModel
{
    public string Title { get; init; } = "";
    public string Summary { get; init; } = "";
    public string LinkPath { get; init; } = "";
    public ImageAssetViewModel? HeroImage { get; init; }
    public string HeroImageAltTag { get; init; } = "";

    public IEnumerable<Guid> AuthorIds { get; init; } = new List<Guid>();

    public DateTime ArticleDate { get; init; }
    public string Categories { get; init; } = "";
    public string Destinations { get; init; } = "";
    public string Experiences { get; init; } = "";
    public string TravelTypes { get; init; } = "";

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, (DateTime CachedAt, ArticleViewModel Vm)> s_articleVmCache = new();
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, string> s_destinationCache = new();

    public static void RegisterDestinationName(Guid guid, string name)
    {
        if (guid != Guid.Empty && !string.IsNullOrWhiteSpace(name))
        {
            s_destinationCache[guid] = name;
        }
    }

    public static async Task<ArticleViewModel> CreateAsync(
        ETG.Core.Content.Article article,
        IWebPageUrlRetriever urlRetriever,
        ITaxonomyRetriever taxonomyRetriever)
    {
        var articleGuid = article.SystemFields.WebPageItemGUID;
        if (articleGuid != Guid.Empty && s_articleVmCache.TryGetValue(articleGuid, out var cached) &&
            (DateTime.UtcNow - cached.CachedAt).TotalMinutes < 30)
        {
            return cached.Vm;
        }

        var heroImage = article.ArticleHeroImage?
            .OfType<ETG.Core.Content.MediaFile>()
            .FirstOrDefault();

        var url = await urlRetriever.Retrieve(article);

        // MIGRATION FIX: Pass the new IEnumerable<Guid> type safely with cached category lookup
        var categoryNames = GetCategoryNames(article.ArticleCategories);

        // MIGRATION FIX: Extract Guids from WebPageRelatedItem and look up from memory cache
        var destinationIds = article.ArticleDestinations?.Select(d => d.WebPageGuid).ToList() ?? new List<Guid>();

        var destinationNames = destinationIds.Count > 0
            ? string.Join(", ", destinationIds.Select(id => s_destinationCache.TryGetValue(id, out var name) ? name : null).Where(n => !string.IsNullOrWhiteSpace(n)))
            : string.Empty;

        var vm = new ArticleViewModel
        {
            Title = article.ArticleTitle ?? "",
            Summary = article.ArticleSummary ?? "",
            LinkPath = url?.RelativePath ?? string.Empty,
            HeroImage = heroImage is null
                ? null
                : ImageAssetViewModel.Create(heroImage),
            HeroImageAltTag = article.ArticleHeroImageAltTag ?? "",

            AuthorIds = article.ArticleAuthor ?? Enumerable.Empty<Guid>(),

            ArticleDate = article.ArticleDate,
            Categories = categoryNames,
            Destinations = destinationNames,

            // Safely map the other relations
            Experiences = string.Join(",", article.ArticleExperiences?.Select(e => e.WebPageGuid.ToString()) ?? Enumerable.Empty<string>()),
            TravelTypes = string.Join(",", article.ArticleTravelTypes?.Select(t => t.WebPageGuid.ToString()) ?? Enumerable.Empty<string>())
        };

        if (articleGuid != Guid.Empty)
        {
            s_articleVmCache[articleGuid] = (DateTime.UtcNow, vm);
        }

        return vm;
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, string> s_categoryCache = new();
    private static DateTime s_lastCategoryCacheRefresh = DateTime.MinValue;
    private static readonly object s_cacheLock = new();

    // PERFORMANCE FIX: Cached category lookup avoids synchronous WAN database round trips
    private static string GetCategoryNames(IEnumerable<Guid>? categoryIds)
    {
        if (categoryIds == null || !categoryIds.Any())
        {
            return string.Empty;
        }

        EnsureCategoriesLoaded();

        var categoryNames = new List<string>();
        foreach (var id in categoryIds)
        {
            if (s_categoryCache.TryGetValue(id, out var name))
            {
                categoryNames.Add(name);
            }
        }

        return string.Join(", ", categoryNames);
    }

    private static void EnsureCategoriesLoaded()
    {
        if (DateTime.UtcNow < s_lastCategoryCacheRefresh.AddMinutes(60) && !s_categoryCache.IsEmpty)
        {
            return;
        }

        lock (s_cacheLock)
        {
            if (DateTime.UtcNow < s_lastCategoryCacheRefresh.AddMinutes(60) && !s_categoryCache.IsEmpty)
            {
                return;
            }

            try
            {
                var categories = ArticleCategoryInfo.Provider.Get().ToList();
                s_categoryCache.Clear();
                foreach (var cat in categories)
                {
                    if (cat != null && !string.IsNullOrWhiteSpace(cat.ArticleCategoryName))
                    {
                        s_categoryCache[cat.ArticleCategoryGuid] = cat.ArticleCategoryName;
                    }
                }
                s_lastCategoryCacheRefresh = DateTime.UtcNow;
            }
            catch
            {
                // Graceful fallback
            }
        }
    }
}

public class ArticlePageViewModel
{
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public string ViewAllUrl { get; init; } = "/articles";
    public IEnumerable<ArticleViewModel> RelatedArticles { get; init; } = [];
}
