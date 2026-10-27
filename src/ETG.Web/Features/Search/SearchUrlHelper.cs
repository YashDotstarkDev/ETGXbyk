using System;

namespace ETG.Web.Features.Search;

/// <summary>
/// Helper to construct search filter URLs matching Kentico 13 SearchUrlHelper.
/// </summary>
public static class SearchUrlHelper
{
    public static string GetExperienceFilterUrl(string tourIndex, string? experience)
    {
        if (string.IsNullOrWhiteSpace(experience))
        {
            return "/search";
        }

        return $"/search?{tourIndex}%5BrefinementList%5D%5Bexperiences%5D%5B0%5D={Uri.EscapeDataString(experience.Trim())}";
    }

    public static string GetDestinationFilterUrl(string tourIndex, string? destination, string? tab = null)
    {
        if (string.IsNullOrWhiteSpace(destination))
        {
            return "/search";
        }

        var url = $"/search?{tourIndex}%5BrefinementList%5D%5Bdestination%5D%5B0%5D={Uri.EscapeDataString(destination.Trim())}";
        if (!string.IsNullOrEmpty(tab))
        {
            url += $"&tab={Uri.EscapeDataString(tab)}";
        }

        return url;
    }
}
