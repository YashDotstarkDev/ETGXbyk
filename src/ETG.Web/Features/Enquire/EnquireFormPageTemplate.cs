using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Content;
using ETG.Core.Models;
using ETG.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

[assembly: RegisterPageTemplate(
    identifier: "ETG.EnquireFormPage",
    name: "Enquire Form Page",
    propertiesType: typeof(ETG.Web.Features.Enquire.EnquireFormPageTemplateProperties),
    customViewName: "~/Features/Enquire/EnquireFormPage.cshtml",
    ContentTypeNames = new[]
    {
        Container.CONTENT_TYPE_NAME
    },
    Description = "Generic enquiry form page",
    IconClass = "icon-form"
)]

[assembly: RegisterWebPageRoute(
    contentTypeName: Container.CONTENT_TYPE_NAME,
    controllerType: typeof(ETG.Web.Features.Enquire.EnquireFormPageTemplateController),
     Path = "/Enquire",
    WebsiteChannelNames = new[] { "ETG" }
)]

namespace ETG.Web.Features.Enquire;

public sealed class EnquireFormPageTemplateProperties : IPageTemplateProperties
{
}

public sealed class EnquireFormPageTemplateController : Controller
{
    private readonly IWebPageDataContextRetriever contextRetriever;
    private readonly IContentQueryExecutor contentQueryExecutor;
    private readonly IWebsiteChannelContext websiteChannelContext;
    private readonly WebPageMetaService metaService;

    public EnquireFormPageTemplateController(
        IWebPageDataContextRetriever contextRetriever,
        IContentQueryExecutor contentQueryExecutor,
        IWebsiteChannelContext websiteChannelContext,
        WebPageMetaService metaService)
    {
        this.contextRetriever = contextRetriever;
        this.contentQueryExecutor = contentQueryExecutor;
        this.websiteChannelContext = websiteChannelContext;
        this.metaService = metaService;
    }

    public async Task<IActionResult> Index()
    {
        if (!contextRetriever.TryRetrieve(out var data))
        {
            return NotFound();
        }

        var containerQuery = new ContentItemQueryBuilder()
            .ForContentType(
                Container.CONTENT_TYPE_NAME,
                config =>
                {
                    config
                        .ForWebsite(websiteChannelContext.WebsiteChannelName)
                        .Where(where =>
                            where.WhereEquals(
                                "WebPageItemID",
                                data.WebPage.WebPageItemID));
                });

        var containerPages =
            await contentQueryExecutor.GetMappedWebPageResult<Container>(
                containerQuery,
                cancellationToken: HttpContext.RequestAborted);

        var page = containerPages.FirstOrDefault();

        if (page is null)
        {
            return NotFound();
        }

        var destinationQuery = new ContentItemQueryBuilder()
            .ForContentType(
                Destination.CONTENT_TYPE_NAME,
                config =>
                {
                    config
                        .ForWebsite(websiteChannelContext.WebsiteChannelName);
                });

        var destinationQueryOptions = new ContentQueryExecutionOptions
        {
            ForPreview = false
        };

        var destinations =
            await contentQueryExecutor.GetMappedWebPageResult<Destination>(
                destinationQuery,
                destinationQueryOptions,
                cancellationToken: HttpContext.RequestAborted);

        var destinationItems = destinations
            .Where(x => !string.IsNullOrWhiteSpace(x.DestinationName))
            .OrderBy(x => x.DestinationName)
            .Select(x => new SelectListItem
            {
                Text = x.DestinationName,
                Value = x.DestinationName
            })
            .ToList();

        var experienceQuery = new ContentItemQueryBuilder()
            .ForContentType(
                ETG.Core.Content.Experience.CONTENT_TYPE_NAME,
                config =>
                {
                    config
                        .ForWebsite(websiteChannelContext.WebsiteChannelName);
                });

        var experienceQueryOptions = new ContentQueryExecutionOptions
        {
            ForPreview = false
        };

        var experiences =
            await contentQueryExecutor.GetMappedWebPageResult<ETG.Core.Content.Experience>(
                experienceQuery,
                experienceQueryOptions,
                cancellationToken: HttpContext.RequestAborted);

        var experienceItems = experiences
            .Where(x => !string.IsNullOrWhiteSpace(x.ExperienceName))
            .OrderBy(x => x.ExperienceName)
            .Select(x => new SelectListItem
            {
                Text = x.ExperienceName,
                Value = x.ExperienceName
            })
            .ToList();

        var selectedDestination =
    GetSelectedValue(
        Request.Query["destination"].ToString(),
        destinationItems);

        var selectedExperience =
            GetSelectedValue(
                Request.Query["experience"].ToString(),
                experienceItems);

        var viewModel = new EnquireFormPageViewModel
        {
            Page = page,

            Form = new EnquireFormViewModel
            {
                PreferredContactType = "Email",
                PreferredDestination = selectedDestination,
                PreferredExperience = selectedExperience
            },
            Destinations = destinationItems,
            Experiences = experienceItems
        };

        if (page is IMetaDataSchema metaFields)
        {
            metaService.SetMeta(metaFields);
        }

        return View("~/Features/Enquire/EnquireFormPage.cshtml", viewModel);
    }
    private static string GetSelectedValue(string value, IEnumerable<SelectListItem> options)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var selected = options.FirstOrDefault(x =>
            string.Equals(
                x.Value,
                value,
                StringComparison.OrdinalIgnoreCase));

        return selected?.Value ?? string.Empty;
    }
}