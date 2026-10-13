using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Content;
using ETG.Core.Content.Hub;
using ETG.Core.Content.Pages;
using ETG.Web.Infrastructure;
using ETG.Web.Rendering;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MediaFile = ETG.Core.Content.MediaFile;

[assembly: RegisterPageTemplate(
    identifier: "Website.AgentPortalPage_Default",
    name: "Agent Portal Page - Default",
    propertiesType: typeof(
        ETG.Web.Features.AgentPortal.AgentPortalPageTemplateProperties),
    customViewName:
        "~/Features/AgentPortal/AgentPortalPage_Default.cshtml",
    ContentTypeNames = new[]
    {
        AgentPortalMainPage.CONTENT_TYPE_NAME
    }
)]

[assembly: RegisterWebPageRoute(
    contentTypeName: AgentPortalMainPage.CONTENT_TYPE_NAME,
    controllerType: typeof(
        ETG.Web.Features.AgentPortal.AgentPortalPageTemplateController)
)]

namespace ETG.Web.Features.AgentPortal;

public class AgentPortalPageTemplateProperties : IPageTemplateProperties
{
}

public class AgentPortalPageTemplateController : Controller
{
    private readonly IMediator mediator;
    private readonly IWebPageDataContextRetriever contextRetriever;
    private readonly IContentRetriever contentRetriever;
    private readonly ILogger<AgentPortalPageTemplateController> logger;
    private readonly IContentQueryExecutor contentQueryExecutor;
    private readonly WebPageMetaService metaService;

    public AgentPortalPageTemplateController(
        IMediator mediator,
        IWebPageDataContextRetriever contextRetriever,
        IContentRetriever contentRetriever,
        ILogger<AgentPortalPageTemplateController> logger,
        WebPageMetaService metaService,
        IContentQueryExecutor contentQueryExecutor)
    {
        this.mediator = mediator;
        this.contextRetriever = contextRetriever;
        this.contentRetriever = contentRetriever;
        this.logger = logger;
        this.contentQueryExecutor = contentQueryExecutor;
        this.metaService = metaService;
    }

    public async Task<IActionResult> Index()
    {
        if (!contextRetriever.TryRetrieve(out var data))
        {
            return NotFound();
        }

        var page = await mediator.Send(
            new AgentPortalPageQuery(data.WebPage));

        var publishedOptions = new ContentQueryExecutionOptions
        {
            ForPreview = false
        };

        var toolkitQuery = new ContentItemQueryBuilder()
            .ForContentType(
                ToolkitFolder.CONTENT_TYPE_NAME,
                parameters => parameters
                    .WithLinkedItems(3)
                    .Where(where => where.WhereEquals(
                        nameof(ToolkitFolder.DocumentName),
                        "Marketing Campaigns")))
            .InLanguage(data.WebPage.LanguageName);

        var toolkitFolders = await contentQueryExecutor
            .GetMappedResult<ToolkitFolder>(
                toolkitQuery,
                publishedOptions);

        var toolkits = toolkitFolders
            .SelectMany(folder =>
                folder.SelectedToolkits ?? Enumerable.Empty<Toolkit>())
            .Select(toolkit => new ToolkitCard
            {
                Name = toolkit.ToolkitName ?? string.Empty,
                Url = toolkit.ToolkitUrl ?? string.Empty,
                ImageUrl = GetImageUrl(
                    toolkit.ToolkitImage,
                    toolkit.ToolkitName,
                    "toolkit")
            })
            .OrderBy(toolkit => GetCampaignMonth(toolkit.Name))
            .ThenBy(toolkit => toolkit.Name)
            .ToArray();

        var webinarQuery = new ContentItemQueryBuilder()
            .ForContentType(
                WebinarFolder.CONTENT_TYPE_NAME,
                parameters => parameters
                    .WithLinkedItems(3)
                    .Where(where => where.WhereEquals(
                        nameof(WebinarFolder.DocumentName),
                        "Training & Updates")))
            .InLanguage(data.WebPage.LanguageName);

        var webinarFolders = await contentQueryExecutor
            .GetMappedResult<WebinarFolder>(
                webinarQuery,
                publishedOptions);

        var webinars = webinarFolders
            .SelectMany(folder =>
                folder.SelectedWebinars ?? Enumerable.Empty<Webinar>())
            .Select(webinar => new WebinarCard
            {
                Name = webinar.WebinarName ?? string.Empty,
                Description =
                    webinar.WebinarDescription ?? string.Empty,
                RecordingUrl =
                    webinar.WebinarRecordingUrl ?? string.Empty,
                ComingSoonDate = webinar.WebinarComingSoon,
                ImageUrl = GetImageUrl(
                    webinar.WebinarImage,
                    webinar.WebinarName,
                    "webinar")
            })
            .ToArray();

        var incentives = new List<IncentiveCard>();

        var portalPath = page.SystemFields?.WebPageItemTreePath;

        if (!string.IsNullOrWhiteSpace(portalPath))
        {
            var childPages = await contentRetriever.RetrievePages<Incentive>(
                new RetrievePagesParameters
                {
                    PathMatch = PathMatch.Children(portalPath),
                    LinkedItemsMaxLevel = 1,
                    IsForPreview = false
                });

            foreach (var incentive in childPages)
            {
                incentives.Add(new IncentiveCard
                {
                    Name = incentive.IncentiveName ?? string.Empty,
                    Description =
                        incentive.IncentiveDescription ?? string.Empty,
                    Destination =
                        incentive.IncentiveDestination ?? string.Empty,
                    ImageUrl = GetImageUrl(
                        incentive.IncentiveImage,
                        incentive.IncentiveName,
                        "incentive"),
                    Url = incentive.GetUrl().RelativePath
                });
            }

            logger.LogInformation(
                "Portal '{PortalPath}' returned {Count} published incentives.",
                portalPath,
                incentives.Count);
        }
        else
        {
            logger.LogWarning(
                "The agent portal has no mapped web-page tree path.");
        }

        var model = new AgentPortalPageViewModel
        {
            Heading = page.AgentPortalHeading ?? string.Empty,
            MainDescription =
                page.AgentPortalMainDescription ?? string.Empty,

            BookingHeading =
                page.AgentPortalBookingHeading ?? string.Empty,
            BookingDescription =
                page.AgentPortalBookingDescription ?? string.Empty,
            InstructionHeading =
                page.AgentPortalInstructionHeading ?? string.Empty,
            InstructionDescription =
                page.AgentPortalInstructionDescription ?? string.Empty,
            BookingOptionsHeading =
                page.AgentPortalBookingOptionsHeading ?? string.Empty,
            BookingOptionsDescription =
                page.AgentPortalBookingOptionsDescription ?? string.Empty,

            PaymentsHeading =
                page.AgentPortalPaymentsHeading ?? string.Empty,
            PaymentsDescription =
                page.AgentPortalPaymentsDescription ?? string.Empty,

            ToolkitHeading =
                page.AgentPortalTookitHeading ?? string.Empty,
            ToolkitDescription =
                page.AgentPortalToolkitDescription ?? string.Empty,

            IncentiveHeading =
                page.AgentPortalIncentiveHeading ?? string.Empty,
            IncentiveDescription =
                page.AgentPortalIncentiveDescription ?? string.Empty,

            WebinarHeading =
                page.AgentPortalWebinarHeading ?? string.Empty,
            WebinarDescription =
                page.AgentPortalWebinarDescription ?? string.Empty,

            Toolkits = toolkits,
            Incentives = incentives,
            Webinars = webinars
        };


        if (page is IMetaDataSchema metaFields)
        {
            metaService.SetMeta(metaFields);
        }

        return View(
            "~/Features/AgentPortal/AgentPortalPage_Default.cshtml",
            model);
    }

    private static DateTime GetCampaignMonth(string name)
    {
        var match = Regex.Match(
            name,
            @"\((?<month>[A-Za-z]+\s+\d{4})\)\s*$");

        if (match.Success &&
            DateTime.TryParseExact(
                match.Groups["month"].Value,
                "MMMM yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var month))
        {
            return month;
        }

        return DateTime.MaxValue;
    }

    private string? GetImageUrl(
        IEnumerable<IContentItemFieldsSource>? linkedItems,
        string? itemName,
        string itemKind)
    {
        var items = linkedItems?.ToArray();

        if (items is null || items.Length == 0)
        {
            logger.LogWarning(
                "{ItemKind} '{ItemName}' has no linked image.",
                itemKind,
                itemName);

            return null;
        }

        var mediaFile = items.OfType<MediaFile>().FirstOrDefault();

        if (mediaFile is null)
        {
            logger.LogWarning(
                "{ItemKind} '{ItemName}' image has mapped type " +
                "'{MappedType}', not MediaFile.",
                itemKind,
                itemName,
                items[0].GetType().FullName);

            return null;
        }

        var imageUrl = ImageAssetViewModel.Create(mediaFile)
            .LegacyMediaFileAsset?.Url;

        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            logger.LogWarning(
                "{ItemKind} '{ItemName}' mapped to MediaFile, " +
                "but its legacy media URL is empty.",
                itemKind,
                itemName);
        }

        return imageUrl;
    }
}

public sealed class AgentPortalPageViewModel
{
    public string Heading { get; init; } = string.Empty;
    public string MainDescription { get; init; } = string.Empty;
    public string BookingHeading { get; init; } = string.Empty;
    public string BookingDescription { get; init; } = string.Empty;
    public string InstructionHeading { get; init; } = string.Empty;
    public string InstructionDescription { get; init; } = string.Empty;
    public string BookingOptionsHeading { get; init; } = string.Empty;
    public string BookingOptionsDescription { get; init; } = string.Empty;
    public string PaymentsHeading { get; init; } = string.Empty;
    public string PaymentsDescription { get; init; } = string.Empty;
    public string ToolkitHeading { get; init; } = string.Empty;
    public string ToolkitDescription { get; init; } = string.Empty;
    public string IncentiveHeading { get; init; } = string.Empty;
    public string IncentiveDescription { get; init; } = string.Empty;
    public string WebinarHeading { get; init; } = string.Empty;
    public string WebinarDescription { get; init; } = string.Empty;

    public IReadOnlyList<ToolkitCard> Toolkits { get; init; }
        = Array.Empty<ToolkitCard>();

    public IReadOnlyList<IncentiveCard> Incentives { get; init; }
        = Array.Empty<IncentiveCard>();

    public IReadOnlyList<WebinarCard> Webinars { get; init; }
        = Array.Empty<WebinarCard>();
}

public sealed class ToolkitCard
{
    public string Name { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
}

public sealed class IncentiveCard
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Destination { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
}

public sealed class WebinarCard
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string RecordingUrl { get; init; } = string.Empty;
    public DateTime ComingSoonDate { get; init; }
    public string? ImageUrl { get; init; }
}