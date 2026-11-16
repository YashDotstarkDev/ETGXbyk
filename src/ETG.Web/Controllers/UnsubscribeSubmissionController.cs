using System;
using System.Threading;
using System.Threading.Tasks;
using ETG.Web.Configuration;
using ETG.Web.Infrastructure;
using ETG.Web.Models;
using ETG.Web.Services.Unsubscribe;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ETG.Web.Controllers;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class UnsubscribeSubmissionController : Controller
{
    // CMS content-tree paths, not browser URLs.
    private const string SuccessPagePath =
        "/Newsletter/Unsubscribe/Unsubscribed";

    private const string CancelPagePath =
        "/Newsletter/Unsubscribe/Unsubscription-Canceled";

    private const string FailurePagePath =
        "/Newsletter/Unsubscribe/Unsubscription-Failed";

    private const string DefaultLanguageName = "en-AU";

    private const string CaptchaAction = "unsubscribe_reason";

    private readonly UnsubscribeLinkService linkService;
    private readonly CaptchaValidator captchaValidator;
    private readonly IContactUnsubscribeService contactService;
    private readonly UnsubscribeRedirectService redirectService;
    private readonly UnsubscribeOptions options;
    private readonly ILogger<UnsubscribeSubmissionController> logger;

    public UnsubscribeSubmissionController(
        UnsubscribeLinkService linkService,
        CaptchaValidator captchaValidator,
        IContactUnsubscribeService contactService,
        UnsubscribeRedirectService redirectService,
        IOptions<UnsubscribeOptions> options,
        ILogger<UnsubscribeSubmissionController> logger)
    {
        this.linkService = linkService;
        this.captchaValidator = captchaValidator;
        this.contactService = contactService;
        this.redirectService = redirectService;
        this.options = options.Value;
        this.logger = logger;
    }

    [HttpPost("~/unsubscribe/submit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(
        [FromForm] UnsubscribeInputModel input,
        [FromForm] string? command,
        CancellationToken cancellationToken)
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";

        // Cancel does not modify any contact or subscription.
        // Handle it before recipient validation.
        if (string.Equals(
                command,
                "cancel",
                StringComparison.Ordinal))
        {
            return await RedirectToCmsPageAsync(
                CancelPagePath,
                DefaultLanguageName,
                cancellationToken);
        }

        // Retain the existing protected-recipient validation.
        if (!linkService.TryRead(input.RecipientToken, out var ticket)
            || ticket is null
            || !Url.IsLocalUrl(ticket.PagePath))
        {
            return await FailureResultAsync(
                DefaultLanguageName,
                cancellationToken);
        }

        var languageName = ticket.LanguageName;

        if (!ModelState.IsValid
            || input.ReasonGuid is null
            || input.ReasonGuid.Value == Guid.Empty)
        {
            return await FailureResultAsync(
                languageName,
                cancellationToken);
        }

        try
        {
            /*
             * Preserved from your original controller:
             * CAPTCHA validation is disabled for local testing.
             * Restore before deploying publicly.

            var captchaResponse = new UnsubscribeCaptchaResponse
            {
                CaptchaToken = input.CaptchaToken ?? string.Empty
            };

            var captchaResult = await captchaValidator.ValidateCaptcha(
                captchaResponse,
                CaptchaAction,
                options.CaptchaAllowedHostnames,
                cancellationToken);

            if (!captchaResult.IsSuccess)
            {
                return await FailureResultAsync(
                    languageName,
                    cancellationToken);
            }
            */

            // Existing behavior: save the selected reason only.
            var saved = await contactService.SetUnsubscribeReasonAsync(
                ticket.ContactId,
                languageName,
                input.ReasonGuid.Value,
                cancellationToken);

            if (!saved)
            {
                return await FailureResultAsync(
                    languageName,
                    cancellationToken);
            }

            return await RedirectToCmsPageAsync(
                SuccessPagePath,
                languageName,
                cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Unable to process the unsubscribe reason.");

            return await FailureResultAsync(
                languageName,
                cancellationToken);
        }
    }

    private Task<IActionResult> FailureResultAsync(
        string languageName,
        CancellationToken cancellationToken)
    {
        return RedirectToCmsPageAsync(
            FailurePagePath,
            languageName,
            cancellationToken);
    }

    private async Task<IActionResult> RedirectToCmsPageAsync(
        string treePath,
        string languageName,
        CancellationToken cancellationToken)
    {
        try
        {
            var url = await redirectService.GetUrlAsync(
                treePath,
                languageName,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(url)
                || !Url.IsLocalUrl(url))
            {
                logger.LogError(
                    "CMS page {TreePath} did not resolve to a local URL.",
                    treePath);

                return StatusCode(500);
            }

            return LocalRedirect(url);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Unable to resolve CMS page {TreePath} for {LanguageName}.",
                treePath,
                languageName);

            return StatusCode(500);
        }
    }

    private sealed class UnsubscribeCaptchaResponse
        : ICaptchaClientResponse
    {
        public string CaptchaToken { get; set; } = string.Empty;
    }
}