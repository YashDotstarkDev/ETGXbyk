using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.DataProtection;
using CMS.Helpers;
using ETG.Web.Components.ViewComponents.TrackingConsent;
using Kentico.Web.Mvc;
using Microsoft.AspNetCore.Mvc;

namespace ETG.Web.Controllers;

public class ConsentController : Controller
{
    private readonly ICurrentCookieLevelProvider cookieLevelProvider;
    private readonly IConsentAgreementService consentAgreementService;
    private readonly IInfoProvider<ConsentInfo> consentInfoProvider;
    private readonly ICookieAccessor cookieAccessor;

    public ConsentController(
        ICurrentCookieLevelProvider cookieLevelProvider,
        IConsentAgreementService consentAgreementService,
        IInfoProvider<ConsentInfo> consentInfoProvider,
        ICookieAccessor cookieAccessor)
    {
        this.cookieLevelProvider = cookieLevelProvider;
        this.consentAgreementService = consentAgreementService;
        this.consentInfoProvider = consentInfoProvider;
        this.cookieAccessor = cookieAccessor;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ProcessConsent(string consentChoice)
    {
        var consent = consentInfoProvider.Get(Constants.CONSENT_AGREE);
        if (consent == null)
        {
            return BadRequest();
        }

        if (consentChoice == "All cookies allow")
        {
            Agree(consent);
        }
        else if (consentChoice == "Decline")
        {
            Decline(consent);
        }
        else
        {
            return BadRequest();
        }

        return Ok();
    }
    private ActionResult Agree(ConsentInfo consent) 
    {
        //var consent = consentInfoProvider.Get(Constants.CONSENT_AGREE);

        if (consent != null)
        {
            cookieLevelProvider.SetCurrentCookieLevel(Kentico.Web.Mvc.CookieLevel.All.Level);

            var contact = ContactManagementContext.CurrentContact;
            if (contact != null)
            {
                consentAgreementService.Agree(contact, consent);
            }

            //return Redirect(returnUrl);
        }

        return new StatusCodeResult(StatusCodes.Status400BadRequest);
    }
    private ActionResult Decline(ConsentInfo consent)
    {
        //var consent = consentInfoProvider.Get(Constants.CONSENT_AGREE);

        if (consent != null)
        {
            cookieLevelProvider.SetCurrentCookieLevel(Kentico.Web.Mvc.CookieLevel.System.Level); // -100

            var contact = ContactManagementContext.CurrentContact;
            if (contact != null)
            {
                consentAgreementService.Revoke(contact, consent);
            }

            cookieAccessor.Set(Constants.CONSENT_REVOKE, "true", new CookieOptions { Expires = DateTimeOffset.Now.AddYears(1) });

            //return Redirect(returnUrl);
        }

        return new StatusCodeResult(StatusCodes.Status400BadRequest);
    }
}
