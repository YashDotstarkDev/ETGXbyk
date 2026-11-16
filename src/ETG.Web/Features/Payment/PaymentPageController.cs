using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CMS.ContentEngine;
using CMS.Core;
using CMS.DataEngine;
using CMS.EmailEngine;
using CMS.EmailLibrary;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG;
using ETG.Core.Content;
using ETG.Core.Models.Payment;
using ETG.Module.Booking.Classes.Info;
using ETG.Web.Features.Payment;
using ETG.Web.Features.Payment.Operations;
using ETG.Web.Infrastructure;
using ETG.Web.Rendering;
using ETG.Web.Services.Payment;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

[assembly: RegisterPageTemplate(
    identifier: "ETG.PaymentPageTemplate",
    name: "Payment Page Template",
    propertiesType: typeof(PaymentPageTemplateProperties),
    customViewName: "~/Features/Payment/PaymentPage.cshtml",
    ContentTypeNames = new[] { PaymentPage.CONTENT_TYPE_NAME }
)]

[assembly: RegisterWebPageRoute(
    contentTypeName: PaymentPage.CONTENT_TYPE_NAME,
    controllerType: typeof(PaymentPageController),
    WebsiteChannelNames = new[] { "ETG" }
)]

namespace ETG.Web.Features.Payment;

public sealed class PaymentPageTemplateProperties : IPageTemplateProperties
{
}

public class PaymentPageController : Controller
{
    private readonly IMediator _mediator;
    private readonly IWebPageDataContextRetriever _contextRetriever;
    private readonly IWebPageDataContextInitializer _contextInitializer;
    private readonly IInfoProvider<PaymentInfo> _paymentProvider;
    private readonly ITravelPayService _travelPayService;

    private readonly CaptchaValidator _captchaValidator;
    private readonly IEmailService _emailService;
    private readonly IInfoProvider<EmailTemplateInfo> _emailTemplateProvider;
    private readonly IContentQueryExecutor _queryExecutor;
    private readonly IWebsiteChannelContext _channelContext;
    private readonly WebPageMetaService _metaService;
    private readonly IEventLogService _eventLogService;
    private readonly ILogger<PaymentPageController> _logger;

    public PaymentPageController(
        IMediator mediator,
        IWebPageDataContextRetriever contextRetriever,
        IWebPageDataContextInitializer contextInitializer,
        IInfoProvider<PaymentInfo> paymentProvider,
        ITravelPayService travelPayService,
        CaptchaValidator captchaValidator,
        IEmailService emailService,
        IInfoProvider<EmailTemplateInfo> emailTemplateProvider,
        IContentQueryExecutor queryExecutor,
        IWebsiteChannelContext channelContext,
        WebPageMetaService metaService, IEventLogService eventLogService,
        ILogger<PaymentPageController> logger)
    {
        _mediator = mediator;
        _contextRetriever = contextRetriever;
        _contextInitializer = contextInitializer;
        _paymentProvider = paymentProvider;
        _travelPayService = travelPayService;
        _captchaValidator = captchaValidator;
        _emailService = emailService;
        _emailTemplateProvider = emailTemplateProvider;
        _queryExecutor = queryExecutor;
        _channelContext = channelContext;
        _metaService = metaService; _eventLogService = eventLogService;
        _logger = logger;
    }

    [HttpGet("/clients-payments")]
    public async Task<IActionResult> Index()
    {
        PaymentPage? paymentPage = null;
        IEnumerable<FAQ> faqs = Enumerable.Empty<FAQ>();
        bool hasContext = false;

        // 1. Resolve via active Kentico Web Page context (CQRS)
        if (_contextRetriever.TryRetrieve(out var data) && data.WebPage != null)
        {
            var response = await _mediator.Send(new PaymentPageQuery((RoutedWebPage)data.WebPage));
            paymentPage = response.Page;
            faqs = response.FAQs;
            hasContext = true;
        }
        else
        {
            // 2. Fallback if accessed directly by route
            var fallback = await LoadFallbackPaymentDataAsync();
            paymentPage = fallback.Page;
            faqs = fallback.FAQs;

            if (paymentPage != null)
            {
                var routedPage = new RoutedWebPage
                {
                    WebPageItemID = paymentPage.SystemFields.WebPageItemID,
                    WebPageItemGUID = paymentPage.SystemFields.WebPageItemGUID,
                    LanguageID = paymentPage.SystemFields.ContentItemCommonDataContentLanguageID,
                    LanguageName = "en",
                    ContentTypeName = PaymentPage.CONTENT_TYPE_NAME,
                    WebsiteChannelName = !string.IsNullOrWhiteSpace(_channelContext.WebsiteChannelName)
                        ? _channelContext.WebsiteChannelName
                        : "ETG"
                };

                _contextInitializer.Initialize(routedPage);
                hasContext = true;
            }
        }

        if (paymentPage == null)
        {
            _logger.LogWarning("Payment page content item could not be retrieved from CMS.");
        }

        var settingsResponse = await _mediator.Send(new WebsiteSettingsQuery());
        var model = PaymentPageViewModel.Create(paymentPage, faqs, settingsResponse.GlobalContent );
        model.HasWebPageContext = hasContext;

        if (paymentPage != null)
        {
            _metaService.SetMeta(paymentPage, model.HeroImageUrl);
        }

        return View("~/Features/Payment/PaymentPage.cshtml", model);
    }

    [HttpPost("/api/payment/create")]
    [HttpPost("/api/booking/createpayment")]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request)
    {
        if (request == null)
        {
            return BadRequest(new CreatePaymentResponse { Success = false, Message = "Invalid request payload." });
        }

        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage
                             ?? "Please verify the form details.";
            return BadRequest(new CreatePaymentResponse { Success = false, Message = firstError });
        }

        if (request.Amount <= 0)
        {
            return BadRequest(new CreatePaymentResponse { Success = false, Message = "Payment amount must be greater than zero." });
        }

        // Validate Google ReCaptcha
        var captchaResult = await _captchaValidator.ValidateCaptcha(request.RecaptchaToken, "client_payment_submit");
        if (!captchaResult.IsSuccess)
        {
            _logger.LogWarning(
                "ReCaptcha validation failed during payment creation for invoice {Invoice}: {Error}",
                request.InvoiceReference,
                captchaResult.ErrorMessage);
            return BadRequest(new CreatePaymentResponse
            {
                Success = false,
                Message = !string.IsNullOrWhiteSpace(captchaResult.ErrorMessage)
                    ? captchaResult.ErrorMessage
                    : "Security verification failed. Please refresh and try again."
            });
        }

        try
        {
            var settings = await _mediator.Send(new WebsiteSettingsQuery());
            var gc = settings.GlobalContent;
            bool isProd = string.Equals(gc.TravelPayEnvironment, "Production", StringComparison.OrdinalIgnoreCase);
            string configuredApiKey = isProd
                ? (gc.TravelPayProductionApiKey ?? string.Empty)
                : (gc.TravelPaySandboxApiKey ?? string.Empty);
            string configuredMerchantCode = isProd
                ? (gc.TravelPayProductionMerchantCode ?? string.Empty)
                : (gc.TravelPaySandboxMerchantCode ?? string.Empty);

            if (string.IsNullOrWhiteSpace(configuredApiKey) || string.IsNullOrWhiteSpace(configuredMerchantCode))
            {
                _logger.LogError("TravelPay configuration is missing in Kentico WebsiteGlobalContent. ApiKey or MerchantCode is empty.");
                return StatusCode(500, new CreatePaymentResponse
                {
                    Success = false,
                    Message = "TravelPay gateway is not properly configured on this server. Please ensure credentials are set in Kentico Website Settings."
                });
            }

            var payment = new PaymentInfo
            {
                PaymentGuid = Guid.NewGuid(),
                PaymentCreated = DateTime.Now,
                PaymentLastModified = DateTime.Now,
                PaymentFirstName = request.FirstName.Trim(),
                PaymentLastName = request.LastName.Trim(),
                PaymentEmail = request.Email.Trim(),
                PaymentContactNumber = request.Phone?.Trim() ?? string.Empty,
                PaymentInvoiceReference = request.InvoiceReference.Trim(),
                PaymentAmount = request.Amount,
                PaymentAmountPaid = 0,
                PaymentMerchantUniquePaymentId = string.Empty
            };

            _paymentProvider.Set(payment);

            // Set unique identifier
            payment.PaymentMerchantUniquePaymentId = $"P{payment.PaymentID}";
            _paymentProvider.Set(payment);

            string redirectUrl = $"{Request.Scheme}://{Request.Host}/payment/process";
            var clientParams = await _travelPayService.BuildClientParameters(payment, redirectUrl);

            // Stage 1 EventLog: Payment Initiated (Ongoing)
            string initLog = $"Payment Initiated (Ongoing)\n----------------------------------------\nPayment ID: {payment.PaymentID} (MUPID: P{payment.PaymentID})\nInvoice Reference: {payment.PaymentInvoiceReference}\nCustomer: {payment.PaymentFirstName} {payment.PaymentLastName} ({payment.PaymentEmail})\nPhone: {payment.PaymentContactNumber}\nAmount: ${payment.PaymentAmount:F2} AUD\nInitiated At: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\nStatus: Modal Launched (Ongoing)";
            if (gc.PaymentEventLogging)
            {
                _eventLogService.LogInformation("PaymentGateway", "PaymentInitiated", initLog);
            }




            _logger.LogInformation("Payment {PaymentID} initiated for invoice {Invoice}, amount ${Amount:F2} AUD", payment.PaymentID, payment.PaymentInvoiceReference, payment.PaymentAmount);

            return Ok(new CreatePaymentResponse
            {
                Success = true,
                PaymentId = payment.PaymentID,
                TravelPay = clientParams
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment record for invoice {Invoice}", request.InvoiceReference);
            return StatusCode(500, new CreatePaymentResponse { Success = false, Message = "An error occurred while initiating the payment. Please try again." });
        }
    }

    [HttpPost("/api/payment/log-stage")]
    public async Task<IActionResult> LogPaymentStage([FromBody] PaymentStageLogRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { success = false, message = "Invalid request payload." });
        }

        var settings = await _mediator.Send(new WebsiteSettingsQuery());
        if (settings.GlobalContent == null || !settings.GlobalContent.PaymentEventLogging)
        {
            return Ok(new { success = true, logged = false });
        }

        int paymentId = request.PaymentId;
        if (paymentId <= 0 && !string.IsNullOrWhiteSpace(request.Mupid))
        {
            string clean = request.Mupid.StartsWith("P", StringComparison.OrdinalIgnoreCase)
                ? request.Mupid[1..]
                : request.Mupid;
            int.TryParse(clean, out paymentId);
        }

        PaymentInfo? payment = paymentId > 0 ? GetPaymentById(paymentId) : null;
        string mupid = payment?.PaymentMerchantUniquePaymentId ?? request.Mupid ?? (paymentId > 0 ? $"P{paymentId}" : "Unknown");
        string invoiceRef = payment?.PaymentInvoiceReference ?? (!string.IsNullOrWhiteSpace(request.InvoiceReference) ? request.InvoiceReference : "Unknown");
        string customerName = payment != null ? $"{payment.PaymentFirstName} {payment.PaymentLastName}".Trim() : "Customer";
        string customerEmail = payment?.PaymentEmail ?? "Unknown";
        double amount = payment?.PaymentAmount ?? request.Amount;

        string stage = request.Stage?.Trim() ?? "PaymentStageUpdate";

        switch (stage.ToLowerInvariant())
        {
            case "paymentmethodattempted":
            case "proceedclicked":
            {
                string method = !string.IsNullOrWhiteSpace(request.PaymentMethod) ? request.PaymentMethod : "Credit Card";
                string desc = $"Payment Attempted / Proceed Clicked\n----------------------------------------\nPayment ID: {paymentId} (MUPID: {mupid})\nInvoice Reference: {invoiceRef}\nSelected Method: {method}\nAmount: ${amount:F2} AUD\nCustomer: {customerName} ({customerEmail})\nTimestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\nStatus: Customer submitted payment in modal";
                _eventLogService.LogInformation("PaymentGateway", "PaymentMethodAttempted", desc);
                _logger.LogInformation("Payment {PaymentID} method attempted: {Method} for invoice {Invoice}", paymentId, method, invoiceRef);
                break;
            }

            case "paymentmodaldecline":
            case "paymentdeclined":
            {
                string code = !string.IsNullOrWhiteSpace(request.FailureCode) ? request.FailureCode : "2003";
                string reason = !string.IsNullOrWhiteSpace(request.FailureReason) ? request.FailureReason : "The transaction was declined";
                string method = !string.IsNullOrWhiteSpace(request.PaymentMethod) ? request.PaymentMethod : "Credit Card";
                string desc = $"Payment Declined in Modal\n----------------------------------------\nPayment ID: {paymentId} (MUPID: {mupid})\nInvoice Reference: {invoiceRef}\nFailure Code: {code}\nFailure Reason: {reason}\nPayment Method: {method}\nAmount: ${amount:F2} AUD\nCustomer: {customerName} ({customerEmail})\nTimestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\nStatus: Declined by gateway inside popup";
                _eventLogService.LogWarning("PaymentGateway", "PaymentFailed", desc);
                _logger.LogWarning("Payment {PaymentID} declined in modal: Code={Code}, Reason={Reason}", paymentId, code, reason);
                break;
            }

            case "paymentmodalclosed":
            case "modalclosed":
            {
                bool alreadySettled = payment != null && payment.PaymentAmountPaid > 0 && !string.IsNullOrWhiteSpace(payment.PaymentReference);
                if (!alreadySettled)
                {
                    string details = !string.IsNullOrWhiteSpace(request.AdditionalDetails) ? request.AdditionalDetails : "Customer closed payment popup before completion";
                    string desc = $"Payment Modal Closed / Abandoned\n----------------------------------------\nPayment ID: {paymentId} (MUPID: {mupid})\nInvoice Reference: {invoiceRef}\nCustomer: {customerName} ({customerEmail})\nAmount: ${amount:F2} AUD\nTimestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\nStatus: {details}";
                    _eventLogService.LogInformation("PaymentGateway", "PaymentModalClosed", desc);
                    _logger.LogInformation("Payment {PaymentID} modal closed without settlement: {Details}", paymentId, details);
                }
                break;
            }

            default:
            {
                string desc = $"Payment Gateway Event: {stage}\n----------------------------------------\nPayment ID: {paymentId} (MUPID: {mupid})\nInvoice Reference: {invoiceRef}\nCustomer: {customerName} ({customerEmail})\nDetails: {request.AdditionalDetails}\nTimestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                _eventLogService.LogInformation("PaymentGateway", stage, desc);
                break;
            }
        }

        return Ok(new { success = true });
    }

    [HttpGet("/payment/process")]
    [HttpPost("/payment/process")]
    [HttpGet("/payments/process")]
    [HttpPost("/payments/process")]
    public async Task<IActionResult> Process()
    {
        var callbackParams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var q in Request.Query)
        {
            callbackParams[q.Key] = q.Value.ToString();
        }
        if (Request.HasFormContentType)
        {
            foreach (var f in Request.Form)
            {
                callbackParams[f.Key] = f.Value.ToString();
            }
        }

        string GetParam(string key)
        {
            if (Request.HasFormContentType && Request.Form.TryGetValue(key, out var formVal))
            {
                return formVal.ToString();
            }
            if (callbackParams.TryGetValue(key, out var cpVal)) return cpVal;
            return Request.Query[key].ToString();
        }

        string merchantUniqueId = GetParam("MerchantUniquePaymentId"); if (string.IsNullOrWhiteSpace(merchantUniqueId)) merchantUniqueId = GetParam("CustomerReference"); if (string.IsNullOrWhiteSpace(merchantUniqueId)) merchantUniqueId = GetParam("OrderId");
        string paymentReference = GetParam("PaymentReference"); if (string.IsNullOrWhiteSpace(paymentReference)) paymentReference = GetParam("ReceiptNo"); if (string.IsNullOrWhiteSpace(paymentReference)) paymentReference = GetParam("Reference"); if (string.IsNullOrWhiteSpace(paymentReference)) paymentReference = GetParam("TransactionId");

        string failureCode = GetParam("FailureCode");
        string failureReason = GetParam("FailureReason");
        bool isExplicitFailure = !string.IsNullOrWhiteSpace(failureCode) || !string.IsNullOrWhiteSpace(failureReason) || string.Equals(GetParam("result"), "failed", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(merchantUniqueId) || (string.IsNullOrWhiteSpace(paymentReference) && !isExplicitFailure))
        {
            _logger.LogWarning("PaymentProcess called without required parameters: MUPID='{Id}', Ref='{Ref}'. Returning 404.", merchantUniqueId, paymentReference);
            return NotFound();
        }
        if (string.IsNullOrWhiteSpace(paymentReference))
        {
            paymentReference = "N/A (Declined before settlement)";
        }

        string cleanId = merchantUniqueId.StartsWith("P", StringComparison.OrdinalIgnoreCase)
            ? merchantUniqueId[1..]
            : merchantUniqueId;

        if (!int.TryParse(cleanId, out int paymentId) || paymentId <= 0)
        {
            _logger.LogWarning("PaymentProcess called with malformed MerchantUniquePaymentId: {Id}", merchantUniqueId); return NotFound();

        }

        var payment = GetPaymentById(paymentId);
        if (payment == null)
        {
            _logger.LogWarning("PaymentInfo record not found for PaymentID {PaymentID}", paymentId); return NotFound();

        }


        var settings = await _mediator.Send(new WebsiteSettingsQuery());
        bool isLoggingEnabled = settings.GlobalContent?.PaymentEventLogging ?? false;

        var verification = await _travelPayService.VerifyCallbackAsync(callbackParams, payment);
        if (verification.IsInvalidRequest)
        {
            string alertLog = $"Security Alert: Invalid / Tampered Payment Callback\n----------------------------------------\nPayment ID: {paymentId}\nReason: {verification.StatusMessage}\nReceived MUPID: {merchantUniqueId}\nReceived Reference: {paymentReference}\nTimestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            if (isLoggingEnabled)
            {
                _eventLogService.LogError("PaymentGateway", "PaymentSecurityAlert", alertLog);
            }
            _logger.LogWarning("Payment {PaymentID} rejected as invalid/forged request: {Reason}. Returning 404.", paymentId, verification.StatusMessage);
            return NotFound();
        }

        int paymentStatus = verification.PaymentStatus;
        double fundsToMerchant = verification.Amount;
        if (!string.IsNullOrWhiteSpace(verification.PaymentReference)) paymentReference = verification.PaymentReference;
        string cardMasked = GetParam("AccountOrCardNo");
        string cardType = GetParam("PaymentCard");

        var details = new
        {
            PaymentStatus = paymentStatus,
            PaymentStatusString = GetParam("PaymentStatusString"),
            PaymentReference = paymentReference,
            FundsToMerchant = fundsToMerchant,
            AccountOrCardNo = cardMasked,
            PaymentCard = cardType,
            FailureCode = GetParam("FailureCode"),
            FailureReason = !string.IsNullOrWhiteSpace(verification.FailureReason) ? verification.FailureReason : GetParam("FailureReason"),
            VerificationMessage = verification.StatusMessage,
            IsVerified = verification.IsSuccess && verification.IsApproved,
            ProcessedAt = DateTime.UtcNow
        };

        payment.PaymentReference = paymentReference;
        payment.PaymentDetails = JsonSerializer.Serialize(details);
        payment.PaymentLastModified = DateTime.Now;

        bool alreadySettled = payment.PaymentAmountPaid > 0 && !string.IsNullOrWhiteSpace(payment.PaymentReference);
        bool isSuccess = verification.IsSuccess && verification.IsApproved;
        bool isApiOrCallback = Request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase)
                               || (Request.ContentType != null && Request.ContentType.Contains("application/json", StringComparison.OrdinalIgnoreCase));

        if (isSuccess)
        {
            payment.PaymentAmountPaid = fundsToMerchant > 0 ? fundsToMerchant : payment.PaymentAmount;
            _paymentProvider.Set(payment);

            if (isLoggingEnabled)
            {
                // Stage 2 EventLog: Payment Succeeded (Completed)
            string successLog = $"Payment Succeeded (Completed)\n----------------------------------------\nPayment ID: {payment.PaymentID} (MUPID: {payment.PaymentMerchantUniquePaymentId})\nInvoice Reference: {payment.PaymentInvoiceReference}\nTravelPay Reference: {paymentReference}\nAmount Paid: ${payment.PaymentAmountPaid:F2} AUD\nFunds to Merchant: ${fundsToMerchant:F2} AUD\nCard: {cardType} ({cardMasked})\nCustomer: {payment.PaymentFirstName} {payment.PaymentLastName} ({payment.PaymentEmail})\nSettlement Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\nStatus: Approved & Settled\nVerification: {verification.StatusMessage}";
            string methodSuccessLog = $"Payment Method Confirmed\n----------------------------------------\nPayment ID: {payment.PaymentID} (MUPID: {payment.PaymentMerchantUniquePaymentId})\nInvoice Reference: {payment.PaymentInvoiceReference}\nPayment Method: {cardType} ({cardMasked})\nTravelPay Reference: {paymentReference}\nAmount: ${payment.PaymentAmountPaid:F2} AUD\nCustomer: {payment.PaymentFirstName} {payment.PaymentLastName} ({payment.PaymentEmail})\nTimestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\nStatus: Card Authorized & Proceed Completed";
            _eventLogService.LogInformation("PaymentGateway", "PaymentMethodAttempted", methodSuccessLog);

            _eventLogService.LogInformation("PaymentGateway", "PaymentSuccess", successLog);
            }
            _logger.LogInformation("Payment {PaymentID} succeeded. Ref: {Ref}, Amount: ${Amount:F2} AUD", payment.PaymentID, paymentReference, payment.PaymentAmountPaid);

            // Dual Email Dispatch
            if (!alreadySettled)
            {
                await SendConfirmationEmailAsync(payment);
                await SendNotificationEmailAsync(payment, cardMasked, cardType);
            }

            if (isApiOrCallback)
            {
                return Ok(new { success = true, paymentId = payment.PaymentID });
            }

            return Redirect($"/payment/success?pid={payment.PaymentID}");
        }
        else
        {
            _paymentProvider.Set(payment);
            if (isLoggingEnabled)
            {
                // Stage 3 EventLog: Payment Failed / Declined
            string failLog = $"Payment Failed / Declined\n----------------------------------------\nPayment ID: {payment.PaymentID} (MUPID: {payment.PaymentMerchantUniquePaymentId})\nInvoice Reference: {payment.PaymentInvoiceReference}\nCustomer: {payment.PaymentFirstName} {payment.PaymentLastName} ({payment.PaymentEmail})\nExpected Amount: ${payment.PaymentAmount:F2} AUD\nGateway Status: {paymentStatus} ({details.PaymentStatusString})\nFailure Code: {details.FailureCode}\nFailure Reason: {details.FailureReason}\nVerification Message: {details.VerificationMessage}\nCard: {cardType} ({cardMasked})\nTimestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\nStatus: Declined / Cancelled";
            _eventLogService.LogWarning("PaymentGateway", "PaymentFailed", failLog);
            }
            _logger.LogWarning("Payment {PaymentID} failed or was cancelled. Status: {Status}, Reason: {Reason}", payment.PaymentID, paymentStatus, details.FailureReason);

            if (isApiOrCallback)
            {
                return Ok(new { success = false, paymentId = payment.PaymentID });
            }

            return Redirect($"/payment/failed?pid={payment.PaymentID}");
        }
    }

    private async Task<string> ResolveEmailBodyAsync(
        string templateName,
        IReadOnlyDictionary<string, string?> values,
        string fallbackHtml)
    {
        try
        {
            var template = await _emailTemplateProvider.Get()
                .WhereEquals(nameof(EmailTemplateInfo.EmailTemplateName), templateName)
                .FirstOrDefaultAsync();

            if (template != null && !string.IsNullOrWhiteSpace(template.EmailTemplateCode))
            {
                var body = template.EmailTemplateCode;
                foreach (var kvp in values)
                {
                    var val = kvp.Value ?? string.Empty;
                    body = Regex.Replace(
                        body,
                        $@"\{{%\s*{Regex.Escape(kvp.Key)}\s*%\}}",
                        val,
                        RegexOptions.IgnoreCase);
                }
                return body;
            }

            _logger.LogInformation("Email template '{TemplateName}' not found in Kentico, using fallback HTML.", templateName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve EmailTemplate '{TemplateName}' from Kentico. Using fallback HTML.", templateName);
        }

        return fallbackHtml;
    }

    private async Task<string> GetFormattedSenderAsync()
    {
        var settingsResponse = await _mediator.Send(new WebsiteSettingsQuery());
        var gc = settingsResponse.GlobalContent;

        string address = gc.TravelPaySenderEmail?.Trim() ?? string.Empty;
        string displayName = gc.TravelPaySenderDisplayName?.Trim() ?? string.Empty;

        return !string.IsNullOrWhiteSpace(displayName)
            ? $"\"{displayName}\" <{address}>"
            : address;
    }

    private async Task SendConfirmationEmailAsync(PaymentInfo payment)
    {
        if (string.IsNullOrWhiteSpace(payment.PaymentEmail))
        {
            return;
        }

        try
        {
            var values = new Dictionary<string, string?>
            {
                ["Firstname"] = payment.PaymentFirstName,
                ["Lastname"] = payment.PaymentLastName,
                ["Email"] = payment.PaymentEmail,
                ["ContactNumber"] = payment.PaymentContactNumber,
                ["InvoiceReference"] = payment.PaymentInvoiceReference,
                ["AmountPaid"] = $"${payment.PaymentAmountPaid:F2} AUD",
                ["PaymentReference"] = payment.PaymentReference,
                ["MerchantUniqueId"] = payment.PaymentMerchantUniquePaymentId,
                ["PaymentDate"] = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
            };

            string fallbackHtml = $@"<p>Dear {payment.PaymentFirstName} {payment.PaymentLastName},</p><p>Thank you for your payment of ${payment.PaymentAmountPaid:F2} AUD for invoice {payment.PaymentInvoiceReference}. Reference: {payment.PaymentReference}.</p>";

            string body = await ResolveEmailBodyAsync("PaymentCustomerConfirmation", values, fallbackHtml);

            var email = new EmailMessage
            {
                From = await GetFormattedSenderAsync(),
                Recipients = payment.PaymentEmail.Trim(),
                Subject = $"Payment Confirmation - Invoice {payment.PaymentInvoiceReference}",
                EmailFormat = EmailFormatEnum.Html,
                Body = body
            };

            await _emailService.SendEmail(email);
            _logger.LogInformation("Customer confirmation email sent for Payment {PaymentID} to {Email}", payment.PaymentID, payment.PaymentEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send customer confirmation email for Payment {PaymentID}", payment.PaymentID);
        }
    }

    private async Task SendNotificationEmailAsync(PaymentInfo payment, string? cardMasked, string? cardType)
    {
        try
        {
            var settingsResponse = await _mediator.Send(new WebsiteSettingsQuery());
            string recipient = settingsResponse.GlobalContent.TravelPayMerchantEmail?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(recipient))
            {
                return;
            }

            var values = new Dictionary<string, string?>
            {
                ["Firstname"] = payment.PaymentFirstName,
                ["Lastname"] = payment.PaymentLastName,
                ["Email"] = payment.PaymentEmail,
                ["ContactNumber"] = payment.PaymentContactNumber,
                ["InvoiceReference"] = payment.PaymentInvoiceReference,
                ["AmountPaid"] = $"${payment.PaymentAmountPaid:F2} AUD",
                ["PaymentReference"] = payment.PaymentReference,
                ["MerchantUniqueId"] = payment.PaymentMerchantUniquePaymentId,
                ["CardDetails"] = $"{cardType} {cardMasked}".Trim(),
                ["PaymentDate"] = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
            };

            string fallbackHtml = $@"<p>New payment received for invoice {payment.PaymentInvoiceReference}. Amount: ${payment.PaymentAmountPaid:F2} AUD by {payment.PaymentFirstName} {payment.PaymentLastName} ({payment.PaymentEmail}).</p>";

            string body = await ResolveEmailBodyAsync("PaymentNotification", values, fallbackHtml);

            var email = new EmailMessage
            {
                From = await GetFormattedSenderAsync(),
                Recipients = recipient,
                Subject = $"[Payment Received] Invoice {payment.PaymentInvoiceReference} - {payment.PaymentFirstName} {payment.PaymentLastName}",
                EmailFormat = EmailFormatEnum.Html,
                Body = body
            };

            await _emailService.SendEmail(email);
            _logger.LogInformation("Merchant notification email sent for Payment {PaymentID} to {Recipient}", payment.PaymentID, recipient);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send merchant notification email for Payment {PaymentID}", payment.PaymentID);
        }
    }

    [HttpGet("/payment/success")]
    [HttpGet("/payments/success")]
    public async Task<IActionResult> Success([FromQuery] int pid)
    {
        var model = await BuildPageViewModelAsync();

        if (pid > 0)
        {
            model.Payment = GetPaymentById(pid);
        }
        else
        {
            // Sample data so the page can be previewed directly in the browser
            model.Payment = new PaymentInfo
            {
                PaymentID = 1001,
                PaymentMerchantUniquePaymentId = "P1001",
                PaymentInvoiceReference = "ETG123456",
                PaymentReference = "TP987654321",
                PaymentAmount = 150.00,
                PaymentAmountPaid = 150.00,
                PaymentFirstName = "Sample",
                PaymentLastName = "Customer",
                PaymentEmail = "customer@example.com",
                PaymentLastModified = DateTime.Now
            };
        }

        if (model.Payment != null && !string.IsNullOrWhiteSpace(model.ThankYouCopy))
        {
            string displayRef = !string.IsNullOrWhiteSpace(model.Payment.PaymentReference)
                ? model.Payment.PaymentReference
                : model.Payment.PaymentMerchantUniquePaymentId;

            model.ThankYouCopy = model.ThankYouCopy
                .Replace("{%referenceno%}", displayRef, StringComparison.OrdinalIgnoreCase)
                .Replace("{%invoicereferenceno%}", model.Payment.PaymentInvoiceReference, StringComparison.OrdinalIgnoreCase);
        }

        return View("~/Features/Payment/PaymentSuccess.cshtml", model);
    }

    [HttpGet("/payment/failed")]
    [HttpGet("/payments/failed")]
    public async Task<IActionResult> Failed([FromQuery] int pid)
    {
        var model = await BuildPageViewModelAsync();
        if (pid > 0)
        {
            model.Payment = GetPaymentById(pid);
        }
        else
        {
            // Sample data for preview
            model.Payment = new PaymentInfo
            {
                PaymentInvoiceReference = "ETG123456"
            };
        }

        return View("~/Features/Payment/PaymentFailed.cshtml", model);
    }

    private async Task<PaymentPageViewModel> BuildPageViewModelAsync()
    {
        var (page, faqs) = await LoadFallbackPaymentDataAsync();
        var settingsResponse = await _mediator.Send(new WebsiteSettingsQuery());
        return PaymentPageViewModel.Create(page, faqs, settingsResponse.GlobalContent);
    }

    private async Task<(PaymentPage? Page, IEnumerable<FAQ> FAQs)> LoadFallbackPaymentDataAsync()
    {
        PaymentPage? page = null;
        var faqs = new List<FAQ>();

        string channelName = !string.IsNullOrWhiteSpace(_channelContext.WebsiteChannelName)
            ? _channelContext.WebsiteChannelName
            : "ETG";

        try
        {
            var query = new ContentItemQueryBuilder()
                .ForContentType(
                    PaymentPage.CONTENT_TYPE_NAME,
                    config => config
                        .ForWebsite(channelName)
                        .WithLinkedItems(2));

            var result = await _queryExecutor.GetMappedWebPageResult<PaymentPage>(
                query,
                cancellationToken: HttpContext.RequestAborted);

            page = result?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load PaymentPage content from CMS for fallback.");
        }

        try
        {
            var faqFolderQuery = new ContentItemQueryBuilder()
                .ForContentType(
                    Folder.CONTENT_TYPE_NAME,
                    config => config
                        .ForWebsite(
                            channelName,
                            pathMatch: PathMatch.Single("/Shared/Payment-FAQs"))
                        .WithLinkedItems(2));

            var folderResult = await _queryExecutor.GetMappedWebPageResult<Folder>(
                faqFolderQuery,
                cancellationToken: HttpContext.RequestAborted);

            var faqFolder = folderResult?.FirstOrDefault();
            if (faqFolder?.SelectedFAQs != null)
            {
                faqs.AddRange(faqFolder.SelectedFAQs.Where(f => f != null));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load Payment FAQs from CMS for fallback.");
        }

        return (page, faqs);
    }

    private PaymentInfo? GetPaymentById(int paymentId)
    {
        if (paymentId <= 0)
        {
            return null;
        }

        return _paymentProvider.Get()
            .WhereEquals(nameof(PaymentInfo.PaymentID), paymentId)
            .TopN(1)
            .GetEnumerableTypedResult()
            .FirstOrDefault();
    }

    public class PaymentStageLogRequest
    {
        public int PaymentId { get; set; }
        public string? Mupid { get; set; }
        public string? InvoiceReference { get; set; }
        public double Amount { get; set; }
        public string? Stage { get; set; }
        public string? PaymentMethod { get; set; }
        public string? FailureCode { get; set; }
        public string? FailureReason { get; set; }
        public string? AdditionalDetails { get; set; }
    }

    private sealed class CaptchaRequestAdapter : ICaptchaClientResponse
    {
        public string CaptchaToken { get; set; }

        public CaptchaRequestAdapter(string token)
        {
            CaptchaToken = token;
        }
    }
}
