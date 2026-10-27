using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Content;
using ETG.Core.Models;
using ETG.Core.Models.GoogleReviews;
using ETG.Web.Features.DefaultPage;
using ETG.Web.Infrastructure;
using ETG.Web.Services.GoogleReviews;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

using MediaFile = ETG.Core.Content.MediaFile;


[assembly: RegisterPageTemplate(
    identifier: "ETG.DefaultPageTemplate",
    name: "Default Page - Default",
    propertiesType: typeof(DefaultPageTemplateProperties),
    customViewName: "~/Features/DefaultPage/DefaultPage_Default.cshtml",
    ContentTypeNames = [Default.CONTENT_TYPE_NAME]
)]

[assembly: RegisterWebPageRoute(
    contentTypeName: Default.CONTENT_TYPE_NAME,
    controllerType: typeof(DefaultPageTemplateController)
)]

namespace ETG.Web.Features.DefaultPage;

public sealed class DefaultPageTemplateProperties : IPageTemplateProperties
{
}

public sealed class DefaultPageViewModel
{
    public string Name { get; init; } = string.Empty;
    public string Heading { get; init; } = string.Empty;
    public string ShortDescription { get; init; } = string.Empty;
    public HeroViewModel Hero { get; init; } = new();
    public bool ShowGoogleReviews { get; init; }
    public ReviewListingResponse? ReviewsData { get; init; }
}

public sealed class DefaultPageTemplateController(
    IMediator mediator,
    IWebPageDataContextRetriever contextRetriever,
    IGoogleReviewsService reviewsService,
    WebPageMetaService metaService,
    ILogger<DefaultPageTemplateController> logger
) : Controller
{
    public async Task<IActionResult> Index()
    {
        if (!contextRetriever.TryRetrieve(out var data))
        {
            return NotFound();
        }

        var page = await mediator.Send(
            new DefaultPageQuery(data.WebPage),
            HttpContext.RequestAborted);

        if (page == null)
        {
            logger.LogWarning("Default page could not be found for WebPageItemID: {WebPageItemID}", data.WebPage.WebPageItemID);
            return NotFound();
        }

        // 1. Resolve Hero Image
        var heroImage = page.DefaultHeroImage?
            .OfType<MediaFile>()
            .FirstOrDefault();

        var heroImageUrl = heroImage?.LegacyMediaFileAsset?.Url ?? string.Empty;

        // 2. Set Metadata (from MetaDataSchema)
        if (page is IMetaDataSchema metaFields)
        {
            metaService.SetMeta(metaFields, heroImageUrl);
        }

        // 3. Build Hero ViewModel (Heading and Hero Image as requested)
        var heroViewModel = new HeroViewModel
        {
            Heading = !string.IsNullOrWhiteSpace(page.DefaultHeading)
                ? page.DefaultHeading
                : (page.DefaultName ?? string.Empty),
            HeroImageUrl = heroImageUrl,
            HeroImageAltText = page.DefaultHeading ?? page.DefaultName ?? string.Empty
        };

        // 4. Fetch Google Reviews if checkbox is enabled
        ReviewListingResponse? reviewsData = null;
        if (page.DefaultGoogleReviews)
        {
            reviewsData = await reviewsService.GetReviewsAsync();
        }

        var viewModel = new DefaultPageViewModel
        {
            Name = page.DefaultName ?? string.Empty,
            Heading = page.DefaultHeading ?? string.Empty,
            ShortDescription = page.DefaultShortDescription ?? string.Empty,
            Hero = heroViewModel,
            ShowGoogleReviews = page.DefaultGoogleReviews,
            ReviewsData = reviewsData
        };

        return View("~/Features/DefaultPage/DefaultPage_Default.cshtml", viewModel);
    }
}
