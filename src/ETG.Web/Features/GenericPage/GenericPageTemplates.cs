using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CMS.Helpers;
using CMS.Websites;
using ETG.Core.Content;
using ETG.Core.Models;
using ETG.Web.Features;
using ETG.Web.Infrastructure;
using ETG.Web.Rendering;
using ETG.Web.Services.GoogleReviews;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterPageTemplate(
    identifier: "ETG.GenericContentTemplate",
    name: "Generic Content",
    propertiesType:
        typeof(ETG.Web.Features.GenericPage.GenericPageTemplateProperties),
    customViewName:
        "~/Features/GenericPage/GenericPage_Default.cshtml",
    ContentTypeNames = [Container.CONTENT_TYPE_NAME]
)]

[assembly: RegisterWebPageRoute(
    contentTypeName: Container.CONTENT_TYPE_NAME,
    controllerType:
        typeof(ETG.Web.Features.GenericPage.GenericPageTemplateController)
)]

namespace ETG.Web.Features.GenericPage;

public sealed class GenericPageTemplateProperties : IPageTemplateProperties
{
}

public sealed class GenericPageTemplateController(
    IMediator mediator,
    IWebPageDataContextRetriever contextRetriever,
    IWebPageUrlRetriever urlRetriever,
    WebPageMetaService metaService,
    IGoogleReviewsService reviewsService,
    IContainerExpiryService expiryService
) : Controller
{
    [HttpGet("~/enquire/thank-you")]
    public async Task<IActionResult> ThankYou()
    {
        return await Index();
    }

    public async Task<IActionResult> Index()
    {
        if (!contextRetriever.TryRetrieve(out var data))
        {
            return NotFound();
        }

        var page = await mediator.Send(
            new GenericPageQuery(data.WebPage));

        if (page is null)
        {
            return NotFound();
        }

        var redirectUrl = await expiryService.GetExpiryRedirectUrlAsync(page);
        if (!string.IsNullOrWhiteSpace(redirectUrl))
        {
            return Redirect(redirectUrl);
        }

        var heroImage = page.ContainerHeroImage?
            .OfType<ETG.Core.Content.MediaFile>()
            .FirstOrDefault();

        // Use the same existing helper as the TravelType reference.
        // An empty collection is supplied when folders are null.
        var galleryImages = HeroImageHelper
            .ExtractGalleryImages(
                page.SelectedImageFolders
                ?? Enumerable.Empty<ImageFolder>())
            ?.Where(image =>
                image is not null
                && !string.IsNullOrWhiteSpace(image.ImageUrl))
            .ToArray()
            ?? Array.Empty<HeroGalleryImageItem>();

        var firstGalleryImage = galleryImages.FirstOrDefault();
        var dedicatedHeroUrl = heroImage?.LegacyMediaFileAsset?.Url;

        // Treat null, empty, and whitespace URLs as missing.
        var heroImageUrl = !string.IsNullOrWhiteSpace(dedicatedHeroUrl)
            ? dedicatedHeroUrl
            : firstGalleryImage?.ImageUrl ?? string.Empty;

        var heroAltText =
            !string.IsNullOrWhiteSpace(page.ContainerHeroImageAltTag)
                ? page.ContainerHeroImageAltTag
                : firstGalleryImage?.AltText ?? string.Empty;

        string? heroContactUsPath = null;

        var heroContactPage = page.ContainerHeroContactUsCtaPath?
            .OfType<IWebPageFieldsSource>()
            .FirstOrDefault();

        if (heroContactPage is not null)
        {
            var contactUrl = await urlRetriever.Retrieve(
                heroContactPage);

            heroContactUsPath = contactUrl?.RelativePath;
        }

        var heroModel = new HeroViewModel
        {
            HeroImageUrl = heroImageUrl,
            HeroImageAltText = heroAltText,

            // Folder images are consumed by the existing hero partial.
            GalleryImages = galleryImages,

            Heading = page.ContainerHeading ?? string.Empty,
            //HeadingText = page.ContainerHeadingText,

            HeroCaption = !string.IsNullOrWhiteSpace(dedicatedHeroUrl)
                ? heroImage?.LegacyMediaFileDescription
                : firstGalleryImage?.Caption,

            HeroAccreditation = page.ContainerHeroAccreditation,
            HeroImageAccreditation = page.ContainerHeroAccreditation,
            HeroContactUsPath = heroContactUsPath,
            HeroPhone = page.ContainerHeroPhone

            // No matching Container fields were supplied for:
            // HeroForegroundImageUrl, DisableOverlay, HeroIconSvg.
        };

        // Preserve existing metadata image behavior.
        if (page is IMetaDataSchema metaFields)
        {
            metaService.SetMeta(
                metaFields,
                heroImage?.LegacyMediaFileAsset?.Url);
        }

        // Preserve existing sticky-bar background behavior.
        var contactUs = new ContactUsViewModel
        {
            CTAUrl = $"{Request.PathBase}/enquire",
            CTABackgroundImage =
                heroImage?.LegacyMediaFileAsset?.Url ?? string.Empty
        };

        var reviews = await reviewsService.GetReviewsAsync();

        ViewData["CampaignStickyBar"] = new CampaignStickyBarViewModel
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

        return View(
            "~/Features/GenericPage/GenericPage_Default.cshtml",
            heroModel);
    }
}