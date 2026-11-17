using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Content;
using ETG.Core.Models;
using ETG.Core.Models.GoogleReviews;
using ETG.Core.Models.Tour;
using ETG.Web.Components.PageBuilder.Widgets.GoogleReviews;
using ETG.Web.Features;
using ETG.Web.Features.Experience;
using ETG.Web.Features.Search;
using Microsoft.Extensions.Configuration;
using ETG.Web.Features.Experience.Operations;
using ETG.Web.Infrastructure;
using ETG.Web.Rendering;
using ETG.Web.Services.GoogleReviews;
using ETG.Web.Services.Tour;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using ArticleContent = ETG.Core.Content.Article;
using ExperienceContent = ETG.Core.Content.Experience;
using MediaFile = ETG.Core.Content.MediaFile;
using TourContent = ETG.Core.Content.Tour;

[assembly: RegisterWebPageRoute(
    contentTypeName: ExperienceContent.CONTENT_TYPE_NAME,
    controllerType: typeof(ExperiencePageTemplateController),
    WebsiteChannelNames = new[] { "ETG" }
)]

namespace ETG.Web.Features.Experience;

public class ExperiencePageTemplateProperties : IPageTemplateProperties
{
}

public class ExperiencePageTemplateController(
    IMediator mediator,
    IWebPageDataContextRetriever contextRetriever,
    IWebsiteChannelContext channelContext,
    IContentQueryExecutor contentQueryExecutor,
    IWebPageUrlRetriever urlRetriever,
    ITaxonomyRetriever taxonomyRetriever,
    IGoogleReviewsService reviewsService,
    ITourPricingService pricingService,
    ITourSummaryService tourSummaryService,
    WebPageMetaService metaService,
    IConfiguration configuration) : Controller


{
    public async Task<ActionResult> Index()
    {
        if (!contextRetriever.TryRetrieve(out var data))
        {
            return NotFound();
        }

        var result = await mediator.Send(
            new ExperiencePageQuery(
                data.WebPage,
                channelContext.WebsiteChannelName)
        );

        if (result?.Experience == null)
        {
            return NotFound();
        }

        var experience = result.Experience;

        var selectedExperienceGuid =
            experience.SystemFields.WebPageItemGUID.ToString("D");

        // Launch all independent secondary operations concurrently
        var articlesTask = mediator.Send(
            new GetExperienceArticlesQuery(
                channelContext.WebsiteChannelName,
                selectedExperienceGuid),
            HttpContext.RequestAborted);

        var tourListingTask = CreateTourListingAsync(
            experience,
            selectedExperienceGuid);

        var reviewsTask = !experience.HideGoogleReviews
            ? reviewsService.GetReviewsAsync()
            : Task.FromResult<ReviewListingResponse?>(null);

        var urlTask = urlRetriever.Retrieve(experience);

        // Wait for all concurrent tasks to finish
        await Task.WhenAll(articlesTask, tourListingTask, reviewsTask, urlTask);

        var relatedArticles = await articlesTask;
        var tourListing = await tourListingTask;
        var reviewsData = await reviewsTask;
        var experienceUrl = await urlTask;

        var relatedArticleViewModels = await Task.WhenAll(
            relatedArticles.Select(article =>
                ArticleViewModel.CreateAsync(
                    article,
                    urlRetriever,
                    taxonomyRetriever)));

        var heroImage = experience.ExperienceHeroImage?
            .OfType<MediaFile>()
            .FirstOrDefault();

        var heroForegroundImage = experience.DestinationHeroForegroundImage?
            .OfType<MediaFile>()
            .FirstOrDefault();

        GoogleReviewsWidgetViewModel? googleReviewsVm = null;
        ReviewListingResponse? reviews = null;

        if (reviewsData != null)
        {
            reviews = reviewsData;

            var props = new GoogleReviewsWidgetProperties
            {
                Heading = "What our customers say"
            };
            googleReviewsVm = new GoogleReviewsWidgetViewModel(props, reviewsData);
        }

        var experiencePath = (experienceUrl?.RelativePath ?? string.Empty)
            .Split(new[] { '?', '#' }, 2)[0]
            .TrimEnd('/');

        var experienceAlias = experiencePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .LastOrDefault();

        ContactUsViewModel? contactUs = string.IsNullOrWhiteSpace(experienceAlias)
            ? null
            : new ContactUsViewModel
            {
                CTAUrl = $"{Request.PathBase}/experiences/enquire/{experienceAlias}",
                CTABackgroundImage = experience.ExperienceHeroImage?
                    .OfType<MediaFile>()
                    .FirstOrDefault()?.LegacyMediaFileAsset?.Url ?? string.Empty
            };

        var viewModel = new ExperiencePageDetailViewModel
        {
            ExperienceName = experience.ExperienceName,
            Summary = experience.ExperienceSummary,
            Hero = new HeroViewModel
            {
                Heading = experience.ExperienceHeading ?? "",
                HeroImageUrl = heroImage?.LegacyMediaFileAsset?.Url ?? "",
                HeroImageAltText = experience.ExperienceHeroImageAltTag ?? "",
                HeroForegroundImageUrl = heroForegroundImage?.LegacyMediaFileAsset?.Url,
                DisableOverlay = experience.DestinationHeroDisableOverlay,
                HeroImageAccreditation = experience.ExperienceHeroImageAccreditation,
                HeroIconSvg = experience.ExperienceSVGIcon,
            },
            JsonSchema = experience.ExperienceJsonSchema ?? "",
            SelectedImageFolders = experience.SelectedImageFolders ?? Enumerable.Empty<ImageFolder>(),
            SelectedImageTileCtaFolders = experience.SelectedImageTileCtaFolders ?? Enumerable.Empty<ImageTileCtaFolder>(),
            ContactUs = contactUs,

            HideGoogleReviews = experience.HideGoogleReviews,
            GoogleReviews = googleReviewsVm,

            ExperienceStickyModel =
                CreateExperienceStickyModel(experience, contactUs, reviews),

            RelatedArticles = relatedArticleViewModels,

            TourListing = tourListing
        };

        if (experience is IMetaDataSchema metaFields)
        {
            metaService.SetMeta(
                metaFields,
                heroImage?.LegacyMediaFileAsset?.Url);
        }

        return View("~/Features/Experience/ExperiencePage_Default.cshtml", viewModel);
    }

    private async Task<TourListingViewModel> CreateTourListingAsync(
        ExperienceContent experience,
        string selectedExperienceGuid)
    {
        var selectedPageGuids = (experience.ExperienceFeatureTours ?? [])
            .Concat(experience.ExperienceFeatureCruises ?? [])
            .Where(item => item != null && item.WebPageGuid != Guid.Empty)
            .Select(item => item.WebPageGuid)
            .Distinct()
            .ToList();

        List<TourContent> toursToDisplay = [];

        if (selectedPageGuids.Any())
        {
            var toursQuery = new ContentItemQueryBuilder();

            toursQuery.ForContentType(
                TourContent.CONTENT_TYPE_NAME,
                query =>
                {
                    query.ForWebsite(channelContext.WebsiteChannelName);

                    query.Where(where => where.WhereIn(
                        "WebPageItemGUID",
                        selectedPageGuids));

                    query.WithLinkedItems(
                        4,
                        options => options.IncludeWebPageData());
                });

            var allTours = (await contentQueryExecutor
                .GetMappedWebPageResult<TourContent>(toursQuery))
                .ToList();

            toursToDisplay = selectedPageGuids
                .Select(guid => allTours.FirstOrDefault(tour => tour.SystemFields.WebPageItemGUID == guid))
                .Where(tour => tour is not null)
                .Select(tour => tour!)
                .ToList();
        }
        else
        {
            // K13 Fallback: When no feature tours/cruises are explicitly selected,
            // auto-fetch top 3 tours tagged with this experience (TourExperiences)
            var fallbackQuery = new ContentItemQueryBuilder();

            fallbackQuery.ForContentType(
                TourContent.CONTENT_TYPE_NAME,
                query =>
                {
                    query.ForWebsite(channelContext.WebsiteChannelName);
                    query.TopN(3);

                    query.Where(where =>
                        where.WhereLike(
                            nameof(TourContent.TourExperiences),
                            $"%{selectedExperienceGuid}%"));

                    query.WithLinkedItems(
                        4,
                        options => options.IncludeWebPageData());
                });

            toursToDisplay = (await contentQueryExecutor
                .GetMappedWebPageResult<TourContent>(fallbackQuery))
                .ToList();
        }

        if (!toursToDisplay.Any())
        {
            return new TourListingViewModel();
        }

        var bulkPrices = await pricingService.GetPricesBulkAsync(toursToDisplay);

        var cards = await Task.WhenAll(toursToDisplay.Select(async tour =>
        {
            var tourUrl = await urlRetriever.Retrieve(tour);

            // Resolve price info for this tour
            var priceInfo = (!string.IsNullOrWhiteSpace(tour.TourCode) && bulkPrices.TryGetValue(tour.TourCode, out var p))
                ? p
                : new TourPricingResult(0, 0);

            return await tourSummaryService.MapToViewModelAsync(tour, tourUrl?.RelativePath ?? string.Empty, priceInfo);
        }));

        return new TourListingViewModel
        {
            Heading = $"Featured {experience.ExperienceHeading ?? experience.ExperienceName} Experiences",
            Tours = cards,
            ViewAllUrl = SearchUrlHelper.GetExperienceFilterUrl(configuration["AlgoliaSettings:SearchIndexTour"] ?? "TourPricing-Live", experience.ExperienceName),
            ViewAllButtonLabel = "View All"
        };
    }

    private static CampaignStickyBarViewModel CreateExperienceStickyModel(
        ExperienceContent experience,
        ContactUsViewModel? contactUs,
        ReviewListingResponse? reviews)
    {
        return new CampaignStickyBarViewModel
        {
            DisplaySticky = experience.DisplaySticky,

            CampaignDetail1 = experience.CampaignDetail1 ?? string.Empty,
            CampaignDetail2 = experience.CampaignDetail2 ?? string.Empty,
            CampaignDetail3 = experience.CampaignDetail3 ?? string.Empty,
            CountdownLabel = experience.CountdownLabel ?? string.Empty,

            EndOfCampaign = experience.EndOfCampaign == DateTime.MinValue
                ? string.Empty
                : experience.EndOfCampaign.ToString(
                    "yyyy-MM-ddTHH:mm:ss",
                    CultureInfo.InvariantCulture),

            CTA = contactUs,
            Reviews = reviews
        };
    }
}

public class ExperiencePageDetailViewModel
{
    public string ExperienceName { get; init; } = "";
    public string Summary { get; init; } = "";
    public HeroViewModel Hero { get; init; } = new();
    public IEnumerable<ArticleViewModel> RelatedArticles { get; init; } = [];
    public TourListingViewModel? TourListing { get; init; }
    public bool HideGoogleReviews { get; init; }
    public string JsonSchema { get; init; } = "";
    public IEnumerable<ImageFolder> SelectedImageFolders { get; init; } = [];
    public IEnumerable<ImageTileCtaFolder> SelectedImageTileCtaFolders { get; init; } = [];
    public GoogleReviewsWidgetViewModel? GoogleReviews { get; init; }
    public ContactUsViewModel? ContactUs { get; init; } = new();
    public CampaignStickyBarViewModel ExperienceStickyModel { get; init; } = new();
}