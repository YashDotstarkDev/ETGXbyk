using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using ETG.Core.Content;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace ETG.Web.Infrastructure;

public class WebPageMetaService(
    IMediator mediator,
    IHttpContextAccessor contextAccessor)
{
    private readonly IMediator mediator = mediator;
    private readonly IHttpContextAccessor contextAccessor = contextAccessor;

    private WebPageMeta meta = WebPageMeta.Default;

    public async Task<WebPageMeta> GetMeta()
    {
        string pageTitle = meta.Title;

        meta = meta with { Title = pageTitle };

        return meta;
    }

    // Preserve the existing overload.
    public void SetMeta(WebPageMeta meta)
    {
        this.meta = meta with
        {
            ShareImage = ToAbsoluteUrl(meta.ShareImage)
        };
    }

    // Preserve existing controller calls.
    public void SetMeta(IMetaDataSchema metaFields)
    {
        meta = new WebPageMeta(metaFields)
        {
            ShareImage = ToAbsoluteUrl(
                ResolveImageUrl(metaFields))
        };
    }

    // Optional explicit image for content types with other field names.
    public void SetMeta(
        IMetaDataSchema metaFields,
        string? shareImageUrl)
    {
        meta = new WebPageMeta(metaFields)
        {
            ShareImage = ToAbsoluteUrl(shareImageUrl)
        };
    }

    private static string ResolveImageUrl(object page)
    {
        var properties = page.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property =>
                property.CanRead &&
                property.GetIndexParameters().Length == 0)
            .ToArray();

        // Prefer a dedicated social sharing image.
        var shareImageUrl = FindImageUrl(properties, page, "ShareImage");

        if (!string.IsNullOrWhiteSpace(shareImageUrl))
        {
            return shareImageUrl;
        }

        // Otherwise use the page's hero image.
        return FindImageUrl(properties, page, "HeroImage");
    }

    private static string FindImageUrl(
        PropertyInfo[] properties,
        object page,
        string fieldSuffix)
    {
        var matchingProperties = properties
            .Where(property =>
                property.Name.EndsWith(
                    fieldSuffix,
                    StringComparison.OrdinalIgnoreCase))
            .OrderBy(property =>
                property.Name.Equals(
                    fieldSuffix,
                    StringComparison.OrdinalIgnoreCase)
                    ? 0
                    : 1)
            .ThenBy(property => property.Name, StringComparer.Ordinal)
            .ToArray();

        foreach (var property in matchingProperties)
        {
            var value = property.GetValue(page);
            var imageUrl = GetImageUrl(value);

            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                return imageUrl;
            }
        }

        return string.Empty;
    }

    private static string GetImageUrl(object? value)
    {
        // Supports an existing URL-valued image field.
        if (value is string imageUrl)
        {
            return imageUrl;
        }

        // Supports a single loaded MediaFile.
        if (value is MediaFile mediaFile)
        {
            return mediaFile.LegacyMediaFileAsset?.Url
                ?? string.Empty;
        }

        // Supports linked-image collections such as
        // PageHeroImage and DestinationHeroImage.
        if (value is IEnumerable linkedItems)
        {
            foreach (var item in linkedItems)
            {
                if (item is MediaFile image)
                {
                    var url = image.LegacyMediaFileAsset?.Url;

                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        return url;
                    }
                }
            }
        }

        return string.Empty;
    }

    private string ToAbsoluteUrl(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return string.Empty;
        }

        imageUrl = imageUrl.Trim();

        if (imageUrl.StartsWith("//", StringComparison.Ordinal))
        {
            var scheme = contextAccessor.HttpContext?
                .Request.Scheme ?? "https";

            return $"{scheme}:{imageUrl}";
        }

        if (Uri.TryCreate(
            imageUrl,
            UriKind.Absolute,
            out var absoluteUri))
        {
            return absoluteUri.Scheme == Uri.UriSchemeHttps
                || absoluteUri.Scheme == Uri.UriSchemeHttp
                    ? absoluteUri.AbsoluteUri
                    : string.Empty;
        }

        var request = contextAccessor.HttpContext?.Request;

        if (request is null)
        {
            return string.Empty;
        }

        if (imageUrl.StartsWith("~/", StringComparison.Ordinal))
        {
            imageUrl = imageUrl[2..];
        }

        var applicationUrl = new Uri(
            $"{request.Scheme}://{request.Host}" +
            $"{request.PathBase}/");

        return Uri.TryCreate(applicationUrl, imageUrl, out var resolvedUrl)
            ? resolvedUrl.AbsoluteUri
            : string.Empty;
    }
}