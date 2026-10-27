using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Content;
using ContentPage = ETG.Core.Content.Page;
using ETG.Core.Models;
using ETG.Core.Models.Tour;
using ETG.Web.Features.Article;
using ETG.Web.Features.Shared;
using ETG.Web.Infrastructure;
using ETG.Web.Rendering;
using ETG.Web.Services.Tour;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using MediaFile = ETG.Core.Content.MediaFile;

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
    IWebsiteChannelContext channelContext,
    ITourPricingService pricingService,
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

        
        TourListingViewModel? tourListing = null;

        if (articleDestinations.Any())
        {
            var toursResult = await mediator.Send(new ArticlePostPageToursQuery(articleDestinations, channelContext.WebsiteChannelName));
            var toursList = toursResult.Items.ToList();

            if (toursList.Any())
            {
                var bulkPrices = await pricingService.GetPricesBulkAsync(toursList);
                var tempCards = new List<(TourSummaryViewModel ViewModel, decimal SortPrice)>();

                foreach (var tour in toursList)
                {
                    // 1. Retrieve first image from SelectedImageFolders (Tour -> ImageFolder -> Image -> MediaFile)
                    var firstImage = tour.SelectedImageFolders?
                        .Where(f => f?.SelectedImages != null)
                        .SelectMany(f => f.SelectedImages)
                        .OfType<ETG.Image>()
                        .FirstOrDefault(img => img != null);

                    MediaFile? mediaFile = null;
                    if (firstImage != null)
                    {
                        mediaFile = firstImage.ImageFilePath?
                            .OfType<MediaFile>()
                            .FirstOrDefault()
                            ?? firstImage.ImageFilePath?.FirstOrDefault() as MediaFile;
                    }



                    // 2. Fallback to TourHeroImage if no image found in SelectedImageFolders
                    if (mediaFile == null && tour.TourHeroImage != null)
                    {
                        var heroItem = tour.TourHeroImage.FirstOrDefault();
                        mediaFile = heroItem switch
                        {
                            MediaFile mf => mf,
                            ETG.Image img => img.ImageFilePath?.OfType<MediaFile>().FirstOrDefault()
                                          ?? img.ImageFilePath?.FirstOrDefault() as MediaFile,
                            _ => null
                        };
                    }

                    string resolvedImageUrl = string.Empty;
                    var asset = mediaFile?.LegacyMediaFileAsset;
                    if (asset != null)
                    {
                        if (!string.IsNullOrWhiteSpace(asset.Url))
                        {
                            resolvedImageUrl = asset.Url.TrimStart('~');
                        }
                        else if (asset.Metadata != null)
                        {
                            resolvedImageUrl = $"/getContentAsset/{asset.Metadata.Identifier}/{asset.Metadata.Name}";
                        }
                    }

                    string finalInclusions = tour.TourPriceInclusions ?? string.Empty;

                    var priceInfo = (!string.IsNullOrWhiteSpace(tour.TourCode) && bulkPrices.TryGetValue(tour.TourCode, out var p))
                        ? p
                        : new TourPricingResult(0, 0);

                    var originalPriceText = priceInfo.OriginalPrice > 0
                        ? priceInfo.OriginalPrice.ToString("N0", CultureInfo.InvariantCulture)
                        : string.Empty;

                    var currentPriceText = priceInfo.CurrentPrice > 0
                        ? priceInfo.CurrentPrice.ToString("N0", CultureInfo.InvariantCulture)
                        : string.Empty;

                    var displayOriginalPrice = string.Empty;
                    if (priceInfo.CurrentPrice > 0 && priceInfo.OriginalPrice > 0 && priceInfo.CurrentPrice < priceInfo.OriginalPrice)
                    {
                        displayOriginalPrice = originalPriceText;
                    }

                    string imageAlt = !string.IsNullOrEmpty(tour.TourHeroImageCaption)
                        ? tour.TourHeroImageCaption
                        : (!string.IsNullOrEmpty(mediaFile?.LegacyMediaFileTitle)
                            ? mediaFile.LegacyMediaFileTitle
                            : (tour.TourName ?? string.Empty));

                    string badge = !string.IsNullOrWhiteSpace(tour.TourDestinationCity)
                        ? tour.TourDestinationCity.ToUpperInvariant()
                        : (!string.IsNullOrWhiteSpace(tour.TourDepartureCity)
                            ? tour.TourDepartureCity.ToUpperInvariant()
                            : string.Empty);

                    string tourPath = string.Empty;
                    try
                    {
                        var tourUrl = await urlRetriever.Retrieve(tour);
                        tourPath = tourUrl?.RelativePath ?? string.Empty;
                    }
                    catch { }

                    if (string.IsNullOrWhiteSpace(tourPath))
                    {
                        tourPath = !string.IsNullOrWhiteSpace(tour.SystemFields?.WebPageUrlPath)
                            ? "/" + tour.SystemFields.WebPageUrlPath.TrimStart('/')
                            : "#";
                    }

                    var cardVm = new TourSummaryViewModel
                    {
                        Name = tour.TourName ?? string.Empty,
                        TourCode = tour.TourCode ?? string.Empty,
                        Path = tourPath,
                        Description = tour.TourSummary ?? string.Empty,
                        HeroImageUrl = resolvedImageUrl,
                        HeroImageAlt = imageAlt,
                        Badge = badge,
                        DaysFromLabel = tour.TourNights > 0 ? $"{tour.TourNights + 1} DAYS FROM" : string.Empty,
                        CurrentPrice = string.IsNullOrEmpty(currentPriceText)
                            ? string.Empty
                            : $"{priceInfo.CurrencySymbol}{currentPriceText}",
                        OriginalPrice = displayOriginalPrice,
                        Currency = priceInfo.CurrencyCode,
                        PriceTypeLabel = !string.IsNullOrWhiteSpace(tour.TourPriceTypeLabel)
                            ? tour.TourPriceTypeLabel
                            : "Per person | twin share",
                        InclusionIcons = TourSummaryViewModel.ParseInclusions(finalInclusions)
                    };

                    // For sorting: cheapest first (tours with prices appear first, then unpriced)
                    decimal sortPrice = priceInfo.CurrentPrice > 0 ? priceInfo.CurrentPrice : decimal.MaxValue;
                    tempCards.Add((cardVm, sortPrice));
                }

                // Exactly like Kentico 13: Sort by lowest price ascending, take top 8
                var finalTourViewModels = tempCards
                    .OrderBy(x => x.SortPrice)
                    .Take(8)
                    .Select(x => x.ViewModel)
                    .ToList();

                if (finalTourViewModels.Any())
                {
                    tourListing = new TourListingViewModel
                    {
                        Heading = "Related Packages",
                        SectionId = "article-tours",
                        Tours = finalTourViewModels,
                        HideViewAllButton = false,
                        ViewAllUrl = "/tours",
                        ViewAllButtonLabel = "VIEW ALL"
                    };
                }
            }
        }

        
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
            metaService.SetMeta(metaFields, finalHeroImageUrl);
        }

        // Fetch ArticleLandingPage's PageHeading for breadcrumb
        try
        {
            var landingPageQuery = new ContentItemQueryBuilder()
                .ForContentType(ContentPage.CONTENT_TYPE_NAME, config => config
                    .ForWebsite(channelContext.WebsiteChannelName)
                    .Where(w => w.WhereEquals("WebPageUrlPath", "articles")));

            var landingPages = await queryExecutor.GetMappedWebPageResult<ContentPage>(landingPageQuery);
            var landingPage = landingPages.FirstOrDefault();
            if (landingPage != null && !string.IsNullOrEmpty(landingPage.PageHeading))
            {
                ViewData["ArticleLandingTitle"] = landingPage.PageHeading;
            }
        }
        catch { }

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
