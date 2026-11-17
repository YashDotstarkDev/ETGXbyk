using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CMS.ContentEngine;
using CMS.Helpers;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Content;
using ETG.Core.Models;
using ETG.Core.Models.GoogleReviews;
using ETG.Web.Features.Destinations.Operations;
using ETG.Web.Infrastructure;
using ETG.Web.Rendering;
using ETG.Web.Services.GoogleReviews;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using ArticleContent = ETG.Core.Content.Article;

[assembly: RegisterPageTemplate(
    identifier: "ETG.DestinationPostPage_Default",
    name: "Destination Post Page - Default",
    propertiesType:
        typeof(ETG.Web.Features.DestinationPostPage.DestinationPostPageTemplateProperties),
    customViewName: "~/Features/Destinations/DestinationPostPage_Default.cshtml",
    ContentTypeNames = new[] { Destination.CONTENT_TYPE_NAME }
)]

[assembly: RegisterWebPageRoute(
    contentTypeName: Destination.CONTENT_TYPE_NAME,
    controllerType:
        typeof(ETG.Web.Features.DestinationPostPage.DestinationPostPageTemplateController)
)]

namespace ETG.Web.Features.DestinationPostPage;

public sealed class DestinationPostPageTemplateProperties
    : IPageTemplateProperties
{
}

public sealed class DestinationPostPageTemplateController : Controller
{
    private readonly IMediator mediator;
    private readonly IWebPageDataContextRetriever contextRetriever;
    private readonly IContentQueryExecutor contentQueryExecutor;
    private readonly IWebPageUrlRetriever urlRetriever;
    private readonly IWebsiteChannelContext websiteChannelContext;
    private readonly ITaxonomyRetriever taxonomyRetriever;
    private readonly WebPageMetaService metaService;
    private readonly IGoogleReviewsService reviewsService;
    private readonly IProgressiveCache? cache;

    public DestinationPostPageTemplateController(
        IMediator mediator,
        IWebPageDataContextRetriever contextRetriever,
        IContentQueryExecutor contentQueryExecutor,
        IWebPageUrlRetriever urlRetriever,
        IWebsiteChannelContext websiteChannelContext,
        ITaxonomyRetriever taxonomyRetriever,
        WebPageMetaService metaService,
        IGoogleReviewsService reviewsService,
        IProgressiveCache? cache = null)
    {
        this.mediator = mediator;
        this.contextRetriever = contextRetriever;
        this.contentQueryExecutor = contentQueryExecutor;
        this.urlRetriever = urlRetriever;
        this.websiteChannelContext = websiteChannelContext;
        this.taxonomyRetriever = taxonomyRetriever;
        this.metaService = metaService;
        this.reviewsService = reviewsService;
        this.cache = cache;
    }

    public async Task<IActionResult> Index()
    {
        if (!contextRetriever.TryRetrieve(out var data))
        {
            return NotFound();
        }

        var destination = await mediator.Send(
            new DestinationPostPageQuery(data.WebPage));

        if (destination is null)
        {
            return NotFound();
        }


        ArticleViewModel.RegisterDestinationName(destination.SystemFields.WebPageItemGUID, destination.DestinationName);

        // Added: inspect the selected folders and their linked items.
        var accordionFolders = destination.SelectedAccordionItemFolders?
            .ToList() ?? new List<AccordionItemFolder>();

        foreach (var folder in accordionFolders)
        {
            var folderName = folder.DocumentName;

            var accordionItems = folder.SelectedAccordionItems?
                .ToList() ?? new List<AccordionItem>();

            var itemCount = accordionItems.Count;

            // Set a breakpoint on the line above and step over it.
            // Inspect folderName, itemCount, and accordionItems.
        }

        var pathBase = Request.PathBase.ToString();

        async Task<DestinationPostPageViewModel> BuildViewModelAsync()
        {
            // Start independent tasks early and concurrently
            var destinationUrlTask = urlRetriever.Retrieve(destination);
            var reviewsTask = reviewsService.GetReviewsAsync();

            // Existing: articles for current destination and its parent.
            var destinationGuids = new HashSet<Guid>
            {
                destination.SystemFields.WebPageItemGUID
            };

            var parentPageId = destination.SystemFields.WebPageItemParentID;
            var parentDestination = await GetParentDestinationAsync(parentPageId);

            if (parentDestination is not null)
            {
                ArticleViewModel.RegisterDestinationName(parentDestination.SystemFields.WebPageItemGUID, parentDestination.DestinationName);
                destinationGuids.Add(parentDestination.SystemFields.WebPageItemGUID);
            }

            var relatedArticlesTask = mediator.Send(
                new GetDestinationArticlesQuery(
                    websiteChannelContext.WebsiteChannelName,
                    destinationGuids),
                HttpContext.RequestAborted);

            await Task.WhenAll(destinationUrlTask, reviewsTask, relatedArticlesTask);

            var destinationUrl = await destinationUrlTask;
            var reviews = await reviewsTask;
            var relatedArticles = await relatedArticlesTask;

            var relatedArticleViewModels = await Task.WhenAll(
                relatedArticles.Select(article =>
                    ArticleViewModel.CreateAsync(
                        article,
                        urlRetriever,
                        taxonomyRetriever)));

            var heroImage = destination.DestinationHeroImage?
                .OfType<MediaFile>()
                .FirstOrDefault();

            var foregroundImage = destination.DestinationHeroForegroundImage?
                .OfType<MediaFile>()
                .FirstOrDefault();

            var destinationPath = (destinationUrl?.RelativePath ?? string.Empty)
                .Split(new[] { '?', '#' }, 2)[0]
                .TrimEnd('/');

            var destinationAlias = destinationPath
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .LastOrDefault();

            var contactUs = new ContactUsViewModel
            {
                CTAUrl = string.IsNullOrWhiteSpace(destinationAlias)
                    ? string.Empty
                    : $"{pathBase}/destinations/enquire/{destinationAlias}",
                CTABackgroundImage = destination.DestinationHeroImage?.OfType<MediaFile>()
                    .FirstOrDefault()?.LegacyMediaFileAsset?.Url ?? string.Empty
            };

            return new DestinationPostPageViewModel
            {
                CtaButtonText = destination.CtaButtonText,
                CtaButtonUrl = destination.CtaButtonUrl,

                DestinationName =
                    destination.DestinationName ?? string.Empty,

                DestinationHeroImage = heroImage is null
                    ? null
                    : ImageAssetViewModel.Create(heroImage),

                HeroForegroundImage = foregroundImage is null
                    ? null
                    : ImageAssetViewModel.Create(foregroundImage),

                DestinationHeroDisableOverlay =
                    destination.DestinationHeroDisableOverlay,

                DestinationHeroImageAltTag =
                    destination.DestinationHeroImageAltTag ?? string.Empty,

                DestinationCampaignTitle =
                    destination.DestinationCampaignTitle ?? string.Empty,

                DestinationHeading =
                    destination.DestinationHeading ?? string.Empty,

                DestinationJsonSchema =
                    destination.DestinationJsonSchema ?? string.Empty,

                DestinationSummary =
                    destination.DestinationSummary ?? string.Empty,

                DestinationDetail =
                    destination.DestinationDetail ?? string.Empty,

                DestinationWhenToVisit =
                    destination.DestinationWhenToVisit ?? string.Empty,

                DestinationStickyModel =
                    CreateDestinationStickyModel(destination, contactUs, reviews),

                SelectedAccordionItemFolders =
                    destination.SelectedAccordionItemFolders
                    ?? Enumerable.Empty<AccordionItemFolder>(),

                SelectedImageFolders =
                    destination.SelectedImageFolders
                    ?? Enumerable.Empty<ImageFolder>(),

                SelectedImageTileCtaFolders =
                    destination.SelectedImageTileCtaFolders
                    ?? Enumerable.Empty<ImageTileCtaFolder>(),

                RelatedArticles = relatedArticleViewModels,
                DestinationRelatedArticlesHeader = destination.DestinationRelatedArticlesHeader ?? string.Empty,
                CTA = contactUs,
                Reviews = reviews
            };
        }

        DestinationPostPageViewModel viewModel;

        if (cache != null)
        {
            var cacheKey = $"DestinationPostPageVM_{websiteChannelContext.WebsiteChannelName}_{data.WebPage.WebPageItemID}_{System.Globalization.CultureInfo.CurrentUICulture.Name}";

            viewModel = await cache.LoadAsync(async cs =>
            {
                cs.GetCacheDependency = () => CacheHelper.GetCacheDependency(new[]
                {
                    $"webpageitem|byid|{data.WebPage.WebPageItemID}",
                    $"webpageitem|bychannel|{websiteChannelContext.WebsiteChannelName}|bycontenttype|{Destination.CONTENT_TYPE_NAME}",
                    $"webpageitem|bychannel|{websiteChannelContext.WebsiteChannelName}|all",
                    "webpageitem|all",
                    CacheHelper.GetCacheItemName("contentitem", "bycontenttype", Destination.CONTENT_TYPE_NAME),
                    CacheHelper.GetCacheItemName("contentitem", "bycontenttype", AccordionItemFolder.CONTENT_TYPE_NAME),
                    CacheHelper.GetCacheItemName("contentitem", "bycontenttype", AccordionItem.CONTENT_TYPE_NAME),
                    CacheHelper.GetCacheItemName("contentitem", "bycontenttype", MediaFile.CONTENT_TYPE_NAME),
                    CacheHelper.GetCacheItemName("contentitem", "bycontenttype", ArticleContent.CONTENT_TYPE_NAME)
                });
                return await BuildViewModelAsync();
            }, new CacheSettings(30, cacheKey));
        }
        else
        {
            viewModel = await BuildViewModelAsync();
        }

        if (destination is IMetaDataSchema metaFields)
        {
            var heroImg = destination.DestinationHeroImage?
                .OfType<MediaFile>()
                .FirstOrDefault();

            metaService.SetMeta(
                metaFields,
                heroImg?.LegacyMediaFileAsset?.Url);
        }

        return View(
            "~/Features/Destinations/DestinationPostPage_Default.cshtml",
            viewModel);
    }

    private async Task<Destination?> GetParentDestinationAsync(int parentPageId)
    {
        if (parentPageId <= 0) return null;

        if (cache != null)
        {
            var cacheKey = $"ParentDestination_{websiteChannelContext.WebsiteChannelName}_{parentPageId}";
            return await cache.LoadAsync(async cs =>
            {
                cs.GetCacheDependency = () => CacheHelper.GetCacheDependency(new[]
                {
                    $"webpageitem|byid|{parentPageId}",
                    $"webpageitem|bychannel|{websiteChannelContext.WebsiteChannelName}|bycontenttype|{Destination.CONTENT_TYPE_NAME}",
                    CacheHelper.GetCacheItemName("contentitem", "bycontenttype", Destination.CONTENT_TYPE_NAME)
                });

                var parentQuery = new ContentItemQueryBuilder()
                    .ForContentType(
                        Destination.CONTENT_TYPE_NAME,
                        query =>
                        {
                            query.ForWebsite(websiteChannelContext.WebsiteChannelName);
                            query.Where(where => where.WhereEquals(nameof(WebPageFields.WebPageItemID), parentPageId));
                            query.WithLinkedItems(0);
                        });

                var parentDestinations = await contentQueryExecutor.GetMappedWebPageResult<Destination>(
                    parentQuery,
                    new ContentQueryExecutionOptions { ForPreview = false });

                return parentDestinations.FirstOrDefault();
            }, new CacheSettings(60, cacheKey));
        }

        var directQuery = new ContentItemQueryBuilder()
            .ForContentType(
                Destination.CONTENT_TYPE_NAME,
                query =>
                {
                    query.ForWebsite(websiteChannelContext.WebsiteChannelName);
                    query.Where(where => where.WhereEquals(nameof(WebPageFields.WebPageItemID), parentPageId));
                    query.WithLinkedItems(0);
                });

        var results = await contentQueryExecutor.GetMappedWebPageResult<Destination>(
            directQuery,
            new ContentQueryExecutionOptions { ForPreview = false });

        return results.FirstOrDefault();
    }

    private static CampaignStickyBarViewModel CreateDestinationStickyModel(
        Destination destination,
        ContactUsViewModel contactUs,
        ReviewListingResponse? reviews)
    {
        return new CampaignStickyBarViewModel
        {
            DisplaySticky = destination.DisplaySticky,

            CampaignDetail1 = destination.CampaignDetail1 ?? string.Empty,
            CampaignDetail2 = destination.CampaignDetail2 ?? string.Empty,
            CampaignDetail3 = destination.CampaignDetail3 ?? string.Empty,
            CountdownLabel = destination.CountdownLabel ?? string.Empty,

            EndOfCampaign = destination.EndOfCampaign == DateTime.MinValue
                ? string.Empty
                : destination.EndOfCampaign.ToString(
                    "yyyy-MM-ddTHH:mm:ss",
                    System.Globalization.CultureInfo.InvariantCulture),

            CTA = contactUs,
            Reviews = reviews
        };
    }
}

public sealed class DestinationPostPageViewModel
{
    public string? CtaButtonText { get; init; }
    public string? CtaButtonUrl { get; init; }

    public string DestinationName { get; init; } = string.Empty;
    public ImageAssetViewModel? DestinationHeroImage { get; init; }
    public ImageAssetViewModel? HeroForegroundImage { get; init; }
    public bool DestinationHeroDisableOverlay { get; init; }
    public string DestinationHeroImageAltTag { get; init; } = string.Empty;
    public string DestinationCampaignTitle { get; init; } = string.Empty;
    public string DestinationHeading { get; init; } = string.Empty;

    public IEnumerable<ArticleViewModel> RelatedArticles { get; init; } = [];

    public string DestinationJsonSchema { get; init; } = string.Empty;
    public string DestinationSummary { get; init; } = string.Empty;
    public string DestinationDetail { get; init; } = string.Empty;
    public string DestinationWhenToVisit { get; init; } = string.Empty;
    public CampaignStickyBarViewModel DestinationStickyModel { get; init; } = new();
    public IEnumerable<AccordionItemFolder> SelectedAccordionItemFolders { get; init; } = [];
    public IEnumerable<ImageFolder> SelectedImageFolders { get; init; } = [];
    public IEnumerable<ImageTileCtaFolder> SelectedImageTileCtaFolders { get; init; } = [];
    public string DestinationRelatedArticlesHeader { get; init; } = string.Empty;
    // Reuse your existing ContactUsViewModel.
    public ContactUsViewModel CTA { get; init; } = new();

    public ReviewListingResponse? Reviews { get; init; }
}
