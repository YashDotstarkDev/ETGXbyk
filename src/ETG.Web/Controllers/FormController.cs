using CMS.ContentEngine;
using CMS.FormEngine;
using CMS.Helpers;
using CMS.OnlineForms;
using CMS.Websites;
using ETG.Core;
using ETG.Core.Content;
using ETG.Core.Content.Forms;
using ETG.Core.Models.Tour;
using ETG.Web.Infrastructure;
using ETG.Core.Models;
using ETG.Web.Components.ViewComponents.SubscribeNow;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace ETG.Web.Controllers;

[Route("form")]
public class FormController : Controller
{
    private readonly IContentQueryExecutor _executor;
    private readonly IContentQueryExecutionOptionsCreator _queryOptionsCreator;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ReCaptchaSettings _captchaOptions;
    private readonly ILogger<FormController> logger;

    public FormController(
        IContentQueryExecutor executor,
        IContentQueryExecutionOptionsCreator queryOptionsCreator,
        IHttpContextAccessor httpContextAccessor,
        ILogger<FormController> logger,
        IOptions<ReCaptchaSettings> captchaOptions)
    {
        _executor = executor;
        _queryOptionsCreator = queryOptionsCreator;
        _httpContextAccessor = httpContextAccessor;
        _captchaOptions = captchaOptions.Value;
        this.logger = logger;
    }

    #region[Competition/Subscribe now VC]
    [HttpPost("subscribe-now")]
    [ValidateAntiForgeryToken]
    public IActionResult SubscribeNow([FromForm] SubscribeNowViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var error = ModelState[nameof(model.EmailAddress)]?.Errors
                            .FirstOrDefault()?.ErrorMessage
                        ?? ModelState[nameof(model.IsTravelAgent)]?.Errors
                            .FirstOrDefault()?.ErrorMessage
                        ?? "Please check your details.";

            return BadRequest(new
            {
                success = false,
                message = error
            });
        }

        var agentSelection = model.IsTravelAgent?.Trim();

        if (agentSelection != "Yes" && agentSelection != "No")
        {
            return BadRequest(new
            {
                success = false,
                message = "Please check your details."
            });
        }

        try
        {
            var item = new CompetitionItem
            {
                EmailAddress = model.EmailAddress.Trim(),
                FirstName = string.Empty,
                LastName = string.Empty
            };

            item.SetValue(
                nameof(CompetitionItem.IsTravelAgent),
                agentSelection == "Yes");

            BizFormItemProvider.SetItem(item);

            if (item.ItemID <= 0)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Subscription could not be saved."
                });
            }

            return Ok(new
            {
                success = true,
                message = "Thank you for subscribing to Entire Travel Group.\n\n" +
                  "We have just sent an email to the address you provided. If you do not receive it, check your junk email folder, and if the email arrives there please make sure you mark our email as a safe sender.\n\n" +
                  "The Team at Entire Travel Group"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Subscribe Now submission failed");

            return StatusCode(500, new
            {
                success = false,
                message = "Subscription could not be saved. Please try again."
            });
        }
    }

    #endregion

    #region Tour Enquiry

    [HttpGet("~/enquire/{tourCode}")]
    public async Task<IActionResult> Enquire(string tourCode, string? date = null, string? flightclass = null, string? city = null)
    {
        if (string.IsNullOrWhiteSpace(tourCode))
        {
            return NotFound();
        }

        var query = new ContentItemQueryBuilder()
            .ForContentType(
                Tour.CONTENT_TYPE_NAME,
                config => config
                    .ForWebsite("ETG")
                    .Where(where =>
                        where.WhereEquals(
                            nameof(Tour.TourCode),
                            tourCode)));

        var tours = await _executor.GetMappedWebPageResult<Tour>(query, _queryOptionsCreator.Create());

        var tour = tours.FirstOrDefault();
        if (tour == null)
        {
            return NotFound();
        }

        var viewModel = new TourEnquireViewModel
        {
            Tour = tour,
            Form = new TourEnquireFormViewModel
            {
                TourCode = tour.TourCode,
                TourName = tour.TourName,
                TourDepartureCity = !string.IsNullOrWhiteSpace(city)
                                     ? city
                                     : tour.TourDepartureCity,
                TourClass = flightclass ?? string.Empty,
                TourDate = date ?? string.Empty,
                DepartureDate = date ?? string.Empty,
                PriceCurrency = "AUD"
            }
        };
        return View("~/Features/TourPackage/TourEnquire.cshtml", viewModel);
    }

    [HttpPost("~/enquire/submit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(TourEnquireViewModel model)
    {
        if (model?.Form == null)
        {
            return BadRequest();
        }
        // Validate reCAPTCHA
        if (!ValidateRecaptcha(
        "tour_enquire_submit",
        out var recaptchaError))
        
        if (string.IsNullOrWhiteSpace(model.Form.FirstName))
            {
                model.Form.ErrorMessage = "Please enter your first name.";
                return await ReturnEnquireView(model);
            }

        if (string.IsNullOrWhiteSpace(model.Form.LastName))
        {
            model.Form.ErrorMessage = "Please enter your last name.";
            return await ReturnEnquireView(model);
        }

        if (string.IsNullOrWhiteSpace(model.Form.Email) && string.Equals(model.Form.PreferredContactMethod, "Email",
                StringComparison.OrdinalIgnoreCase))
        {
            model.Form.ErrorMessage = "Please enter your email address.";
            return await ReturnEnquireView(model);
        }

        if (string.IsNullOrWhiteSpace(model.Form.Phone) &&
            string.Equals(model.Form.PreferredContactMethod, "Phone", StringComparison.OrdinalIgnoreCase))
        {
            model.Form.ErrorMessage = "Please enter your phone number.";
            return await ReturnEnquireView(model);
        }

        if (string.Equals(model.Form.FirstName.Trim(), model.Form.LastName.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            model.Form.ErrorMessage = "First and last name cannot be the same.";
            return await ReturnEnquireView(model);
        }

        if (model.Form.FirstName.Contains("http", StringComparison.OrdinalIgnoreCase) || model.Form.LastName.Contains("http", StringComparison.OrdinalIgnoreCase))
        {
            model.Form.ErrorMessage = "Please enter valid values.";
            return await ReturnEnquireView(model);
        }

        var tour = await GetTour(model.Form.TourCode);
        if (tour == null)
        {
            return NotFound();
        }

        var enquireItem = new EnquireItem
        {
            FirstName = model.Form.FirstName.Trim(),
            LastName = model.Form.LastName.Trim(),
            Email = model.Form.Email?.Trim() ?? string.Empty,
            Phone = model.Form.Phone?.Trim() ?? string.Empty,
            PreferredContactMethod = model.Form.PreferredContactMethod,
            TourCode = tour.TourCode,
            TourName = tour.TourName,
            TourClass = model.Form.TourClass,
            TourDepartureCity = model.Form.TourDepartureCity,
            TourQuotedPrice = ParseQuotedPrice(model.Form.TourQuotedPrice),
            PriceCurrency = string.IsNullOrWhiteSpace(model.Form.PriceCurrency)
                            ? "AUD"
                            : model.Form.PriceCurrency,

            TourDiscountText = model.Form.TourDiscountText,
            TourDate = model.Form.TourDate,
            DepartureDate = model.Form.DepartureDate,
            ReturnDate = model.Form.ReturnDate,
            SubscribeToNewsletter = model.Form.SubscribeToNewsletter,
            Message = model.Form.Message
        };
        var tourUrl = tour.GetUrl();
        enquireItem.TourLink = tourUrl.AbsoluteUrl;
        BizFormItemProvider.SetItem(enquireItem);
        return Redirect("/enquire/thank-you");
    }

    private async Task<Tour?> GetTour(string tourCode)
    {
        if (string.IsNullOrWhiteSpace(tourCode))
        {
            return null;
        }

        var query = new ContentItemQueryBuilder()
            .ForContentType(
                Tour.CONTENT_TYPE_NAME,
                config => config
                    .ForWebsite("ETG")
                    .Where(where =>
                        where.WhereEquals(
                            nameof(Tour.TourCode),
                            tourCode)));

        var tours = await _executor.GetMappedWebPageResult<Tour>(query, _queryOptionsCreator.Create());
        return tours.FirstOrDefault();
    }

    private async Task<IActionResult> ReturnEnquireView(TourEnquireViewModel model)
    {
        var tour = await GetTour(model.Form.TourCode);
        if (tour == null)
        {
            return NotFound();
        }
        model.Tour = tour;
        return View("~/Features/TourPackage/TourEnquire.cshtml", model);
    }
    private static int ParseQuotedPrice(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        return int.TryParse(value, out var price) ? price : 0;
    }
    #endregion

    #region Main Enquiry

    [HttpPost("~/api/enquire/submit")]
    [ValidateAntiForgeryToken]
    public IActionResult GenericEnquire(EnquireFormPageViewModel model)
    {
        if (model?.Form == null)
        {
            return Json(new
            {
                success = false,
                message = "Invalid form submission."
            });
        }

        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(model.Form.FirstName))
        {
            errors["firstName"] = "Please enter a valid first name";
        }

        if (string.IsNullOrWhiteSpace(model.Form.LastName))
        {
            errors["lastName"] = "Please enter a valid last name";
        }

        if (string.IsNullOrWhiteSpace(model.Form.Email))
        {
            errors["email"] = "Email must be a valid e-mail";
        }
        else if (!new EmailAddressAttribute().IsValid(model.Form.Email))
        {
            errors["email"] = "Email must be a valid e-mail";
        }

        if (!string.IsNullOrWhiteSpace(model.Form.FirstName) &&
            model.Form.FirstName.Any(char.IsDigit))
        {
            errors["firstName"] =
                "First name cannot contain numbers.";
        }

        if (!string.IsNullOrWhiteSpace(model.Form.LastName) &&
            model.Form.LastName.Any(char.IsDigit))
        {
            errors["lastName"] =
                "Last name cannot contain numbers.";
        }

        if (errors.Count > 0)
        {
            return Json(new
            {
                success = false,
                errors
            });
        }

        // reCAPTCHA
        if (!ValidateRecaptcha("generic_enquire_submit", out var recaptchaError))
        {
            return Json(new
            {
                success = false,
                message = recaptchaError
            });
        }

        if (string.Equals(model.Form.FirstName.Trim(), model.Form.LastName.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return Json(new
            {
                success = false,
                errors = new Dictionary<string, string>
                {
                    ["firstName"] = "First and last name cannot be the same.",
                    ["lastName"] = "First and last name cannot be the same."
                }
            });
        }

        if (model.Form.FirstName.Contains(
                "http",
                StringComparison.OrdinalIgnoreCase) ||
            model.Form.LastName.Contains(
                "http",
                StringComparison.OrdinalIgnoreCase))
        {
            return Json(new
            {
                success = false,
                message = "Please enter valid values."
            });
        }

        if (string.IsNullOrWhiteSpace(model.Form.Email) &&
            string.Equals(
                model.Form.PreferredContactType,
                "Email",
                StringComparison.OrdinalIgnoreCase))
        {
            return Json(new
            {
                success = false,
                message = "Please enter your email address."
            });
        }

        var enquireItem = new EnquireItem
        {
            FirstName = model.Form.FirstName.Trim(),
            LastName = model.Form.LastName.Trim(),

            Email = model.Form.Email?.Trim() ?? string.Empty,
            Phone = model.Form.Phone?.Trim() ?? string.Empty,

            PreferredContactMethod =
                model.Form.PreferredContactType,

            PreferredTourType =
                model.Form.PreferredTourType,

            PreferredDestination =
                model.Form.PreferredDestination,

            PreferredExperience =
                model.Form.PreferredExperience,

            Message =
                model.Form.Message?.Trim() ?? string.Empty,

            SubscribeToNewsletter =
                model.Form.SubscribeToNewsletter
        };

        BizFormItemProvider.SetItem(enquireItem);

        return Json(new
        {
            success = true,
            redirectUrl = "/enquire/thank-you"
        });
    }
    #endregion


    #region Contact Us Enquiry

    [HttpPost("~/api/contact-us/submit")]
    [ValidateAntiForgeryToken]
    public IActionResult ContactEnquire(ContactUsPageViewModel model)
    {
        if (model?.Form == null)
        {
            logger.LogWarning("Contact Us enquiry submission received with an invalid model.");

            return BadRequest(new
            {
                success = false,
                message = "Invalid form submission."
            });
        }

        var form = model.Form;

        try
        {
            if (!ValidateRecaptcha(
                    "contact_us_submit",
                    out var recaptchaError))
            {
                logger.LogWarning(
                    "Contact Us enquiry failed reCAPTCHA validation.");

                return Json(new
                {
                    success = false,
                    message = recaptchaError
                });
            }

            var errors = new Dictionary<string, string>();

            if (string.IsNullOrWhiteSpace(model.Form.FirstName))
            {
                errors["firstName"] = "Please enter a valid first name";
            }

            if (string.IsNullOrWhiteSpace(model.Form.LastName))
            {
                errors["lastName"] = "Please enter a valid last name";
            }

            if (string.IsNullOrWhiteSpace(model.Form.Email))
            {
                errors["email"] = "Email must be a valid e-mail";
            }
            else if (!new EmailAddressAttribute().IsValid(model.Form.Email))
            {
                errors["email"] = "Email must be a valid e-mail";
            }

            if (!string.IsNullOrWhiteSpace(model.Form.FirstName) &&
                model.Form.FirstName.Any(char.IsDigit))
            {
                errors["firstName"] =
                    "First name cannot contain numbers.";
            }

            if (!string.IsNullOrWhiteSpace(model.Form.LastName) &&
                model.Form.LastName.Any(char.IsDigit))
            {
                errors["lastName"] =
                    "Last name cannot contain numbers.";
            }

            if (form.FirstName.Contains(
                    "http",
                    StringComparison.OrdinalIgnoreCase) ||
                form.LastName.Contains(
                    "http",
                    StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning(
                    "Potential spam detected in Contact Us enquiry.");

                return Json(new
                {
                    success = false,
                    message = "Please enter valid values."
                });
            }

            if (!string.IsNullOrWhiteSpace(form.Friend))
            {
                logger.LogWarning(
                    "Honeypot validation failed for Contact Us enquiry.");

                return Json(new
                {
                    success = false,
                    message = "Please enter valid values."
                });
            }

            var enquireItem = new EnquireItem
            {
                FirstName =
                    form.FirstName.Trim(),

                LastName =
                    form.LastName.Trim(),

                Email =
                    form.Email?.Trim() ?? string.Empty,

                Phone =
                    form.Phone?.Trim() ?? string.Empty,

                PreferredContactMethod =
                    form.PreferredContactType,

                Message =
                    form.Message?.Trim() ?? string.Empty,

                SubscribeToNewsletter =
                    form.SubscribeToNewsletter,

                StoreView = "Contact Us",

                Status = "New"
            };

            BizFormItemProvider.SetItem(enquireItem);

            if (enquireItem.ItemID <= 0)
            {
                logger.LogError(
                    "Contact Us enquiry could not be saved to the Enquire BizForm.");

                return StatusCode(500, new
                {
                    success = false,
                    message = "Your enquiry could not be saved. Please try again."
                });
            }

            logger.LogInformation(
                "Contact Us enquiry saved successfully. Enquire Item ID: {ItemID}",
                enquireItem.ItemID);

            return Json(new
            {
                success = true,
                redirectUrl = "/contact-us/thank-you"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error while processing Contact Us enquiry.");

            return StatusCode(500, new
            {
                success = false,
                message = "Something went wrong while submitting your enquiry. Please try again."
            });
        }
    }

    #endregion


    private bool ValidateRecaptcha(
        string expectedAction,
        out string errorMessage)
    {
        errorMessage = string.Empty;

        if (_captchaOptions.IsValidationDisabled)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(_captchaOptions.SiteKey) ||
            string.IsNullOrWhiteSpace(_captchaOptions.SecretKey))
        {
            errorMessage = "reCAPTCHA is not configured.";
            return false;
        }

        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext == null)
        {
            errorMessage = "Unable to validate reCAPTCHA.";
            return false;
        }

        var responseToken =
            httpContext.Request.Form["g-recaptcha-response"]
                .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(responseToken))
        {
            errorMessage =
                "Please complete the reCAPTCHA verification.";

            return false;
        }

        var action =
            httpContext.Request.Form["g-recaptcha-action"]
                .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(action))
        {
            errorMessage = "Invalid reCAPTCHA action.";
            return false;
        }

        if (!string.Equals(
                action,
                expectedAction,
                StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = "Invalid reCAPTCHA action.";
            return false;
        }

        var validator = new RecaptchaValidator
        {
            PrivateKey = _captchaOptions.SecretKey,
            RemoteIP = RequestContext.UserHostAddress,
            Response = responseToken
        };

        var validationResult = validator.Validate().Result;

        if (validationResult == null)
        {
            errorMessage = "Unable to validate reCAPTCHA.";
            return false;
        }

        if (!string.IsNullOrEmpty(validationResult.ErrorMessage))
        {
            errorMessage = "reCAPTCHA validation failed.";
            return false;
        }

        if (validationResult.Score < _captchaOptions.ScoreThredhold)
        {
            errorMessage = "reCAPTCHA verification failed.";
            return false;
        }

        if (!string.Equals(
                validationResult.Action,
                expectedAction,
                StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = "Invalid reCAPTCHA action.";
            return false;
        }

        return true;
    }
}
