using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Content;
using Kentico.Content.Web.Mvc;

namespace ETG.Admin.Services;

public sealed class TourVanityUrlService(
    IWebPageUrlPathDataRetriever urlDataRetriever,
    IVanityUrlPathNormalizer urlNormalizer,
    IWebPageManagerFactory pageManagerFactory,
    IContentQueryExecutor queryExecutor,
    IInfoProvider<WebsiteChannelInfo> websiteChannels,
    IInfoProvider<ChannelInfo> channels)
{
    // Confirm these match the real CruiseType dropdown option values.
    private static readonly Dictionary<string, string> CruiseSlugs =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["1"] = "self-drive-cruising",
            ["2"] = "barge-cruising",
            ["3"] = "river-cruising",
            ["4"] = "ocean-cruising"
        };

    // Blocks the re-trigger caused by our own publish.
    // Shared by the event handler and the backfill.
    private static readonly ConcurrentDictionary<string, byte> Active = new();

    private static readonly ConcurrentDictionary<int, string> ChannelNames = new();

    // URL path data can only be read from a draft, so a draft is created first
    // and always discarded unless it was published.
    // apply = false: reports what would change and writes nothing permanent.
    public async Task<TourVanityUrlResult> ProcessAsync(
        int channelId, int userId, int pageId, string lang, bool apply, CancellationToken ct)
    {
        var key = $"{channelId}:{pageId}:{lang}";

        if (!Active.TryAdd(key, 0))
        {
            return Fail("Page is already being processed.");
        }

        try
        {
            var manager = pageManagerFactory.Create(channelId, userId);

            if (!await manager.TryCreateDraft(pageId, lang, ct))
            {
                return Fail("Could not create a draft (the page may already have an unpublished draft).");
            }

            var published = false;

            try
            {
                var urls = await urlDataRetriever.GetDraftData(pageId, lang, ct);
                var slug = urls.SystemUrlSlug?.Trim('/');
                var (tour, prefix, rule) = await ResolveAsync(channelId, pageId, lang);
                var name = tour?.TourName ?? "Unknown";
                var current = urls.CanonicalUrlPath;
                var systemPath = urls.SystemUrlFullPath;

                if (string.IsNullOrWhiteSpace(slug))
                {
                    return new(name, current, null, TourVanityUrlRule.None, TourVanityUrlStatus.Unchanged);
                }

                // Desired canonical vanity URL; null means "no vanity URL wanted".
                var path = prefix is null ? null : $"/{prefix}/{slug}";

                // Previously auto-generated canonical vanity URL ("/{prefix}/{tour-slug}").
                // Vanity URLs added manually by editors are never touched.
                var stale = urls.VanityUrlPaths.FirstOrDefault(p =>
                    Same(p, current) && !Same(p, systemPath) && IsAutoPattern(p, slug));

                // Nothing to add and nothing to remove, or already correct.
                if (path is null ? stale is null : Same(current, path))
                {
                    return new(name, current, path, rule, TourVanityUrlStatus.Unchanged);
                }

                var target = path ?? systemPath;

                if (!apply)
                {
                    return new(name, current, target, rule, TourVanityUrlStatus.WouldChange);
                }

                if (path is null)
                {
                    // Nothing selected anymore: back to the system URL, drop the old vanity URL.
                    urls.SetCanonicalUrlPath(systemPath);
                    urls.RemoveVanityUrlPath(stale!);
                }
                else
                {
                    var vanity = urls.VanityUrlPaths.FirstOrDefault(p => Same(p, path));

                    if (vanity is null)
                    {
                        urls.AddVanityUrlPath(path);
                        vanity = urls.VanityUrlPaths.Single(p => Same(p, path));
                    }

                    urls.SetCanonicalUrlPath(vanity);

                    // Drop the previous auto-generated one (e.g. destination changed).
                    if (stale is not null)
                    {
                        urls.RemoveVanityUrlPath(stale);
                    }
                }

                if (await manager.TryUpdateDraft(pageId, lang, new UpdateDraftData(urls), ct))
                {
                    published = await manager.TryPublish(pageId, lang, ct);
                }

                return published
                    ? new(name, current, target, rule, TourVanityUrlStatus.Applied)
                    : new(name, current, target, rule, TourVanityUrlStatus.Failed,
                        "Update or publish failed (possible URL conflict).");
            }
            finally
            {
                if (!published)
                {
                    await manager.TryDiscardDraft(pageId, lang, CancellationToken.None);
                }
            }
        }
        finally
        {
            Active.TryRemove(key, out _);
        }
    }

    // Priority: Destination, then Experience, then CruiseType (only when Is Cruise is on).
    private async Task<(Tour? Tour, string? Prefix, TourVanityUrlRule Rule)> ResolveAsync(
        int channelId, int pageId, string lang)
    {
        var tour = await GetPageAsync<Tour>(
            Tour.CONTENT_TYPE_NAME, channelId, nameof(IWebPageContentQueryDataContainer.WebPageItemID), pageId, lang);

        if (tour is null)
        {
            return (null, null, TourVanityUrlRule.None);
        }

        if (await PageSlug<Destination>(Destination.CONTENT_TYPE_NAME, channelId,
                tour.TourPrimaryCountry?.FirstOrDefault()?.WebPageGuid, lang, d => d.DestinationName) is { } dest)
        {
            return (tour, dest, TourVanityUrlRule.Destination);
        }

        if (await PageSlug<Experience>(Experience.CONTENT_TYPE_NAME, channelId,
                tour.TourPrimaryExperience?.FirstOrDefault()?.WebPageGuid, lang, x => x.ExperienceName) is { } exp)
        {
            return (tour, exp, TourVanityUrlRule.Experience);
        }

        return tour.TourIsCruise is true && CruiseSlug(tour.TourCruiseType) is { } cruise
            ? (tour, cruise, TourVanityUrlRule.CruiseType)
            : (tour, null, TourVanityUrlRule.None);
    }

    // ForWebsite(channelName) joins the web page columns (WebPageItemID, WebPageItemGUID),
    // which a plain content type query does not have.
    private async Task<T?> GetPageAsync<T>(
        string contentType, int channelId, string column, object value, string lang)
        where T : IWebPageFieldsSource, new()
    {
        var query = new ContentItemQueryBuilder()
            .ForContentType(contentType, q => q
                .ForWebsite(ChannelName(channelId))
                .Where(w => w.WhereEquals(column, value))
                .TopN(1))
            .InLanguage(lang);

        return (await queryExecutor.GetMappedWebPageResult<T>(query)).FirstOrDefault();
    }

    // Linked page's name as a URL slug. (URL path data of a page without a draft
    // cannot be read, so the slug is derived from the page name.)
    private async Task<string?> PageSlug<T>(
        string contentType, int channelId, Guid? guid, string lang, Func<T, string?> name)
        where T : IWebPageFieldsSource, new()
    {
        if (guid is not { } id || id == Guid.Empty)
        {
            return null;
        }

        var page = await GetPageAsync<T>(
            contentType, channelId, nameof(IWebPageContentQueryDataContainer.WebPageItemGUID), id, lang);

        return page is null ? null : Clean(name(page));
    }

    private string ChannelName(int websiteChannelId) =>
        ChannelNames.GetOrAdd(websiteChannelId, id =>
            channels.Get(websiteChannels.Get(id).WebsiteChannelChannelID).ChannelName);

    private string? CruiseSlug(string? value)
    {
        var v = value?.Trim();

        if (string.IsNullOrEmpty(v))
        {
            return null;
        }

        if (CruiseSlugs.TryGetValue(v, out var slug))
        {
            return slug;
        }

        // "Please select" placeholder means no cruise type was chosen.
        if (v is "0" or "-1" || v.Contains("select", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return Clean(v);
    }

    private string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null
        : urlNormalizer.Normalize(value)?.Trim('/') is { Length: > 0 } s ? s : null;

    private static TourVanityUrlResult Fail(string message) =>
        new("Unknown", null, null, TourVanityUrlRule.None, TourVanityUrlStatus.Failed, message);

    // "/{prefix}/{tour-slug}" shape.
    private static bool IsAutoPattern(string path, string slug) =>
        path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries) is [_, var last] && Same(last, slug);

    private static bool Same(string? a, string? b) =>
        string.Equals(a?.Trim('/'), b?.Trim('/'), StringComparison.OrdinalIgnoreCase);
}

public record TourVanityUrlResult(
    string TourName,
    string? CurrentUrl,
    string? ComputedUrl,
    TourVanityUrlRule Rule,
    TourVanityUrlStatus Status,
    string? Message = null);

public enum TourVanityUrlStatus
{
    Unchanged,
    WouldChange,
    Applied,
    Failed
}

public enum TourVanityUrlRule
{
    None,
    Destination,
    Experience,
    CruiseType
}
