using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Content;
using ETG.Core.Models;
using ETG.Core.Models.Tour;
using ETG.Web.Features.Article;
using ETG.Web.Features.Shared;
using ETG.Web.Infrastructure;
using ETG.Web.Rendering;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[assembly: RegisterPageTemplate(
    identifier: "ETG.ArticlePostPage_Default",
    name: "Article Page - Default",
    propertiesType: typeof(ArticlePostPageTemplateProperties),
    customViewName: "~/Features/Article/ArticlePostPage_Default.cshtml",
    ContentTypeNames = [Article.CONTENT_TYPE_NAME],
    Description = "",
    IconClass = ""
)]

[assembly: RegisterWebPageRoute(
    contentTypeName: Article.CONTENT_TYPE_NAME,
    controllerType: typeof(ArticlePostPageTemplateController)
)]

namespace ETG.Web.Features.Article;

public class ArticlePostPageTemplateProperties : IPageTemplateProperties { }

public class ArticlePostPageTemplateController(
    IMediator mediator,
    IWebPageDataContextRetriever contextRetriever,
    IWebPageUrlRetriever urlRetriever,
    ITaxonomyRetriever taxonomyRetriever,
    IContentQueryExecutor queryExecutor,
    WebPageMetaService metaService) : Controller

{
    public async Task<ActionResult> Index()
    {
        if (!contextRetriever.TryRetrieve(out var data))
            return NotFound();

        var articlePage = await mediator.Send(
            new ArticlePostPageQuery((RoutedWebPage)data.WebPage));

        if (articlePage == null)
            return NotFound();

        var authorQuery = new ContentItemQueryBuilder()
            .ForWebPage((RoutedWebPage)data.WebPage);

        var rawResult = await queryExecutor.GetWebPageResult(authorQuery, container => container, new ContentQueryExecutionOptions { ForPreview = true });

        var rawArticle = rawResult.FirstOrDefault();

        List<Guid> rawAuthorGuids = new List<Guid>();
        if (rawArticle != null && rawArticle.TryGetValue<object>("ArticleAuthor", out var authorObj))
        {
            if (authorObj is string strVal)
            {
                rawAuthorGuids = ExtractGuids(strVal);
            }
            else if (authorObj is IEnumerable<Guid> guidList)
            {
                rawAuthorGuids = guidList.ToList();
            }
        }

        string resolvedAuthorName = ResolveAuthorNames(rawAuthorGuids);

        DateTime modifiedDate = GetLastModifiedDate(articlePage.SystemFields.WebPageItemID);
        string finalModifiedDateIso = modifiedDate != DateTime.MinValue
            ? modifiedDate.ToString("yyyy-MM-dd")
            : (articlePage.ArticleDate != default ? articlePage.ArticleDate.ToString("yyyy-MM-dd") : string.Empty);

        var heroImage = articlePage.ArticleHeroImage?.FirstOrDefault();
        string finalHeroImageUrl = heroImage is MediaFile mediaFileHero
            ? mediaFileHero.LegacyMediaFileAsset?.Url ?? string.Empty
            : string.Empty;

        // Remember to uncomment this if your Article data is fully migrated in the database!
        var relatedResult = await mediator.Send(
            new ArticlePostPagesLatestQuery(articlePage, "ETG"));

        var relatedArticles = new List<ArticleViewModel>();
        foreach (var related in relatedResult.Items)
        {
            var vmArticle = await ArticleViewModel.CreateAsync(related, urlRetriever, taxonomyRetriever);
            relatedArticles.Add(vmArticle);
        }


        var articleDestinations = articlePage.ArticleDestinations?
            .Select(d => d.WebPageGuid.ToString())
            .ToList() ?? new List<string>();

        var toursResult = await mediator.Send(new ArticlePostPageToursQuery(articleDestinations));
        var tourViewModels = new List<TourSummaryViewModel>();

        foreach (var tour in toursResult.Items)
        {
            var tourHero = tour.TourHeroImage?.FirstOrDefault() as MediaFile;
            string finalInclusions = tour.TourPriceInclusions ?? string.Empty;
            int dynamicLowestPrice = 0;
            var allPrices = new List<int>();

            // MIGRATION FIX: Property name changed from SelectTourPricingFolders to SelectedTourPricingFolders
            if (tour.SelectedTourPricingFolders != null)
            {
                foreach (var folder in tour.SelectedTourPricingFolders)
                {
                    if (folder != null && folder.SelectedTourPricings != null)
                    {
                        foreach (var pricingObj in folder.SelectedTourPricings)
                        {
                            if (pricingObj is TourPricing pricing)
                            {
                                if (pricing.TourPricingOverridePrice > 0) allPrices.Add(pricing.TourPricingOverridePrice);
                                if (pricing.TourPricingEconomyPrice1 > 0) allPrices.Add(pricing.TourPricingEconomyPrice1);
                                if (pricing.TourPricingEconomyPrice2 > 0) allPrices.Add(pricing.TourPricingEconomyPrice2);
                                if (pricing.TourPricingEconomyPrice3 > 0) allPrices.Add(pricing.TourPricingEconomyPrice3);
                                if (pricing.TourPricingEconomyPrice4 > 0) allPrices.Add(pricing.TourPricingEconomyPrice4);
                            }
                        }
                    }
                }
            }

            if (allPrices.Any()) dynamicLowestPrice = allPrices.Min();

            string finalCurrentPrice = dynamicLowestPrice > 0 ? $"${dynamicLowestPrice:#,##0}" : "No Price Found";

            tourViewModels.Add(new TourSummaryViewModel
            {
                Name = tour.TourName ?? string.Empty,
                TourCode = tour.TourCode ?? string.Empty,
                Path = tour.SystemFields?.WebPageUrlPath ?? "#",
                Description = tour.TourSummary ?? string.Empty,
                HeroImageUrl = tourHero?.LegacyMediaFileAsset?.Url ?? string.Empty,
                HeroImageAlt = !string.IsNullOrEmpty(tour.TourHeroImageCaption) ? tour.TourHeroImageCaption : (tour.TourName ?? string.Empty),
                Badge = tour.TourDestinationCity?.ToUpper() ?? string.Empty,
                DaysFromLabel = tour.TourNights > 0 ? $"{tour.TourNights + 1} DAYS FROM" : string.Empty,
                OriginalPrice = string.Empty,
                CurrentPrice = finalCurrentPrice,
                Currency = !string.IsNullOrEmpty(tour.TourPriceCurrency) ? tour.TourPriceCurrency : "AUD",
                PriceTypeLabel = tour.TourPriceTypeLabel ?? string.Empty,
                InclusionIcons = TourSummaryViewModel.ParseInclusions(finalInclusions)
            });
        }

        var tourListing = new TourListingViewModel
        {
            Heading = "Related Packages",
            SectionId = "article-tours",
            Tours = tourViewModels,
            HideViewAllButton = false,
            ViewAllUrl = "/tours",
            ViewAllButtonLabel = "VIEW ALL"
        };

        var vm = new ArticlePostPageViewModel
        {
            Title = articlePage.ArticleTitle ?? string.Empty,
            Summary = articlePage.ArticleSummary ?? string.Empty,
            HeroImageUrl = finalHeroImageUrl,
            HeroImageAltTag = articlePage.ArticleHeroImageAltTag ?? string.Empty,
            Hero = new HeroViewModel
            {
                HeroImageUrl = finalHeroImageUrl,
                HeroImageAltText = articlePage.ArticleHeroImageAltTag ?? string.Empty,
                Heading = articlePage.ArticleTitle ?? string.Empty,
                DisableOverlay = false
            },
            AuthorName = resolvedAuthorName,
            AuthorPhotoUrl = null,
            AuthorBiographyHtml = string.Empty,
            DisplayPublishDate = articlePage.ArticleDate != default
                ? articlePage.ArticleDate.ToString("dd MMM yyyy")
                : string.Empty,
            PublishDateIso = articlePage.ArticleDate != default
                ? articlePage.ArticleDate.ToString("yyyy-MM-dd")
                : string.Empty,
            ModifiedDateIso = finalModifiedDateIso,

            Categories = GetCategoryNames(articlePage.ArticleCategories),

            Destinations = articleDestinations,
            RelatedArticles = relatedArticles,
            TourListing = tourListing
        };

        if (articlePage is IMetaDataSchema metaFields)
        {
            metaService.SetMeta(
                metaFields,
                finalHeroImageUrl);
        }
        return View("~/Features/Article/ArticlePostPage_Default.cshtml", vm);
    }

    private List<Guid> ExtractGuids(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return new List<Guid>();
        return System.Text.RegularExpressions.Regex.Matches(
                value,
                @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}")
            .Select(match => Guid.Parse(match.Value))
            .Distinct()
            .ToList();
    }

    private string ResolveAuthorNames(IEnumerable<Guid>? guids)
    {
        if (guids == null || !guids.Any()) return string.Empty;
        var names = new List<string>();

        try
        {
            var items = new CMS.DataEngine.ObjectQuery("cms.contentitem").WhereIn("ContentItemGUID", guids.ToArray()).ToList();
            foreach (var item in items)
            {
                string name = item.GetStringValue("ContentItemName", "");
                if (!string.IsNullOrWhiteSpace(name)) names.Add(name.Trim());
            }

            if (!names.Any())
            {
                var users = new CMS.DataEngine.ObjectQuery("cms.user").WhereIn("UserGUID", guids.ToArray()).ToList();
                foreach (var user in users)
                {
                    string name = user.GetStringValue("FullName", "");
                    if (!string.IsNullOrWhiteSpace(name)) names.Add(name.Trim());
                }
            }
        }
        catch { }

        return string.Join(" and ", names.Distinct());
    }


    private List<string> GetCategoryNames(IEnumerable<Guid>? categoryIds)
    {
        if (categoryIds == null || !categoryIds.Any()) return new List<string>();

        var categoryNames = new List<string>();
        try
        {
            foreach (var categoryGuid in categoryIds)
            {
                var category = ETG.Core.Modules.ArticleCategoryInfo.Provider.Get()
                    .WhereEquals(nameof(ETG.Core.Modules.ArticleCategoryInfo.ArticleCategoryGuid), categoryGuid)
                    .TopN(1)
                    .FirstOrDefault();

                if (category != null && !string.IsNullOrWhiteSpace(category.ArticleCategoryName))
                {
                    categoryNames.Add(category.ArticleCategoryName);
                }
            }
        }
        catch { }
        return categoryNames;
    }

    private DateTime GetLastModifiedDate(int webPageItemId)
    {
        try
        {
            var page = new CMS.DataEngine.ObjectQuery("cms.webpageitem")
                .WhereEquals("WebPageItemID", webPageItemId)
                .FirstOrDefault();

            if (page != null)
            {
                int contentItemId = page.GetIntegerValue("WebPageItemContentItemID", 0);

                var metadata = new CMS.DataEngine.ObjectQuery("cms.contentitemlanguagemetadata")
                    .WhereEquals("ContentItemLanguageMetadataContentItemID", contentItemId)
                    .FirstOrDefault();

                if (metadata != null)
                {
                    return metadata.GetDateTimeValue("ContentItemLanguageMetadataModifiedWhen", DateTime.MinValue);
                }
            }
        }
        catch { }
        return DateTime.MinValue;
    }
}

// ─── ViewModels ───────────────────────────────────────────────────────────────

public class ArticlePostPageViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string HeroImageUrl { get; set; } = string.Empty;
    public string HeroImageAltTag { get; set; } = string.Empty;
    public string DisplayPublishDate { get; set; } = string.Empty;
    public string PublishDateIso { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorPhotoUrl { get; set; }
    public string AuthorBiographyHtml { get; set; } = string.Empty;
    public System.Collections.Generic.List<string> Categories { get; set; } = new();
    public System.Collections.Generic.List<string> Destinations { get; set; } = new();
    public string DestinationsText => string.Join(", ", Destinations);
    public string CategoriesText => string.Join(", ", Categories);
    public string JsonSchema { get; set; } = string.Empty;
    public string ModifiedDateIso { get; set; } = string.Empty;
    public System.Collections.Generic.List<ArticleViewModel> RelatedArticles { get; set; } = new();
    public TourListingViewModel? TourListing { get; set; }
    public HeroViewModel? Hero { get; set; }
}
