#nullable enable
using System;
using System.Text;
using CMS.ContentEngine;
using ETG.Core.Content;
using ETG.Web.Configuration;
using ETG.Web.Rendering;

namespace ETG.Web;

/// <summary>
/// Provides Imgix image optimization extension methods, mirroring the Kentico 13 (K13) Imgixify developer experience.
/// Takes Kentico assets (MediaFile, ImageAssetViewModel, ContentItemAsset, or URL strings) and transforms them into optimized Imgix CDN URLs.
/// </summary>
public static class ImgixExtensions
{
    /// <summary>
    /// Imgix CDN domain. Configured via Imgix:Domain in appsettings.json (set in ServiceCollectionAppExtensions).
    /// </summary>
    public static string ImgixDomain { get; private set; } = string.Empty;

    /// <summary>
    /// Toggle to enable or disable Imgix URL rewriting (e.g. for local development).
    /// </summary>
    public static bool IsEnabled { get; private set; } = true;

    /// <summary>
    /// Initializes Imgix configuration from strongly-typed <see cref="ImgixOptions"/>.
    /// </summary>
    public static void Initialize(ImgixOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ImgixDomain = options.Domain ?? string.Empty;
        IsEnabled = options.Enabled;
    }

    /// <summary>
    /// Transforms a MediaFile (Content Hub item) into an Imgix CDN URL with dynamic resizing and optimization.
    /// </summary>
    public static string Imgixify(
        this MediaFile? mediaFile,
        int width = 0,
        int height = 0,
        string fit = "crop",
        string auto = "format,compress",
        string crop = "") =>
        mediaFile?.LegacyMediaFileAsset?.Url.Imgixify(width, height, fit, auto, crop) ?? string.Empty;

    /// <summary>
    /// K13-compatible Imgixify overload: Imgixify(width, height, doubleSize = false, cropFit = true).
    /// </summary>
    public static string Imgixify(
        this MediaFile? mediaFile,
        int width,
        int height,
        bool doubleSize,
        bool cropFit = true) =>
        mediaFile?.LegacyMediaFileAsset?.Url.Imgixify(width, height, doubleSize, cropFit) ?? string.Empty;

    /// <summary>
    /// Transforms an ImageAssetViewModel into an Imgix CDN URL with dynamic resizing and optimization.
    /// </summary>
    public static string Imgixify(
        this ImageAssetViewModel? imageVm,
        int width = 0,
        int height = 0,
        string fit = "crop",
        string auto = "format,compress",
        string crop = "") =>
        imageVm?.LegacyMediaFileAsset?.Url.Imgixify(width, height, fit, auto, crop) ?? string.Empty;

    /// <summary>
    /// K13-compatible Imgixify overload: Imgixify(width, height, doubleSize = false, cropFit = true).
    /// </summary>
    public static string Imgixify(
        this ImageAssetViewModel? imageVm,
        int width,
        int height,
        bool doubleSize,
        bool cropFit = true) =>
        imageVm?.LegacyMediaFileAsset?.Url.Imgixify(width, height, doubleSize, cropFit) ?? string.Empty;

    /// <summary>
    /// Transforms a ContentItemAsset into an Imgix CDN URL with dynamic resizing and optimization.
    /// </summary>
    public static string Imgixify(
        this ContentItemAsset? asset,
        int width = 0,
        int height = 0,
        string fit = "crop",
        string auto = "format,compress",
        string crop = "") =>
        asset?.Url.Imgixify(width, height, fit, auto, crop) ?? string.Empty;

    /// <summary>
    /// Transforms any relative or absolute image URL into an Imgix CDN URL.
    /// </summary>
    public static string Imgixify(
        this string? url,
        int width = 0,
        int height = 0,
        string fit = "crop",
        string auto = "format,compress",
        string crop = "")
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        var cleanPath = url.TrimStart('~');

        // If an absolute URL is passed (e.g. from WebPageMetaService: https://xbyk.staging.entiretravel.com.au/getContentAsset/...)
        // strip the domain so we only have the relative /getContentAsset/... path
        if (cleanPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            cleanPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            cleanPath.StartsWith("/http://", StringComparison.OrdinalIgnoreCase) ||
            cleanPath.StartsWith("/https://", StringComparison.OrdinalIgnoreCase))
        {
            var raw = cleanPath.TrimStart('/');
            if (Uri.TryCreate(raw, UriKind.Absolute, out var parsedUri))
            {
                cleanPath = parsedUri.PathAndQuery;
            }
        }

        if (!IsEnabled || string.IsNullOrWhiteSpace(ImgixDomain))
        {
            return cleanPath;
        }

        if (cleanPath.Split('?')[0].EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
        {
            return cleanPath;
        }

        return BuildImgixUrl(cleanPath, width, height, fit, auto, crop);
    }

    /// <summary>
    /// K13-compatible Imgixify overload: Imgixify(width, height, doubleSize = false, cropFit = true).
    /// </summary>
    public static string Imgixify(
        this string? url,
        int width,
        int height,
        bool doubleSize,
        bool cropFit = true) =>
        url.Imgixify(width: width * (doubleSize ? 2 : 1), height: height * (doubleSize ? 2 : 1), fit: cropFit ? "crop" : string.Empty);

    /// <summary>
    /// Appends Imgix transformation parameters and prepends the configured Imgix domain.
    /// </summary>
    private static string BuildImgixUrl(
        string path,
        int width,
        int height,
        string fit,
        string auto,
        string crop = "")
    {
        var pathOnly = path;
        var existingQuery = string.Empty;
        var queryIndex = path.IndexOf('?');
        if (queryIndex >= 0)
        {
            pathOnly = path.Substring(0, queryIndex);
            existingQuery = path.Substring(queryIndex + 1);
        }

        var domain = ImgixDomain.TrimEnd('/');
        var relativePath = pathOnly.StartsWith('/') ? pathOnly : "/" + pathOnly;

        var sb = new StringBuilder();
        sb.Append(domain);
        sb.Append(relativePath);
        sb.Append("?");

        if (!string.IsNullOrWhiteSpace(auto))
        {
            if (!auto.Contains("compress", StringComparison.OrdinalIgnoreCase))
            {
                auto = auto + ",compress";
            }
            sb.Append("auto=").Append(auto);
        }
        else
        {
            sb.Append("auto=compress");
        }

        // Add cs=origin to preserve original color profile and prevent color changes during compression
        sb.Append("&cs=origin");

        if (width > 0)
        {
            sb.Append("&w=").Append(width);
        }

        if (height > 0)
        {
            sb.Append("&h=").Append(height);
            if (!string.IsNullOrWhiteSpace(fit))
            {
                sb.Append("&fit=").Append(fit);
                if (fit == "crop" && !string.IsNullOrWhiteSpace(crop))
                {
                    sb.Append("&crop=").Append(crop);
                }
            }
        }

        // Preserve non-language query parameters if any existed
        if (!string.IsNullOrWhiteSpace(existingQuery))
        {
            var queryParams = existingQuery.Split('&');
            foreach (var qp in queryParams)
            {
                if (!qp.StartsWith("language=", StringComparison.OrdinalIgnoreCase) &&
                    !qp.StartsWith("cs=", StringComparison.OrdinalIgnoreCase) &&
                    !qp.StartsWith("auto=", StringComparison.OrdinalIgnoreCase))
                {
                    sb.Append("&").Append(qp);
                }
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generates the 5 responsive breakpoints (375w, 600w, 768w, 1024w, 1920w) matching K13 PageHero implementation.
    /// </summary>
    public static string ResponsiveDataSource(this string? url, int height = 1000, bool cropFit = false)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        return $"{url.Imgixify(width: 375, height: height)} 375w, " +
               $"{url.Imgixify(width: 600, height: height)} 600w, " +
               $"{url.Imgixify(width: 768, height: height)} 768w, " +
               $"{url.Imgixify(width: 1024, height: height)} 1024w, " +
               $"{url.Imgixify(width: 1920, height: height)} 1920w";
    }

    public static string ResponsiveDataSource(this MediaFile? mediaFile, int height = 1000, bool cropFit = false) =>
        mediaFile?.LegacyMediaFileAsset?.Url.ResponsiveDataSource(height, cropFit) ?? string.Empty;

    public static string ResponsiveDataSource(this ImageAssetViewModel? imageVm, int height = 1000, bool cropFit = false) =>
        imageVm?.LegacyMediaFileAsset?.Url.ResponsiveDataSource(height, cropFit) ?? string.Empty;

    public static string ResponsiveDataSource(this ContentItemAsset? asset, int height = 1000, bool cropFit = false) =>
        asset?.Url.ResponsiveDataSource(height, cropFit) ?? string.Empty;

    /// <summary>
    /// Generates a Low-Quality Image Placeholder (LQIP) URL for blur-up lazy loading, default width=3 matching K13.
    /// </summary>
    public static string ImgixifyLowRes(this MediaFile? mediaFile, int width = 3, int blur = 0) =>
        mediaFile?.LegacyMediaFileAsset?.Url.ImgixifyLowRes(width, blur) ?? string.Empty;

    /// <summary>
    /// Generates a Low-Quality Image Placeholder (LQIP) URL for blur-up lazy loading, default width=3 matching K13.
    /// </summary>
    public static string ImgixifyLowRes(this ImageAssetViewModel? imageVm, int width = 3, int blur = 0) =>
        imageVm?.LegacyMediaFileAsset?.Url.ImgixifyLowRes(width, blur) ?? string.Empty;

    /// <summary>
    /// Generates a Low-Quality Image Placeholder (LQIP) URL for blur-up lazy loading, default width=3 matching K13.
    /// </summary>
    public static string ImgixifyLowRes(this ContentItemAsset? asset, int width = 3, int blur = 0) =>
        asset?.Url.ImgixifyLowRes(width, blur) ?? string.Empty;

    /// <summary>
    /// Generates a Low-Quality Image Placeholder (LQIP) URL for blur-up lazy loading, default width=3 matching K13.
    /// </summary>
    public static string ImgixifyLowRes(this string? url, int width = 30, int blur = 30) =>
        url.Imgixify(width: width) + (blur > 0 ? $"&blur={blur}" : string.Empty);

    /// <summary>
    /// Generates a DPR-based srcset string (e.g. 1x, 2x) for retina and responsive displays.
    /// </summary>
    public static string ImgixSrcset(
        this string? url,
        int width,
        int height = 0,
        int[]? dprs = null,
        string fit = "crop",
        string auto = "format,compress",
        string crop = "")
    {
        if (string.IsNullOrWhiteSpace(url)) return string.Empty;
        if (url.Split('?')[0].EndsWith(".svg", StringComparison.OrdinalIgnoreCase)) return string.Empty;

        dprs ??= [1, 2];
        return string.Join(", ", dprs.Select(dpr =>
        {
            var w = width * dpr;
            var h = height > 0 ? height * dpr : 0;
            return $"{url.Imgixify(w, h, fit, auto, crop)} {dpr}x";
        }));
    }

    /// <summary>
    /// Generates a DPR-based srcset string (e.g. 1x, 2x) for retina and responsive displays.
    /// </summary>
    public static string ImgixSrcset(
        this MediaFile? mediaFile,
        int width,
        int height = 0,
        int[]? dprs = null,
        string fit = "crop",
        string auto = "format,compress",
        string crop = "") =>
        mediaFile?.LegacyMediaFileAsset?.Url.ImgixSrcset(width, height, dprs, fit, auto, crop) ?? string.Empty;

    /// <summary>
    /// Generates a DPR-based srcset string (e.g. 1x, 2x) for retina and responsive displays.
    /// </summary>
    public static string ImgixSrcset(
        this ImageAssetViewModel? imageVm,
        int width,
        int height = 0,
        int[]? dprs = null,
        string fit = "crop",
        string auto = "format,compress",
        string crop = "") =>
        imageVm?.LegacyMediaFileAsset?.Url.ImgixSrcset(width, height, dprs, fit, auto, crop) ?? string.Empty;

    /// <summary>
    /// Generates a DPR-based srcset string (e.g. 1x, 2x) for retina and responsive displays.
    /// </summary>
    public static string ImgixSrcset(
        this ContentItemAsset? asset,
        int width,
        int height = 0,
        int[]? dprs = null,
        string fit = "crop",
        string auto = "format,compress",
        string crop = "") =>
        asset?.Url.ImgixSrcset(width, height, dprs, fit, auto, crop) ?? string.Empty;
}
