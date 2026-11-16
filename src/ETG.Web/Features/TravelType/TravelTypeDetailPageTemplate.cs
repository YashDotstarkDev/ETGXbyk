using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CMS.Websites.Routing;
using ETG;
using ETG.Core.Content;
using ETG.Core.Models;
using ETG.Core.Models.GoogleReviews;
using ETG.Web.Components.PageBuilder.Widgets.GoogleReviews;
using ETG.Web.Features.TravelType.Operations;
using ETG.Web.Infrastructure;
using ETG.Web.Rendering;
using ETG.Web.Services.GoogleReviews;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using MediaFile = ETG.Core.Content.MediaFile;

[assembly: RegisterPageTemplate(
    identifier: "ETG.TravelTypeDetailTemplate",
    name: "Travel Type Detail - Default",
    propertiesType: typeof(
        ETG.Web.Features.TravelType.TravelTypeDetailPageTemplateProperties),
    customViewName:
        "~/Features/TravelType/TravelTypeDetailPage_Default.cshtml",
    ContentTypeNames = [TravelTypeDetail.CONTENT_TYPE_NAME]
)]

[assembly: RegisterWebPageRoute(
    contentTypeName: TravelTypeDetail.CONTENT_TYPE_NAME,
    controllerType: typeof(
        ETG.Web.Features.TravelType.TravelTypeDetailPageTemplateController),
    WebsiteChannelNames = new[] { "ETG" }
)]

namespace ETG.Web.Features.TravelType;

public class TravelTypeDetailPageTemplateProperties : IPageTemplateProperties
{
}

public class TravelTypeDetailPageTemplateController(
    IMediator mediator,
    IWebsiteChannelContext channelContext,
    IWebPageDataContextRetriever contextRetriever,
    WebPageMetaService metaService,
    IGoogleReviewsService reviewsService
) : Controller
{
    public async Task<ActionResult> Index()
    {
        if (!contextRetriever.TryRetrieve(out var data))
            return NotFound();

        var result = await mediator.Send(
            new TravelTypeDetailPageQuery(
                data.WebPage,
                channelContext.WebsiteChannelName
            )
        );

        if (result?.Page is null)
            return NotFound();

        var page = result.Page;

        var heroImage = page.TravelTypeDetailHeroImage?
            .OfType<MediaFile>()
            .FirstOrDefault();

        var heading = page.TravelTypeDetailHeading
            ?? page.TravelTypeDetailName
            ?? "";

        var heroIcon = !string.IsNullOrWhiteSpace(page.TravelTypeDetailHeroSVGIcon)
            ? page.TravelTypeDetailHeroSVGIcon
            : page.TravelTypeDetailHeroIcon ?? "";

        GoogleReviewsWidgetViewModel? googleReviewsVm = null;
        ReviewListingResponse? reviews = null;

        if (!page.HideGoogleReviews)
        {
            var reviewsData = await reviewsService.GetReviewsAsync();
            if (reviewsData != null)
            {
                reviews = reviewsData;

                var props = new GoogleReviewsWidgetProperties
                {
                    Heading = "What our customers say"
                };
                googleReviewsVm = new GoogleReviewsWidgetViewModel(props, reviewsData);
            }
        }

        var galleryImages = HeroImageHelper.ExtractGalleryImages(page.SelectedImageFolders);

        var contactUs = new ContactUsViewModel
        {
            CTAUrl = $"{Request.PathBase}/enquire",
            CTABackgroundImage = heroImage?.LegacyMediaFileAsset?.Url ?? string.Empty
        };

        var viewModel = new TravelTypeDetailPageViewModel
        {
            Name = page.TravelTypeDetailName ?? "",
            Heading = heading,
            Summary = page.TravelTypeDetailSummary ?? "",
            Detail = page.TravelTypeDetail1 ?? "",

            Hero = new HeroViewModel
            {
                HeroImageUrl = heroImage?.LegacyMediaFileAsset?.Url ?? galleryImages.FirstOrDefault()?.ImageUrl ?? "",
                HeroImageAltText = page.TravelTypeDetailHeroImageAltTag ?? galleryImages.FirstOrDefault()?.AltText ?? "",
                Heading = heading,
                HeroIconSvg = heroIcon,
                DisableOverlay = true,
                GalleryImages = galleryImages
            },

            ThemedPackagesHeading = result.ThemedPackagesHeading,
            ThemedPackagesUrl = result.ThemedPackagesUrl,
            HideGoogleReviews = page.HideGoogleReviews,
            GoogleReviews = googleReviewsVm,
            SelectedImageFolders = page.SelectedImageFolders ?? [],

            ContactUs = contactUs,

            TravelTypeStickyModel =
                CreateTravelTypeStickyModel(page, contactUs, reviews)
        };

        if (result.Page is IMetaDataSchema metaFields)
        {
            metaService.SetMeta(
                metaFields,
                heroImage?.LegacyMediaFileAsset?.Url);
        }

        return View(
            "~/Features/TravelType/TravelTypeDetailPage_Default.cshtml",
            viewModel
        );
    }

    private static CampaignStickyBarViewModel CreateTravelTypeStickyModel(
        TravelTypeDetail page,
        ContactUsViewModel contactUs,
        ReviewListingResponse? reviews)
    {
        return new CampaignStickyBarViewModel
        {
            DisplaySticky = page.DisplaySticky,

            CampaignDetail1 = page.CampaignDetail1 ?? string.Empty,
            CampaignDetail2 = page.CampaignDetail2 ?? string.Empty,
            CampaignDetail3 = page.CampaignDetail3 ?? string.Empty,
            CountdownLabel = page.CountdownLabel ?? string.Empty,

            EndOfCampaign = page.EndOfCampaign == DateTime.MinValue
                ? string.Empty
                : page.EndOfCampaign.ToString(
                    "yyyy-MM-ddTHH:mm:ss",
                    CultureInfo.InvariantCulture),

            CTA = contactUs,
            Reviews = reviews
        };
    }
}

public class TravelTypeDetailPageViewModel
{
    public string Name { get; init; } = "";
    public string Heading { get; init; } = "";
    public string Summary { get; init; } = "";
    public string Detail { get; init; } = "";

    public HeroViewModel Hero { get; init; } = new();

    public string ThemedPackagesHeading { get; init; } = "";
    public string ThemedPackagesUrl { get; init; } = "";
    public bool HideGoogleReviews { get; init; }
    public GoogleReviewsWidgetViewModel? GoogleReviews { get; init; }
    public IEnumerable<ImageFolder> SelectedImageFolders { get; init; } = [];
    public IEnumerable<ArticleViewModel> RelatedArticles { get; init; } = [];

    public ContactUsViewModel? ContactUs { get; init; }

    public CampaignStickyBarViewModel TravelTypeStickyModel { get; init; } = new();
}