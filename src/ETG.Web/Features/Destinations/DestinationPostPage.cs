using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Content;
using ETG.Core.Models;
using ETG.Core.Models.GoogleReviews;
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

    public DestinationPostPageTemplateController(
        IMediator mediator,
        IWebPageDataContextRetriever contextRetriever,
        IContentQueryExecutor contentQueryExecutor,
        IWebPageUrlRetriever urlRetriever,
        IWebsiteChannelContext websiteChannelContext,
        ITaxonomyRetriever taxonomyRetriever,
        WebPageMetaService metaService,
        IGoogleReviewsService reviewsService)
    {
        this.mediator = mediator;
        this.contextRetriever = contextRetriever;
        this.contentQueryExecutor = contentQueryExecutor;
        this.urlRetriever = urlRetriever;
        this.websiteChannelContext = websiteChannelContext;
        this.taxonomyRetriever = taxonomyRetriever;
        this.metaService = metaService;
        this.reviewsService = reviewsService;
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

        var destinationGuids = new HashSet<Guid>
        {
            destination.SystemFields.WebPageItemGUID
        };

        var parentPageId =
            destination.SystemFields.WebPageItemParentID;

        if (parentPageId > 0)
        {
            var parentQuery = new ContentItemQueryBuilder()
                .ForContentType(
                    Destination.CONTENT_TYPE_NAME,
                    query =>
                    {
                        query.ForWebsite(
                            websiteChannelContext.WebsiteChannelName);

                        query.Where(where => where.WhereEquals(
                            nameof(WebPageFields.WebPageItemID),
                            parentPageId));
                    });

            var parentDestinations = await contentQueryExecutor
                .GetMappedWebPageResult<Destination>(parentQuery);

            var parentDestination = parentDestinations.FirstOrDefault();

            if (parentDestination is not null)
            {
                destinationGuids.Add(
                    parentDestination.SystemFields.WebPageItemGUID);
            }
        }

        var articlesQuery = new ContentItemQueryBuilder()
            .ForContentType(
                ArticleContent.CONTENT_TYPE_NAME,
                query =>
                {
                    query.ForWebsite(
                        websiteChannelContext.WebsiteChannelName);

                    query.WithLinkedItems(4);
                });

        var articleCandidates = await contentQueryExecutor
            .GetMappedWebPageResult<ArticleContent>(articlesQuery);

        var relatedArticles = articleCandidates
            .Where(article =>
                article.ArticleDestinations?.Any(reference =>
                    destinationGuids.Contains(reference.WebPageGuid))
                == true)
            .ToArray();

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

        var destinationUrl = await urlRetriever.Retrieve(destination);

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
                : $"{Request.PathBase}/destinations/enquire/{destinationAlias}"
        };

        var reviews = await reviewsService.GetReviewsAsync();

        var viewModel = new DestinationPostPageViewModel
        {
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
                CreateDestinationStickyModel(destination),

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
            CTA = contactUs,
            Reviews = reviews
        };

        if (destination is IMetaDataSchema metaFields)
        {
            metaService.SetMeta(
                metaFields,
                heroImage?.LegacyMediaFileAsset?.Url);
        }

        return View(
            "~/Features/Destinations/DestinationPostPage_Default.cshtml",
            viewModel);
    }

    private static DestinationStickyModel CreateDestinationStickyModel(
        Destination destination)
    {
        return new DestinationStickyModel
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
                    System.Globalization.CultureInfo.InvariantCulture)
        };
    }
}

public sealed class DestinationPostPageViewModel
{
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

    public DestinationStickyModel DestinationStickyModel { get; init; }
        = new();

    public IEnumerable<AccordionItemFolder> SelectedAccordionItemFolders
    { get; init; } = [];

    public IEnumerable<ImageFolder> SelectedImageFolders
    { get; init; } = [];

    public IEnumerable<ImageTileCtaFolder> SelectedImageTileCtaFolders
    { get; init; } = [];

    public ContactUsViewModel CTA { get; init; } = new();

    public ReviewListingResponse? Reviews { get; init; }
}

public sealed class DestinationStickyModel
{
    public bool DisplaySticky { get; init; }
    public string CampaignDetail1 { get; init; } = string.Empty;
    public string CampaignDetail2 { get; init; } = string.Empty;
    public string CampaignDetail3 { get; init; } = string.Empty;
    public string CountdownLabel { get; init; } = string.Empty;
    public string EndOfCampaign { get; init; } = string.Empty;
}