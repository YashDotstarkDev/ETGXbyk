using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Content;
using ETG.Web.Features;
using ETG.Web.Features.Experience;
using ETG.Web.Features.Experience.Operations;
using ETG.Web.Infrastructure;
using ETG.Web.Rendering;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using ArticleContent = ETG.Core.Content.Article;
using ExperienceContent = ETG.Core.Content.Experience;
using MediaFile = ETG.Core.Content.MediaFile;
using ETG.Core.Models.Tour;
using TourContent = ETG.Core.Content.Tour;


using ETG.Web.Services.GoogleReviews;
using ETG.Web.Components.PageBuilder.Widgets.GoogleReviews;

using ETG.Core.Models;


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
    WebPageMetaService metaService) : Controller


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

        var relatedArticlesQuery = new ContentItemQueryBuilder();

        relatedArticlesQuery.ForContentType(
            ArticleContent.CONTENT_TYPE_NAME,
            query =>
            {
                query.ForWebsite(channelContext.WebsiteChannelName);

                query.Where(where =>
                    where.WhereLike(
                        nameof(ArticleContent.ArticleExperiences),
                        $"%{selectedExperienceGuid}%"));
            });

        var relatedArticles = (await contentQueryExecutor
            .GetMappedWebPageResult<ArticleContent>(relatedArticlesQuery))
            .OrderByDescending(article => article.ArticleDate)
            .Take(3)
            .ToList();

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

        var tourListing = await CreateTourListingAsync(
            experience,
            selectedExperienceGuid);

        GoogleReviewsWidgetViewModel? googleReviewsVm = null;
        if (!experience.HideGoogleReviews)
        {
            var reviewsData = await reviewsService.GetReviewsAsync();
            if (reviewsData != null)
            {
                var props = new GoogleReviewsWidgetProperties
                {
                    Heading = "What our customers say"
                };
                googleReviewsVm = new GoogleReviewsWidgetViewModel(props, reviewsData);
            }
        }



        var experienceAlias = (experience.ExperienceName ?? string.Empty).Trim().Replace(" ", "-").ToLowerInvariant();

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
            ContactUs = new ContactUsViewModel
            {
                CTAHeading = "Connect with us",
                CTAPhoneDetail1 = "Australia: (02) 9094 3322",
                CTAPhoneDetail2 = "New Zealand: 0800 855 045",
                CTAUrl = $"/experiences/enquire/{experienceAlias}",
                CTAText = "ENQUIRE NOW",
                CTABackgroundImage = "/content/images/travel_destination.png"
            },

            HideGoogleReviews = experience.HideGoogleReviews,
            GoogleReviews = googleReviewsVm,


            RelatedArticles =
                relatedArticleViewModels,

            TourListing =
                tourListing
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

        var toursQuery = new ContentItemQueryBuilder();

        toursQuery.ForContentType(
            TourContent.CONTENT_TYPE_NAME,
            query =>
            {
                query.ForWebsite(channelContext.WebsiteChannelName);

                if (selectedPageGuids.Any())
                {
                    query.Where(where => where.WhereIn(
                        "WebPageItemGUID",
                        selectedPageGuids));
                }
                else
                {
                    query.Where(where => where.WhereLike(
                        nameof(TourContent.TourExperiences),
                        $"%{selectedExperienceGuid}%"));

                    query.TopN(3);
                }

                query.WithLinkedItems(
                    2,
                    options => options.IncludeWebPageData());
            });

        var allTours = (await contentQueryExecutor
            .GetMappedWebPageResult<TourContent>(toursQuery))
            .ToList();
        IEnumerable<TourContent> featuredTours;

        if (selectedPageGuids.Any())
        {
            featuredTours = selectedPageGuids
                .Select(guid => allTours.FirstOrDefault(tour => tour.SystemFields.WebPageItemGUID == guid))
                .Where(tour => tour is not null)
                .Select(tour => tour!);
        }
        else
        {
            featuredTours = allTours;
        }

        var cards = await Task.WhenAll(featuredTours.Select(async tour =>
        {
            var tourUrl = await urlRetriever.Retrieve(tour);
            var selectedPricing = tour.SelectedTourPricingFolders?
     .SelectMany(folder => folder.SelectedTourPricings ?? [])
     .Select(pricing => new
     {
         Price = GetLowestConfiguredPrice(pricing)
     })
     .Where(item => item.Price > 0)
     .OrderBy(item => item.Price)
     .FirstOrDefault();

            var price = selectedPricing?.Price ?? 0;

            var tourHeroImage = tour.TourHeroImage?
                .OfType<MediaFile>()
                .FirstOrDefault();

            return new TourSummaryViewModel
            {
                Name = tour.TourName ?? "",
                TourCode = tour.TourCode ?? "",
                Description = tour.TourSummary ?? "",
                Path = tourUrl.RelativePath,

                HeroImageUrl = tourHeroImage is null
    ? ""
    : ImageAssetViewModel.Create(tourHeroImage)
        .LegacyMediaFileAsset?.Url ?? "",

                HeroImageAlt =
                    tour.TourHeroImageAltTag ??
                    tour.TourName ??
                    "",

                Badge =
                    tour.TourDestinationCity?.ToUpperInvariant() ??
                    "",

                DaysFromLabel = tour.TourNights > 0
                    ? $"{tour.TourNights + 1} DAYS FROM"
                    : "",

                CurrentPrice = price > 0
                    ? $"${price:N0}"
                    : "",

                OriginalPrice = "",

                Currency = tour.TourPriceCurrency ?? "AUD",

                PriceTypeLabel = !string.IsNullOrWhiteSpace(
                    tour.TourPriceTypeLabel)
                        ? tour.TourPriceTypeLabel
                        : "Per person | twin share",

                InclusionIcons = TourSummaryViewModel.ParseInclusions(
    tour.TourPriceInclusions)
            };
        }));

        return new TourListingViewModel
        {
            Heading = $"Featured {experience.ExperienceHeading ?? experience.ExperienceName} Experiences",
            Tours = cards,
            ViewAllUrl = $"/search?experience={Uri.EscapeDataString(experience.ExperienceName)}",

            ViewAllButtonLabel = "View All"
        };

    }
    private static int GetLowestConfiguredPrice(TourPricing pricing)
    {
        if (pricing.TourPricingOverridePrice > 0)
        {
            return pricing.TourPricingOverridePrice;
        }

        return new[]
        {
        pricing.TourPricingEconomyPrice1,
        pricing.TourPricingPremiumPrice1,
        pricing.TourPricingBusinessPrice1,
        pricing.TourPricingFirstClassPrice1,

        pricing.TourPricingEconomyPrice2,
        pricing.TourPricingPremiumPrice2,
        pricing.TourPricingBusinessPrice2,
        pricing.TourPricingFirstClassPrice2,

        pricing.TourPricingEconomyPrice3,
        pricing.TourPricingPremiumPrice3,
        pricing.TourPricingBusinessPrice3,
        pricing.TourPricingFirstClassPrice3,

        pricing.TourPricingEconomyPrice4,
        pricing.TourPricingPremiumPrice4,
        pricing.TourPricingBusinessPrice4,
        pricing.TourPricingFirstClassPrice4
    }
        .Where(value => value > 0)
        .DefaultIfEmpty(0)
        .Min();
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
    public ContactUsViewModel ContactUs { get; init; } = new();
}