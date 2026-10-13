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

    public static async Task<ArticleViewModel> CreateAsync(
        ETG.Core.Content.Article article,
        IWebPageUrlRetriever urlRetriever,
        ITaxonomyRetriever taxonomyRetriever)
    {
        var heroImage = article.ArticleHeroImage?
            .OfType<ETG.Core.Content.MediaFile>()
            .FirstOrDefault();

        var url = await urlRetriever.Retrieve(article);

        // MIGRATION FIX: Pass the new IEnumerable<Guid> type safely
        var categoryNames = GetCategoryNames(article.ArticleCategories);

        // MIGRATION FIX: Extract Guids from WebPageRelatedItem
        var destinationIds = article.ArticleDestinations?.Select(d => d.WebPageGuid).ToList() ?? new List<Guid>();

        var destinationTags = destinationIds.Any()
            ? await taxonomyRetriever.RetrieveTags(destinationIds.ToArray(), "en")
            : Enumerable.Empty<Tag>();

        var destinationNames = string.Join(
            ", ",
            destinationTags
                .OrderBy(tag => destinationIds.IndexOf(tag.Identifier))
                .Select(tag => tag.Title)
                .Where(title => !string.IsNullOrWhiteSpace(title)));

        return new ArticleViewModel
        {
            Title = article.ArticleTitle ?? "",
            Summary = article.ArticleSummary ?? "",
            LinkPath = url.RelativePath,
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
    }

    // MIGRATION FIX: Changed parameter to accept IEnumerable<Guid> directly!
    private static string GetCategoryNames(IEnumerable<Guid>? categoryIds)
    {
        if (categoryIds == null || !categoryIds.Any())
        {
            return string.Empty;
        }

        var categoryNames = new List<string>();

        try
        {
            var categories = ArticleCategoryInfo.Provider
                .Get()
                .WhereIn(nameof(ArticleCategoryInfo.ArticleCategoryGuid), categoryIds.ToArray())
                .ToList();

            foreach (var category in categories)
            {
                if (!string.IsNullOrWhiteSpace(category.ArticleCategoryName))
                {
                    categoryNames.Add(category.ArticleCategoryName);
                }
            }
        }
        catch
        {
            // Graceful fallback: do not crash if category lookup fails
        }

        return string.Join(", ", categoryNames);
    }
}

public class ArticlePageViewModel
{
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public string ViewAllUrl { get; init; } = "/articles";
    public IEnumerable<ArticleViewModel> RelatedArticles { get; init; } = [];
}
