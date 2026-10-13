using CMS.Websites.Routing;
using ETG.Core.Content;
using ETG.Core.Content.Pages;
using ETG.Web.Features.AgentIncentive.Operations;
using ETG.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using MediaFile = ETG.Core.Content.MediaFile;

[assembly: RegisterPageTemplate(
    identifier: "ETG.AgentIncentiveTemplate",
    name: "Agent Incentive",
    propertiesType: typeof(
        ETG.Web.Features.AgentIncentive.AgentIncentivePageTemplateProperties),
    customViewName:
        "~/Features/AgentIncentive/AgentIncentivePage_Default.cshtml",
    ContentTypeNames = [Incentive.CONTENT_TYPE_NAME])]

[assembly: RegisterWebPageRoute(
    contentTypeName: Incentive.CONTENT_TYPE_NAME,
    controllerType: typeof(
        ETG.Web.Features.AgentIncentive.AgentIncentivePageTemplateController))]

namespace ETG.Web.Features.AgentIncentive;

public class AgentIncentivePageTemplateProperties : IPageTemplateProperties
{
}

public class AgentIncentivePageTemplateController(
    IMediator mediator,
    IWebPageDataContextRetriever contextRetriever,
    IWebsiteChannelContext websiteChannelContext,
    WebPageMetaService metaService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var model = await LoadViewModelAsync(
            mediator,
            contextRetriever,
            websiteChannelContext,
            HttpContext.RequestAborted);

        if (model is null)
        {
            return NotFound();
        }

        if (model.Page is IMetaDataSchema metaFields)
        {
            metaService.SetMeta(
                metaFields,
                model.HeroImageUrl);
        }

        return new TemplateResult(model);
    }

    public static async Task<AgentIncentivePageViewModel?> LoadViewModelAsync(
        IMediator mediator,
        IWebPageDataContextRetriever contextRetriever,
        IWebsiteChannelContext websiteChannelContext,
        CancellationToken cancellationToken)
    {
        if (!contextRetriever.TryRetrieve(out var context))
        {
            return null;
        }

        var incentive = await mediator.Send(
            new AgentIncentivePageQuery(
                context.WebPage,
                websiteChannelContext.IsPreview),
            cancellationToken);

        var heading = !string.IsNullOrWhiteSpace(incentive.IncentiveName)
            ? incentive.IncentiveName
            : incentive.DocumentName ?? string.Empty;

        var heroImage = incentive.IncentiveHeroImage?
            .OfType<MediaFile>()
            .FirstOrDefault();

        return new AgentIncentivePageViewModel
        {
            Page = incentive,
            Heading = heading,
            HeroImageUrl = heroImage?.LegacyMediaFileAsset?.Url,
            HeroImageAlt = !string.IsNullOrWhiteSpace(
                heroImage?.LegacyMediaFileTitle)
                ? heroImage.LegacyMediaFileTitle
                : heading
        };
    }
}

public class AgentIncentivePageViewModel
{
    public string Heading { get; init; } = string.Empty;
    public string? HeroImageUrl { get; init; }
    public string HeroImageAlt { get; init; } = string.Empty;
    public Incentive? Page { get; init; }
}