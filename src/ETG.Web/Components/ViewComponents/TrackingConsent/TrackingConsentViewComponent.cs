using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.DataProtection;
using CMS.Helpers;
using CMS.Websites;
using CMS.Websites.Routing;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.Web.Mvc;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using System.Web;

namespace ETG.Web.Components.ViewComponents.TrackingConsent;

public class TrackingConsentViewComponent : ViewComponent
{
    private readonly IInfoProvider<ConsentInfo> consentInfoProvider;
    private readonly IConsentAgreementService consentAgreementService;
    private readonly IPreferredLanguageRetriever currentLanguageRetriever;
    private readonly IWebPageDataContextRetriever webPageDataContextRetriever;
    private readonly IWebPageUrlRetriever urlRetriever;
    private readonly IWebsiteChannelContext websiteChannelContext;
    private readonly ICurrentCookieLevelProvider currentCookieLevelProvider;
    private readonly ICookieAccessor cookieAccessor;
    private readonly IMediator mediator;

    public TrackingConsentViewComponent(
        IInfoProvider<ConsentInfo> consentInfoProvider,
        IConsentAgreementService consentAgreementService,
        IPreferredLanguageRetriever currentLanguageRetriever,
        IWebPageDataContextRetriever webPageDataContextRetriever,
        IWebPageUrlRetriever urlRetriever,
        IWebsiteChannelContext websiteChannelContext,
        ICurrentCookieLevelProvider currentCookieLevelProvider,
        ICookieAccessor cookieAccessor,
        IMediator mediator)
    {
        this.consentInfoProvider = consentInfoProvider;
        this.consentAgreementService = consentAgreementService;
        this.currentLanguageRetriever = currentLanguageRetriever;
        this.webPageDataContextRetriever = webPageDataContextRetriever;
        this.urlRetriever = urlRetriever;
        this.websiteChannelContext = websiteChannelContext;
        this.currentCookieLevelProvider = currentCookieLevelProvider;
        this.cookieAccessor = cookieAccessor;
        this.mediator = mediator;
    }

    private static string CleanConsentText(string rawText)
    {
        if (string.IsNullOrEmpty(rawText))
        {
            return string.Empty;
        }

        var text = rawText.Trim();

        text = UnwrapOuterTag(text, "pre");
        text = UnwrapOuterTag(text, "p");
        text = HttpUtility.HtmlDecode(text);
        text = Regex.Replace(text, @"<br\s*/?>", string.Empty, RegexOptions.IgnoreCase);
        text = text.Replace('\u00A0', ' ');
        text = Regex.Replace(text, @"\s+", " ").Trim();

        return text;
    }

    private static string UnwrapOuterTag(string text, string tagName)
    {
        var match = Regex.Match(
            text,
            $@"^<{tagName}[^>]*>(.*)</{tagName}>$",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        return match.Success ? match.Groups[1].Value.Trim() : text;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        const string PRIVACY_PAGE_TREE_PATH = "/legals/privacy-policy";
        var consent = consentInfoProvider.Get(Constants.CONSENT_AGREE);

        if (consent != null)
        {
            var currentLanguage = currentLanguageRetriever.Get();
            var contact = ContactManagementContext.CurrentContact;
            var consentText = await consent.GetConsentTextAsync(currentLanguage);

            if (contact != null && !consentAgreementService.IsAgreed(contact, consent))
            {
                var defaultCookieLevel = currentCookieLevelProvider.GetDefaultCookieLevel();
                currentCookieLevelProvider.SetCurrentCookieLevel(defaultCookieLevel);
            }

            // ViewComponent.InvokeAsync() has no CancellationToken parameter of its own,
            // so HttpContext.RequestAborted stands in for it here - same role as the
            // cancellationToken passed through in GetNavigationHierarchyQueryHandler.
            var layoutResult = await mediator.Send(
    new ETG.Web.Components.ViewComponents.Shared.GetWebsiteLayoutContentQuery(),
    CancellationToken.None);

            var consentModel = new ConsentViewModel
            {
                ConsentShortText = CleanConsentText(consentText.ShortText),
                ConsentFullText = CleanConsentText(consentText.FullText),
                PrivacyPageUrl = Url.Content((await urlRetriever.Retrieve(PRIVACY_PAGE_TREE_PATH, websiteChannelContext.WebsiteChannelName, currentLanguage)).RelativePath),
                ReturnPageUrl = webPageDataContextRetriever.TryRetrieve(out var currentWebPageContext)
                    ? (await urlRetriever.Retrieve(currentWebPageContext.WebPage.WebPageItemID, currentLanguage)).RelativePath
                    : (HttpContext.Request.PathBase + HttpContext.Request.Path).Value,
                IsAgreed = contact != null && consentAgreementService.IsAgreed(contact, consent),
                IsRevoke = currentCookieLevelProvider.GetCurrentCookieLevel() == Kentico.Web.Mvc.CookieLevel.System.Level,
                Layout = layoutResult.HasValue ? layoutResult.Value : null
            };

            return View("~/Components/ViewComponents/TrackingConsent/TrackingConsent.cshtml", consentModel);
        }

        return Content(string.Empty);
    }
}

public class ConsentViewModel
{
    public string ConsentShortText { get; set; }
    public string ConsentFullText { get; set; }
    public bool IsAgreed { get; set; }
    public bool IsRevoke { get; set; }
    public string ReturnPageUrl { get; set; }
    public string PrivacyPageUrl { get; set; }
    public ETG.Web.Components.ViewComponents.Shared.WebsiteLayoutContentViewModel? Layout { get; init; }
}