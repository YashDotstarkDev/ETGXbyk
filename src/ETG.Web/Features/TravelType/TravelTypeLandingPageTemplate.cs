using CMS.Websites;
using CMS.Websites.Routing;
using ETG;
using ETG.Core.Content;
using ETG.Core.Models;
using ETG.Web.Components.PageBuilder.Widgets.Products;
using ETG.Web.Features;
using ETG.Web.Features.TravelType.Operations;
using ETG.Web.Infrastructure;
using ETG.Web.Rendering;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using MediaFile = ETG.Core.Content.MediaFile;

[assembly: RegisterWebPageRoute(
    contentTypeName: TravelTypeLanding.CONTENT_TYPE_NAME,
    controllerType: typeof(
        ETG.Web.Features.TravelType.TravelTypeLandingPageTemplateController),
   

    WebsiteChannelNames = new[] { "ETG" }

)]

namespace ETG.Web.Features.TravelType;

public class TravelTypeLandingPageTemplateProperties
    : IPageTemplateProperties
{
}

public class TravelTypeLandingPageTemplateController(
    IMediator mediator,
    IWebPageUrlRetriever urlRetriever,
    IWebsiteChannelContext channelContext,
    IWebPageDataContextRetriever contextRetriever,
    WebPageMetaService metaService
) : Controller
{
    public async Task<ActionResult> Index()
    {
        if (!contextRetriever.TryRetrieve(out var data))
        {
            return NotFound();
        }

        var result = await mediator.Send(
            new TravelTypeLandingPageQuery(
                data.WebPage,
                channelContext.WebsiteChannelName
            )
        );

        if (result?.Page is null)
        {
            return NotFound();
        }

        var heroImage = result.Page.TravelTypeHeroImage?
            .OfType<MediaFile>()
            .FirstOrDefault();

        var items = await Task.WhenAll(
            result.Items.Select(async detail =>
            {
                var url = await urlRetriever.Retrieve(detail);

                var image = detail.TravelTypeDetailHeroImage?
                    .OfType<MediaFile>()
                    .FirstOrDefault();

                return new TravelTypeDetailSummaryModel
                {
                    Heading = detail.TravelTypeDetailHeading
          ?? detail.TravelTypeDetailName
          ?? "",
                    Summary = detail.TravelTypeDetailSummary ?? "",
                    HeroImageUrl = image is null
    ? ""
    : ImageAssetViewModel.Create(image).LegacyMediaFileAsset?.Url ?? "",
                    HeroImageAltText = detail.TravelTypeDetailHeroImageAltTag
          ?? detail.TravelTypeDetailName
          ?? "",
                    Path = url.RelativePath
                };
            })
        );

        var heading = result.Page.TravelTypeHeading
     ?? result.Page.TravelTypeName
     ?? "";

        var viewModel = new TravelTypeLandingPageViewModel
        {
            Name = result.Page.TravelTypeName ?? "",
            Heading = heading,
            Intro = result.Page.TravelTypeIntro ?? "",
            Summary = result.Page.TravelTypeSummary ?? "",

            Hero = new HeroViewModel
            {
                HeroImageUrl = heroImage?.LegacyMediaFileAsset?.Url ?? "",
                HeroImageAltText = result.Page.TravelTypeHeroAltText ?? "",
                Heading = heading,
                HeroIconSvg = result.Page.TravelTypeSVGIcon,
            },

            HeroIcon = result.Page.TravelTypeHeroIcon,
            SvgIcon = result.Page.TravelTypeSVGIcon,

            TravelTypeDetails = items
        };

        if (result.Page is IMetaDataSchema metaFields)
        {
            metaService.SetMeta(
                metaFields,
                heroImage?.LegacyMediaFileAsset?.Url);
        }
        return View(
                    "~/Features/TravelType/TravelTypeLandingPage_Default.cshtml",
                     viewModel
                );
    }
}

public class TravelTypeLandingPageViewModel
{
    public string Name { get; init; } = "";
    public string Heading { get; init; } = "";
    public string Intro { get; init; } = "";
    public string Summary { get; init; } = "";
    public string? HeroIcon { get; init; }
    public string? SvgIcon { get; init; }

    public HeroViewModel Hero { get; init; } = new();

    public IReadOnlyList<TravelTypeDetailSummaryModel> TravelTypeDetails { get; init; }
    = [];
}