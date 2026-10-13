using CMS.Websites;
using CMS.Websites.Routing;
using ETG;
using ETG.Core.Content;
using ETG.Core.Models;
using ETG.Web.Features;
using ETG.Web.Features.Career.Operations;
using ETG.Web.Infrastructure;
using ETG.Web.Rendering;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using MediaFile = ETG.Core.Content.MediaFile;
using Page = ETG.Core.Content.Page;


[assembly: RegisterWebPageRoute(
    contentTypeName: Page.CONTENT_TYPE_NAME,
    controllerType: typeof(
        ETG.Web.Features.Career.CareerLandingPageTemplateController),
         Path = "/careers",
    WebsiteChannelNames = new[] { "ETG" }
)]

namespace ETG.Web.Features.Career;

public class CareerLandingPageTemplateProperties : IPageTemplateProperties
{
}

public class CareerLandingPageTemplateController(
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
            new CareerLandingPageQuery(
                data.WebPage,
                channelContext.WebsiteChannelName
            )
        );

        if (result?.Page is null)
        {
            return NotFound();
        }

        var heroImage = result.Page.PageHeroImage?
            .OfType<MediaFile>()
            .FirstOrDefault();

        var roles = await Task.WhenAll(
            result.Items.Select(async role =>
            {
                var url = await urlRetriever.Retrieve(role);

                return new CareerRoleSummaryModel
                {
                    Title = role.CareerRoleTitle
                        ?? role.DocumentName
                        ?? role.SystemFields.WebPageItemName
                        ?? "",
                    Location = role.CareerRoleLocation ?? "",
                    PositionType = role.CareerRolePositionType ?? "",
                    Path = url.RelativePath
                };
            })
        );

        var heading = !string.IsNullOrWhiteSpace(result.Page.PageHeading)
            ? result.Page.PageHeading
            : result.Page.DocumentName ?? "Careers";

        var viewModel = new CareerLandingPageViewModel
        {
            Name = result.Page.DocumentName ?? "",
            Content = result.Page.PageContent ?? "",
            JsonSchema = result.Page.PageJsonSchema ?? "",
            HideShareButton = result.Page.PageHideShareButton,

            Hero = new HeroViewModel
            {
                HeroImageUrl = heroImage?.LegacyMediaFileAsset?.Url ?? "",
                HeroImageAltText = result.Page.PageHeroImageAltTag ?? "",
                Heading = heading,
                HeroCaption = result.Page.PageHeroCaption ?? ""
            },

            CareerRoles = roles
        };

        if (result.Page is IMetaDataSchema metaFields)
        {
            metaService.SetMeta(
                metaFields,
                heroImage?.LegacyMediaFileAsset?.Url);
        }
        return View("~/Features/Careers/CareerLandingPage_Default.cshtml", viewModel);
    }
}

public class CareerLandingPageViewModel
{
    public string Name { get; init; } = "";
    public string Content { get; init; } = "";
    public string JsonSchema { get; init; } = "";
    public bool HideShareButton { get; init; }

    public HeroViewModel Hero { get; init; } = new();

    public IReadOnlyList<CareerRoleSummaryModel> CareerRoles { get; init; } = [];
}

public class CareerRoleSummaryModel
{
    public string Title { get; init; } = "";
    public string Location { get; init; } = "";
    public string PositionType { get; init; } = "";
    public string Path { get; init; } = "";
}
