using CMS.Websites.Routing;
using ETG.Core.Models;
using ETG.Web.Features.Unsubscribe.Operations;
using ETG.Web.Infrastructure;
using ETG.Web.Models;
using ETG.Web.Services.Unsubscribe;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PageContent = ETG.Core.Content.Page;

[assembly: RegisterPageTemplate(
    identifier: "ETG.UnsubscribeTemplate",
    name: "Unsubscribe",
    propertiesType:
        typeof(ETG.Web.Features.Unsubscribe.UnsubscribeTemplateProperties),
    customViewName:
        "~/Features/Unsubscribe/Unsubscribe.cshtml",
    ContentTypeNames = new[] { PageContent.CONTENT_TYPE_NAME }
)]

[assembly: RegisterWebPageRoute(
    contentTypeName: PageContent.CONTENT_TYPE_NAME,
    controllerType:
        typeof(ETG.Web.Features.Unsubscribe.UnsubscribePageController),
    Path = "/Newsletter/Unsubscribe"
)]

namespace ETG.Web.Features.Unsubscribe;

public sealed class UnsubscribeTemplateProperties : IPageTemplateProperties
{
}

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class UnsubscribePageController : Controller
{
    private readonly IMediator mediator;
    private readonly IWebPageDataContextRetriever contextRetriever;
    private readonly IWebsiteChannelContext channelContext;
    private readonly WebPageMetaService metaService;
    private readonly IUnsubscribeReasonService reasonService;
    private readonly UnsubscribeLinkService linkService;
    private readonly ReCaptchaSettings captchaSettings;

    public UnsubscribePageController(
        IMediator mediator,
        IWebPageDataContextRetriever contextRetriever,
        IWebsiteChannelContext channelContext,
        WebPageMetaService metaService,
        IUnsubscribeReasonService reasonService,
        UnsubscribeLinkService linkService,
        IOptions<ReCaptchaSettings> captchaOptions)
    {
        this.mediator = mediator;
        this.contextRetriever = contextRetriever;
        this.channelContext = channelContext;
        this.metaService = metaService;
        this.reasonService = reasonService;
        this.linkService = linkService;
        captchaSettings = captchaOptions.Value;
    }

    public async Task<IActionResult> Index(
        [FromQuery] string? recipient,
        CancellationToken cancellationToken)
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";

        if (!contextRetriever.TryRetrieve(out var context))
        {
            return NotFound();
        }

        var routedPage = context.WebPage;

        var page = await mediator.Send(
            new GetUnsubscribePageQuery(
                routedPage.WebPageItemID,
                routedPage.WebsiteChannelName,
                routedPage.LanguageName,
                channelContext.IsPreview),
            cancellationToken);

        if (page is null)
        {
            return NotFound();
        }

        // Retains your project's existing media mapping.
        var heroImage = page.PageHeroImage?
            .OfType<ETG.Core.Content.MediaFile>()
            .FirstOrDefault();

        var imageUrl =
            heroImage?.LegacyMediaFileAsset?.Url ?? string.Empty;

        metaService.SetMeta(page, imageUrl);

        var currentPath = $"{Request.PathBase}{Request.Path}";

        var validRecipient =
            linkService.TryRead(recipient, out var ticket)
            && string.Equals(
                ticket!.PagePath,
                currentPath,
                StringComparison.OrdinalIgnoreCase)
            && string.Equals(
                ticket.LanguageName,
                routedPage.LanguageName,
                StringComparison.OrdinalIgnoreCase);

        var reasons = await reasonService.GetReasonsAsync(
            routedPage.LanguageName,
            cancellationToken);

        var error = TempData["Unsubscribe.Error"] as string;

        var captchaConfigured =
            captchaSettings.IsValidationDisabled
            || (!string.IsNullOrWhiteSpace(captchaSettings.SiteKey)
                && !string.IsNullOrWhiteSpace(captchaSettings.SecretKey));

        if (!channelContext.IsPreview && !validRecipient)
        {
            error = "This recipient link is missing, invalid, or expired.";
        }
        else if (reasons.Count == 0)
        {
            error = "No published reasons are available.";
        }
        else if (!captchaConfigured)
        {
            error = "Security verification is not configured.";
        }

        Guid? selectedReasonGuid = Guid.TryParse(
            TempData["Unsubscribe.SelectedReason"] as string,
            out var selectedGuid)
                ? selectedGuid
                : null;

        var model = new UnsubscribePageViewModel
        {
            Hero = new HeroViewModel
            {
                Heading = string.IsNullOrWhiteSpace(page.PageHeading)
                    ? page.DocumentName
                    : page.PageHeading,

                HeroImageUrl = imageUrl,
                HeroImageAltText =
                    page.PageHeroImageAltTag ?? string.Empty,
                HeroCaption = page.PageHeroCaption
            },

            Content = page.PageContent ?? string.Empty,

            Form = new UnsubscribeFromViewModel
            {
                Reasons = reasons
        .Select(reason => new SelectListItem
        {
            Value = reason.ItemGuid.ToString("D"),
            Text = reason.Text
        })
        .ToList(),

                ReasonGuid = selectedReasonGuid,

                RecipientToken = validRecipient
        ? recipient!
        : string.Empty,

                Error = error,

                IsPreview = channelContext.IsPreview,

                SkipCaptchaValidation =
        captchaSettings.IsValidationDisabled,
              
            }
        };

        //return new TemplateResult(model);
        return View(
    "~/Features/Unsubscribe/UnsubscribePage_Default.cshtml",
    model);
    }
}