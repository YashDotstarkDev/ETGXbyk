using System.ComponentModel.DataAnnotations;
using System.Globalization;
using CMS.Core;
using CMS.FormEngine;
using CMS.Helpers;
using CMS.Helpers.Internal;
using ETG.Web.Components.FormBuilder.ReCaptcha;
using ETG.Web.Infrastructure;
using Kentico.Forms.Web.Mvc;
using Microsoft.Extensions.Options;
using XperienceCommunity.KenticoComponentIcons;

[assembly: RegisterFormComponent(
    identifier: ReCaptchaFormComponent.IDENTIFIER,
    formComponentType: typeof(ReCaptchaFormComponent),
    name: "Website ReCaptcha",
    Description = "{$kentico.formbuilder.component.recaptcha.description$}",
    IconClass = KenticoIcons.RECAPTCHA,
    ViewName = "~/Components/FormBuilder/ReCaptcha/ReCaptcha.cshtml",
    IsMappableToContactFields = false)]

namespace ETG.Web.Components.FormBuilder.ReCaptcha;

/// <summary>
/// Represents a reCAPTCHA form component for Kentico Form Builder.
/// </summary>
public class ReCaptchaFormComponent(
    IHttpContextAccessor httpContextAccessor,
    ILocalizationService localizationService,
    CaptchaValidator validator,
    IOptions<ReCaptchaSettings> captchaOptions) : FormComponent<ReCaptchaFormComponentProperties, string>
{
    /// <summary>
    /// Represents the <see cref="ReCaptchaFormComponent"/> identifier.
    /// </summary>
    public const string IDENTIFIER = "Website.Components.Recaptcha-FormComponent";

    private const string RESPONSE_KEY = "g-recaptcha-response";

    private static readonly Lazy<HashSet<string>> mFullLanguageCodes = new(() =>
    {
        string[] codes = ["zh-HK", "zh-CN", "zh-TW", "en-GB", "fr-CA", "de-AT", "de-CH", "pt-BR", "pt-PT"];

        return new HashSet<string>(codes, StringComparer.OrdinalIgnoreCase);
    });

    private readonly IHttpContextAccessor httpContextAccessor = httpContextAccessor;
    private readonly ILocalizationService localizationService = localizationService;
    private readonly CaptchaValidator validator = validator;
    private readonly ReCaptchaSettings captchaOptions = captchaOptions.Value;
    private bool? mSkipRecaptcha;

    /// <summary>
    /// Provides access to the injected <see cref="CaptchaValidator"/>.
    /// </summary>
    public CaptchaValidator Validator => validator;

    /// <summary>
    /// Holds nothing and is here just because it is required by FormComponent.
    /// </summary>
    public string Value
    {
        get;
        set;
    } = "";

    /// <summary>
    /// Optional. Forces the reCAPTCHA to render in a specific language.
    /// Auto-detects the user's language if unspecified.
    /// Currently supported values are listed at https://developers.google.com/recaptcha/docs/language.
    /// </summary>
    public string Language
    {
        get
        {
            if (string.IsNullOrEmpty(field))
            {
                var currentCulture = CultureInfo.CurrentCulture;

                field = mFullLanguageCodes.Value.Contains(currentCulture.Name) ? currentCulture.Name : currentCulture.TwoLetterISOLanguageName;
            }

            return field;
        }
        set;
    } = "";

    /// <summary>
    /// Determines whether the component is configured and allowed to be displayed.
    /// </summary>
    public bool IsConfigured => AreKeysConfigured && !SkipRecaptcha;

    /// <summary>
    /// Indicates whether to skip the reCAPTCHA validation.
    /// Useful for testing platform. Can be set using RecaptchaSkipValidation in AppSettings.
    /// </summary>
    private bool SkipRecaptcha
    {
        get
        {
            if (!mSkipRecaptcha.HasValue)
            {
                mSkipRecaptcha = validator.IsValidationDisabled();
            }

            return mSkipRecaptcha.Value;
        }
    }

    /// <summary>
    /// Indicates whether both required keys are configured in Channel settings or appsettings.
    /// </summary>
    public bool AreKeysConfigured => validator.AreKeysConfigured();

    /// <summary>
    /// Label "for" cannot be used for this component. 
    /// </summary>
    /// <remarks>
    /// This type MUST be string? otherwise the field is validated as invalid upon form submission
    /// when nullable reference types is enabled
    /// </remarks>
    public override string? LabelForPropertyName => null!;

    /// <inheritdoc/>
    public override void LoadProperties(FormComponentProperties properties)
    {
        base.LoadProperties(properties);

        Properties.Label = string.Empty;
    }

    /// <summary>
    /// Returns empty string since the <see cref="Value"/> does not hold anything.
    /// </summary>
    /// <returns>Returns the value of the form component.</returns>
    public override string GetValue() => string.Empty;

    /// <summary>
    /// Does nothing since the <see cref="Value"/> does not need to hold anything.
    /// </summary>
    /// <param name="value">Value to be set.</param>
    public override void SetValue(string value)
    {
        // the Value does not need to hold anything
    }

    /// <summary>
    /// Performs validation of the reCAPTCHA component.
    /// </summary>
    /// <param name="validationContext">The validation context.</param>
    /// <returns>A collection that holds failed-validation information.</returns>
    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var errors = new List<ValidationResult>();
        errors.AddRange(base.Validate(validationContext));

        bool isRenderedInAdminUI = VirtualContext.IsInitialized;

        if (!IsConfigured || isRenderedInAdminUI)
        {
            return errors;
        }

        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext is null)
        {
            return [new("Cannot validate captcha outside of an active HTTP request")];
        }

        var token = httpContext.Request.Form.TryGetValue(RESPONSE_KEY, out var value) ? value[0] : string.Empty;

        string expectedAction = httpContext.Request.Form.TryGetValue("g-recaptcha-action", out var action)
            ? action[0] ?? "form_builder_submit"
            : "form_builder_submit";

        var cancellationToken = httpContext.RequestAborted;
        var validationResult = validator.ValidateCaptchaSync(
            token,
            expectedAction,
            cancellationToken,
            TimeSpan.FromSeconds(5));

        if (!validationResult.IsSuccess)
        {
            string errorKey = validationResult.ErrorMessage switch
            {
                "reCAPTCHA score verification failed." => "recaptcha.error.scorenotmet",
                "Invalid reCAPTCHA action." => "recaptcha.error.actionmismatch",
                "Unable to verify reCAPTCHA with Google." => "recaptcha.error.serverunavailable",
                "reCAPTCHA verification timed out. Please try again." => "recaptcha.error.serverunavailable",
                _ => validationResult.ErrorMessage ?? "recaptcha.error.serverunavailable"
            };

            var localizedMessage = localizationService.GetString(errorKey);
            if (string.IsNullOrWhiteSpace(localizedMessage) || localizedMessage == errorKey)
            {
                localizedMessage = validationResult.ErrorMessage ?? "reCAPTCHA verification failed.";
            }

            errors.Add(new ValidationResult(localizedMessage));
        }

        return errors;
    }
}

public class ReCaptchaFormComponentProperties : FormComponentProperties<string>
{
    public ReCaptchaFormComponentProperties() : base("text", 1, -1)
    {
    }

    public override bool Required { get; set; }
    public override string DefaultValue { get; set; } = "";
    public override bool SmartField { get; set; }
    public override string Tooltip { get; set; } = "";
}
