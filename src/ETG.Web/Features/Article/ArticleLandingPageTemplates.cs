using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Content;
using ETG.Core.Models; 
using ETG.Web.Features;
using ETG.Web.Features.Article;
using ETG.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;


[assembly: RegisterWebPageRoute(
    contentTypeName: ETG.Core.Content.Page.CONTENT_TYPE_NAME,
    controllerType: typeof(ArticleLandingPageTemplateController),
     Path = "/articles",
    WebsiteChannelNames = new[] { "ETG" }
)]

namespace ETG.Web.Features.Article;

public class ArticleLandingPageTemplateProperties : IPageTemplateProperties { }

public class ArticleLandingPageTemplateController(
    IMediator mediator,
    IWebPageDataContextRetriever contextRetriever,
    IWebPageUrlRetriever urlRetriever,
    IWebsiteChannelContext channelContext,
    ITaxonomyRetriever taxonomyRetriever,
    IContentQueryExecutor queryExecutor,
    IConfiguration configuration,
    WebPageMetaService metaService) : Controller
{
    public async Task<IActionResult> Index()
    {
        if (!contextRetriever.TryRetrieve(out var data))
            return NotFound();

        // 1. Safely fetch the typed Page to get dynamic Hero content
        ETG.Core.Content.Page currentPage = null;
        try
        {
            var builder = new ContentItemQueryBuilder()
                .ForContentType(ETG.Core.Content.Page.CONTENT_TYPE_NAME, config => config
                    .ForWebsite(channelContext.WebsiteChannelName)
                    .WithLinkedItems(1)
                    .Where(w => w.WhereEquals("WebPageItemID", data.WebPage.WebPageItemID)));

            var result = await queryExecutor.GetMappedWebPageResult<ETG.Core.Content.Page>(builder);
            currentPage = result.FirstOrDefault();
        }
        catch
        {
            // Fallback gracefully
        }

        // Extract dynamic hero fields
        var pageTitle = currentPage?.PageHeading ?? "The Explorer's Log";
        var pageHeroCaption = currentPage?.PageHeroCaption ?? string.Empty;
        var heroAlt = currentPage?.PageHeroImageAltTag ?? string.Empty;
        var heroImageUrl = string.Empty;

        if (currentPage != null && currentPage.PageHeroImage != null)
        {
            var heroImage = currentPage.PageHeroImage.FirstOrDefault();
            if (heroImage is ETG.Core.Content.MediaFile mediaFile)
            {
                heroImageUrl = mediaFile.LegacyMediaFileAsset?.Url ?? string.Empty;
            }
        }

        // Only fetch top 3 featured articles (grid is fully Algolia-powered)
        var articlesResult = await mediator.Send(new ArticleLandingPageQuery(channelContext.WebsiteChannelName));
        var featuredArticles = articlesResult.Items.Take(3).ToList();
        var excludedObjectIds = featuredArticles
            .Select(a => a.SystemFields?.WebPageItemGUID.ToString() ?? string.Empty)
            .Where(id => !string.IsNullOrEmpty(id))
            .ToList();

        var articleViewModels = new List<ArticleViewModel>();
        foreach (var article in featuredArticles)
        {
            var vmArticle = await ArticleViewModel.CreateAsync(article, urlRetriever, taxonomyRetriever);
            articleViewModels.Add(vmArticle);
        }

        // Fetch popular destinations
        var destinationsResult = await mediator.Send(new PopularDestinationsQuery(channelContext.WebsiteChannelName, 7));
        var popularDestinations = new List<NavigationLinkViewModel>();
        foreach (var dest in destinationsResult.Items)
        {
            popularDestinations.Add(new NavigationLinkViewModel
            {
                Title = dest.DestinationName ?? "Destination",
                Url = dest.SystemFields?.WebPageUrlPath ?? "#"
            });
        }

        var vm = new ArticleLandingPageViewModel
        {
            Title = pageTitle,
            HeroImageUrl = heroImageUrl,
            HeroImageAltTag = heroAlt,
            Content = currentPage?.PageContent ?? string.Empty,
            PageHeroCaption = pageHeroCaption,

            // --- POPULATE THE NEW HERO MODEL FOR THE PARTIAL ---
            Hero = new HeroViewModel
            {
                Heading = pageTitle,
                HeadingText = pageHeroCaption,
                HeroImageUrl = heroImageUrl,
                HeroImageAltText = heroAlt,
                DisableOverlay = false
            },

            Articles = articleViewModels,
            PopularDestinations = popularDestinations,
            ExcludedObjectIds = excludedObjectIds,

            // --- Algolia config (read from appsettings, passed safely to view) ---
            AlgoliaAppId = configuration["CMSAlgoliaSearch:ApplicationId"] ?? string.Empty,
            AlgoliaSearchKey = configuration["CMSAlgoliaSearch:SearchKey"] ?? string.Empty,
            AlgoliaIndexName = configuration["CMSAlgoliaSearch:IndexArticle"] ?? "Article"
        };


        if (currentPage is IMetaDataSchema metaFields)
        {
            metaService.SetMeta(
                metaFields,
                heroImageUrl);
        }

        return View(
            "~/Features/Article/ArticleLandingPage_Default.cshtml",
            vm);
    }
}

// ─── ViewModels ────────────────────────────────────────────────────────────────

public class NavigationLinkViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class ArticleLandingPageViewModel
{
    public string Title { get; set; } = string.Empty;
    public string HeroImageUrl { get; set; } = string.Empty;
    public string HeroImageAltTag { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string PageHeroCaption { get; set; } = string.Empty;

    // ADDED: Model specifically for the Hero partial!
    public HeroViewModel? Hero { get; set; }

    public List<ArticleViewModel> Articles { get; set; } = new();
    public List<NavigationLinkViewModel> PopularDestinations { get; set; } = new();
    public List<string> ExcludedObjectIds { get; set; } = new();

    public string AlgoliaAppId { get; set; } = string.Empty;
    public string AlgoliaSearchKey { get; set; } = string.Empty;
    public string AlgoliaIndexName { get; set; } = string.Empty;
}
