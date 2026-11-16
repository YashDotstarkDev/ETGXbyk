using CMS.DataEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using Microsoft.Extensions.Configuration;

namespace ETG.Web.Infrastructure;

/// <summary>
/// Defines reCAPTCHA settings resolution operations.
/// </summary>
public interface IRecaptchaSettingsProvider
{
    string GetSiteKey();
    string GetSecretKey();
    double GetScoreThreshold();
    bool IsValidationDisabled();
    bool AreKeysConfigured();
}

/// <summary>
/// POCO representing reCAPTCHA configuration settings.
/// Can be bound to IConfiguration or populated from Channel settings.
/// </summary>
public class ReCaptchaSettings
{
    private string _siteKey = string.Empty;
    private string _secretKey = string.Empty;
    private double _scoreThreshold = 0.5;

    /// <summary>
    /// Used to disable server-side captcha validation in specific scenarios (e.g. CI/testing).
    /// </summary>
    public bool IsValidationDisabled { get; set; }

    /// <summary>
    /// Google reCAPTCHA v3 Site Key.
    /// </summary>
    public string SiteKey
    {
        get => !string.IsNullOrWhiteSpace(_siteKey) ? _siteKey : string.Empty;
        set => _siteKey = value;
    }

    /// <summary>
    /// Google reCAPTCHA v3 Secret Key.
    /// </summary>
    public string SecretKey
    {
        get => !string.IsNullOrWhiteSpace(_secretKey) ? _secretKey : string.Empty;
        set => _secretKey = value;
    }

    /// <summary>
    /// reCAPTCHA v3 score threshold (defaults to 0.5).
    /// </summary>
    public double ScoreThreshold
    {
        get => _scoreThreshold > 0 ? _scoreThreshold : 0.5;
        set => _scoreThreshold = value;
    }
}

