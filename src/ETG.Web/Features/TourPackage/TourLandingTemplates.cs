using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Content;
using ETG.Web.Features.TourPackage.Operations;
using ETG.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

[assembly: RegisterPageTemplate(
    identifier: "ETG.TourLandingPage_Default",
    name: "Tour Landing Page",
    propertiesType: typeof(ETG.Web.Features.TourPackage.TourLandingPageTemplateProperties),
    customViewName: "~/Features/TourPackage/TourLanding_Default.cshtml",
    ContentTypeNames = new[] { ETG.Core.Content.Page.CONTENT_TYPE_NAME },
    Description = "Algolia Search Page"
)]

[assembly: RegisterWebPageRoute(
    contentTypeName: ETG.Core.Content.Page.CONTENT_TYPE_NAME,
    controllerType: typeof(ETG.Web.Features.TourPackage.TourLandingPageTemplateController),
         Path = "/Search",
    WebsiteChannelNames = new[] { "ETG" }
)]

namespace ETG.Web.Features.TourPackage;

public class TourLandingPageTemplateProperties : IPageTemplateProperties { }

// ==========================================
// 1. CONTROLLER (100% Algolia Client-Side)
// ==========================================
public class TourLandingPageTemplateController(
    IMediator mediator,
    IWebPageDataContextRetriever contextRetriever,
    IConfiguration config,
    WebPageMetaService metaService
    ) : Controller
{
    public async Task<ActionResult> Index()
    {
        if (!contextRetriever.TryRetrieve(out var data))
        {
            return NotFound();
        }

        var pageItem = await mediator.Send(new TourLandingQuery(data.WebPage));
        var viewModel = TourLandingViewModel.Create(pageItem, config);


        if (pageItem is IMetaDataSchema metaFields)
        {
            var heroImage = pageItem.PageHeroImage?
                .OfType<ETG.Core.Content.MediaFile>()
                .FirstOrDefault();

            metaService.SetMeta(
                metaFields,
                heroImage?.LegacyMediaFileAsset?.Url);
        }
        //return new TemplateResult(viewModel);
        return View(
    "~/Features/TourPackage/TourLanding_Default.cshtml",
    viewModel);
    }
}

// ==========================================
// 2. VIEW MODELS 
// ==========================================
public class TourLandingViewModel
{
    public string ApplicationId { get; init; } = "";
    public string SearchKey { get; init; } = "";
    public string TourIndexName { get; init; } = "";
    public string Heading { get; init; } = "";

    public static TourLandingViewModel Create(Page page, IConfiguration config)
    {
        return new TourLandingViewModel
        {
            ApplicationId = config["CMSAlgoliaSearch:ApplicationId"] ?? "",
            SearchKey = config["CMSAlgoliaSearch:SearchKey"] ?? "",
            TourIndexName = config["CMSAlgoliaSearch:SearchIndexTour"] ?? "SiteSearch",
            Heading = page?.PageHeading ?? ""
        };
    }
}
