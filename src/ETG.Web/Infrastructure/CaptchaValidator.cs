using System.Globalization;
using CMS.DataEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace ETG.Web.Infrastructure;

public class CaptchaValidator : IRecaptchaSettingsProvider
{
    private readonly IWebsiteChannelContext? _websiteChannelContext;
    private readonly IInfoProvider<WebsiteCaptchaSettingsInfo>? _captchaSettingsProvider;
    private readonly IOptions<ReCaptchaSettings>? _options;
    private readonly IConfiguration? _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CaptchaValidator>? _logger;

    public CaptchaValidator(
        IHttpClientFactory httpClientFactory,
        IServiceProvider serviceProvider,
        IOptions<ReCaptchaSettings>? options = null,
        IConfiguration? configuration = null,
        ILogger<CaptchaValidator>? logger = null)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _configuration = configuration;
        _logger = logger;

        _websiteChannelContext = serviceProvider.GetService<IWebsiteChannelContext>();
        _captchaSettingsProvider = serviceProvider.GetService<IInfoProvider<WebsiteCaptchaSettingsInfo>>();

        SyncOptionsWithChannelSettings();
    }

    /// <summary>
    /// Synchronizes channel-configured keys back into the IOptions instance for compatibility.
    /// </summary>
    private void SyncOptionsWithChannelSettings()
    {
        try
        {
            if (_options?.Value == null) return;

            var channelSettings = GetCurrentChannelCaptchaSettings();
            if (channelSettings != null)
            {
                if (!string.IsNullOrWhiteSpace(channelSettings.WebsiteCaptchaSettingsReCaptchaSiteKey))
                {
                    _options.Value.SiteKey = channelSettings.WebsiteCaptchaSettingsReCaptchaSiteKey.Trim();
                }
                if (!string.IsNullOrWhiteSpace(channelSettings.WebsiteCaptchaSettingsReCaptchaSecretKey))
                {
                    _options.Value.SecretKey = channelSettings.WebsiteCaptchaSettingsReCaptchaSecretKey.Trim();
                }
                if (channelSettings.WebsiteCaptchaSettingsReCaptchaThreshold > 0)
                {
                    _options.Value.ScoreThreshold = channelSettings.WebsiteCaptchaSettingsReCaptchaThreshold;
                }
            }
        }
        catch
        {
            // Ignore during startup or background contexts
        }
    }

    /// <summary>
    /// Resolves the CAPTCHA settings configured for the current Website Channel in Kentico.
    /// </summary>
    public WebsiteCaptchaSettingsInfo? GetCurrentChannelCaptchaSettings()
    {
        try
        {
            if (_websiteChannelContext != null && _captchaSettingsProvider != null)
            {
                var channelId = _websiteChannelContext.WebsiteChannelID;
                if (channelId > 0)
                {
                    return _captchaSettingsProvider.Get()
                        .WhereEquals(nameof(WebsiteCaptchaSettingsInfo.WebsiteCaptchaSettingsWebsiteChannelID), channelId)
                        .FirstOrDefault();
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Could not retrieve channel CAPTCHA settings from website channel context.");
        }

        return null;
    }

    /// <summary>
    /// Resolves Site Key from Channel Settings first, falling back to appsettings.
    /// </summary>
    public string GetSiteKey()
    {
        // 1. Channel Settings (Admin UI)
        var channelSettings = GetCurrentChannelCaptchaSettings();
        if (!string.IsNullOrWhiteSpace(channelSettings?.WebsiteCaptchaSettingsReCaptchaSiteKey))
        {
            return channelSettings.WebsiteCaptchaSettingsReCaptchaSiteKey.Trim();
        }

        // 2. Fallback to appsettings / IConfiguration
        var configKey = _configuration?["ReCaptcha:SiteKey"]
                     ?? _configuration?["RecaptchaV3SiteKey"]
                     ?? _options?.Value.SiteKey;

        return configKey?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Resolves Secret Key from Channel Settings first, falling back to appsettings.
    /// </summary>
    public string GetSecretKey()
    {
        // 1. Channel Settings (Admin UI)
        var channelSettings = GetCurrentChannelCaptchaSettings();
        if (!string.IsNullOrWhiteSpace(channelSettings?.WebsiteCaptchaSettingsReCaptchaSecretKey))
        {
            return channelSettings.WebsiteCaptchaSettingsReCaptchaSecretKey.Trim();
        }

        // 2. Fallback to appsettings / IConfiguration
        var configSecret = _configuration?["ReCaptcha:SecretKey"]
                        ?? _configuration?["RecaptchaV3SecretKey"]
                        ?? _options?.Value.SecretKey;

        return configSecret?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Resolves Score Threshold from Channel Settings first, falling back to appsettings, defaulting to 0.5.
    /// </summary>
    public double GetScoreThreshold()
    {
        // 1. Channel Settings (Admin UI)
        var channelSettings = GetCurrentChannelCaptchaSettings();
        if (channelSettings != null && channelSettings.WebsiteCaptchaSettingsReCaptchaThreshold > 0)
        {
            return channelSettings.WebsiteCaptchaSettingsReCaptchaThreshold;
        }

        // 2. Fallback to appsettings / IConfiguration
        var configThreshold = _configuration?["ReCaptcha:ScoreThreshold"]
                           ?? _configuration?["CMSRecaptchaV3Threshold"];
        if (!string.IsNullOrWhiteSpace(configThreshold) &&
            double.TryParse(configThreshold, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedThreshold) &&
            parsedThreshold > 0)
        {
            return parsedThreshold;
        }

        if (_options?.Value.ScoreThreshold > 0)
        {
            return _options.Value.ScoreThreshold;
        }

        // 3. System Default (matches Kentico admin default: 0.5)
        return 0.5;
    }

    /// <summary>
    /// Determines whether captcha validation is disabled.
    /// </summary>
    public bool IsValidationDisabled()
    {
        if (_options?.Value.IsValidationDisabled == true)
        {
            return true;
        }

        var configVal = _configuration?["ReCaptcha:IsValidationDisabled"]
                     ?? _configuration?["RecaptchaSkipValidation"];
        if (bool.TryParse(configVal, out var disabled))
        {
            return disabled;
        }

        return false;
    }

    /// <summary>
    /// Indicates whether both Site Key and Secret Key are configured.
    /// </summary>
    public bool AreKeysConfigured()
    {
        return !string.IsNullOrWhiteSpace(GetSiteKey()) && !string.IsNullOrWhiteSpace(GetSecretKey());
    }

    public const string HTTP_CLIENT_NAME = "ReCaptchaClient";
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    public string SiteKey => GetSiteKey();
    public string SecretKey => GetSecretKey();
    public double ScoreThreshold => GetScoreThreshold();

    public async Task<CaptchaValidationResult> ValidateCaptcha(ICaptchaClientResponse clientResponse, CancellationToken cancellationToken = default)
    {
        return await ValidateCaptcha(clientResponse?.CaptchaToken, cancellationToken: cancellationToken);
    }

    public async Task<CaptchaValidationResult> ValidateCaptcha(
        string? token,
        string? expectedAction = null,
        CancellationToken cancellationToken = default)
    {
        if (IsValidationDisabled())
        {
            return new() { IsSuccess = true };
        }

        var secret = GetSecretKey();
        if (string.IsNullOrWhiteSpace(secret))
        {
            return new() { IsSuccess = false, ErrorMessage = "reCAPTCHA is not configured." };
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return new() { IsSuccess = false, ErrorMessage = "Please complete the reCAPTCHA verification." };
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(DefaultTimeout);

            var client = _httpClientFactory.CreateClient(HTTP_CLIENT_NAME);
            var postData = new Dictionary<string, string>
            {
                ["secret"] = secret,
                ["response"] = token
            };

            var responseMessage = await client.PostAsync(
                "https://www.google.com/recaptcha/api/siteverify",
                new FormUrlEncodedContent(postData),
                cts.Token);

            _ = responseMessage.EnsureSuccessStatusCode();

            string data = await responseMessage.Content.ReadAsStringAsync(cts.Token);
            var response = JsonConvert.DeserializeObject<CaptchaResponse>(data);

            if (response is null)
            {
                return new() { IsSuccess = false, ErrorMessage = "Could not validate captcha." };
            }

            if (!response.IsSuccess)
            {
                return new()
                {
                    IsSuccess = false,
                    ErrorMessage = response.ErrorMessages.FirstOrDefault() ?? "reCAPTCHA verification failed.",
                    Score = response.Score,
                    Action = response.Action
                };
            }

            if (!string.IsNullOrWhiteSpace(expectedAction) &&
                !string.Equals(response.Action, expectedAction, StringComparison.OrdinalIgnoreCase))
            {
                return new()
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid reCAPTCHA action.",
                    Score = response.Score,
                    Action = response.Action
                };
            }

            var threshold = GetScoreThreshold();
            if (response.Score < threshold)
            {
                return new()
                {
                    IsSuccess = false,
                    ErrorMessage = "reCAPTCHA score verification failed.",
                    Score = response.Score,
                    Action = response.Action
                };
            }

            return new()
            {
                IsSuccess = true,
                Score = response.Score,
                Action = response.Action
            };
        }
        catch (OperationCanceledException oce) when (!cancellationToken.IsCancellationRequested)
        {
            _logger?.LogWarning(oce, "reCAPTCHA verification request to Google timed out after {Timeout}s.", DefaultTimeout.TotalSeconds);
            return new() { IsSuccess = false, ErrorMessage = "reCAPTCHA verification timed out. Please try again." };
        }
        catch (OperationCanceledException oce)
        {
            _logger?.LogWarning(oce, "reCAPTCHA verification was cancelled by the caller.");
            return new() { IsSuccess = false, ErrorMessage = "reCAPTCHA verification was cancelled." };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unable to verify reCAPTCHA with Google.");
            return new() { IsSuccess = false, ErrorMessage = "Unable to verify reCAPTCHA with Google." };
        }
    }

    /// <summary>
    /// Validates reCAPTCHA token synchronously with a bounded timeout, safely bridging sync-to-async
    /// for components where only synchronous validation is supported by the framework (e.g. FormComponent).
    /// Prevents worker thread deadlocks and enforces fail-closed behavior on timeout or failure.
    /// </summary>
    public CaptchaValidationResult ValidateCaptchaSync(
        string? token,
        string? expectedAction = null,
        CancellationToken cancellationToken = default,
        TimeSpan? timeout = null)
    {
        var boundedTimeout = timeout ?? TimeSpan.FromSeconds(5);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(boundedTimeout);

        try
        {
            return Task.Run(() => ValidateCaptcha(token, expectedAction, cts.Token), cts.Token).GetAwaiter().GetResult();
        }
        catch (OperationCanceledException oce)
        {
            _logger?.LogWarning(oce, "Synchronous reCAPTCHA validation timed out or was cancelled.");
            return new() { IsSuccess = false, ErrorMessage = "reCAPTCHA verification timed out. Please try again." };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Synchronous reCAPTCHA validation failed with unexpected exception.");
            return new() { IsSuccess = false, ErrorMessage = "Unable to verify reCAPTCHA with Google." };
        }
    }

    public class CaptchaResponse
    {
        [JsonProperty("success")]
        public bool IsSuccess { get; set; }

        [JsonProperty("error-codes")]
        public List<string> ErrorMessages { get; set; } = [];

        [JsonProperty("score")]
        public double Score { get; set; }

        [JsonProperty("action")]
        public string? Action { get; set; }
    }
}

public interface ICaptchaClientResponse
{
    public string CaptchaToken { get; set; }
}

public class CaptchaValidationResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public double Score { get; set; }
    public string? Action { get; set; }
}