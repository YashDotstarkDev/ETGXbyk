using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Helpers;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Content;
using ETG.Web.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Sidio.Sitemap.Core;

namespace ETG.Web.Features.SEO;

public class SitemapRetriever(
    IProgressiveCache cache,
    IWebPageUrlRetriever urlRetriever,
    IWebsiteChannelContext website,
    IContentQueryExecutor executor,
    ICacheDependencyBuilderFactory cacheBuilderFactory,
    IHttpContextAccessor httpContextAccessor,
    IInfoProvider<WebsiteChannelInfo>? websiteChannelInfoProvider = null,
    IConfiguration? configuration = null)
{
    private readonly IProgressiveCache cache = cache;
    private readonly IWebPageUrlRetriever urlRetriever = urlRetriever;
    private readonly IWebsiteChannelContext website = website;
    private readonly IContentQueryExecutor executor = executor;
    private readonly ICacheDependencyBuilderFactory cacheBuilderFactory = cacheBuilderFactory;
    private readonly IHttpContextAccessor httpContextAccessor = httpContextAccessor;
    private readonly IInfoProvider<WebsiteChannelInfo>? websiteChannelInfoProvider = websiteChannelInfoProvider;
    private readonly IConfiguration? configuration = configuration;

    public async Task<List<SitemapNode>> GetSitemapNodes(string? customBaseUrl = null)
    {
        string baseUrl = GetBaseUrl(customBaseUrl);
        string hostKey = string.IsNullOrWhiteSpace(baseUrl) ? "no-host" : baseUrl;

        return await cache.LoadAsync(async cs =>
        {
            var builder = cacheBuilderFactory.Create();

            cs.CacheDependency = builder.ForWebPageItems().All(website.WebsiteChannelName).Builder().Build();

            return await GetSitemapNodesInternal(baseUrl);
        }, new CacheSettings(60, [nameof(GetSitemapNodes), website.WebsiteChannelName, hostKey]));
    }

    private string GetBaseUrl(string? customBaseUrl = null)
    {
        
        if (!string.IsNullOrWhiteSpace(customBaseUrl))
        {
            return NormalizeBaseUrl(customBaseUrl);
        }

        var request = httpContextAccessor.HttpContext?.Request;
        if (request != null && request.Host.HasValue)
        {
            return $"{request.Scheme}://{request.Host}{request.PathBase}".TrimEnd('/');
        }

        try
        {
            var channel = websiteChannelInfoProvider?.Get(website.WebsiteChannelID);
            if (channel != null && !string.IsNullOrWhiteSpace(channel.WebsiteChannelDomain))
            {
                return NormalizeBaseUrl(channel.WebsiteChannelDomain);
            }

            var channelItem = new ObjectQuery("cms.websitechannel")
                .WhereEquals("WebsiteChannelID", website.WebsiteChannelID)
                .FirstOrDefault();

            if (channelItem != null)
            {
                string domain = channelItem.GetStringValue("WebsiteChannelDomain", string.Empty);
                if (!string.IsNullOrWhiteSpace(domain))
                {
                    return NormalizeBaseUrl(domain);
                }
            }
        }
        catch
        {
            // Graceful fallback if channel lookup fails
        }

        
        if (configuration != null)
        {
            string? configDomain = configuration["Website:BaseUrl"] ?? configuration["SiteDomain"];
            if (!string.IsNullOrWhiteSpace(configDomain))
            {
                return NormalizeBaseUrl(configDomain);
            }
        }

        return string.Empty;
    }

    private static string NormalizeBaseUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        url = url.Trim().TrimEnd('/');
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            url = "https://" + url;
        }

        return url;
    }

    private async Task<List<SitemapNode>> GetSitemapNodesInternal(string baseUrl)
    {
        var nodes = new List<SitemapNode>();
        var addedUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Fetch last modified dates from CMS_ContentItemLanguageMetadata
        var lastModifiedDates = new Dictionary<int, DateTime>();
        try
        {
            var metadataItems = new ObjectQuery("cms.contentitemlanguagemetadata")
                .Columns("ContentItemLanguageMetadataContentItemID", "ContentItemLanguageMetadataModifiedWhen") // ✅ changed
                .ToList();

            foreach (var item in metadataItems)
            {
                int contentItemId = item.GetIntegerValue("ContentItemLanguageMetadataContentItemID", 0);
                DateTime modifiedWhen = item.GetDateTimeValue("ContentItemLanguageMetadataModifiedWhen", DateTime.MinValue); // ✅ changed

                if (contentItemId > 0 && modifiedWhen != DateTime.MinValue)
                {
                    if (!lastModifiedDates.TryGetValue(contentItemId, out var existing) || modifiedWhen > existing) // ✅ changed
                    {
                        lastModifiedDates[contentItemId] = modifiedWhen; 
                    }
                }
            }
        }
        catch
        {
            // Graceful fallback if metadata table query fails
        }

        var b = new ContentItemQueryBuilder()
            .ForContentTypes(c => c
                .OfReusableSchema(IMetaDataSchema.REUSABLE_FIELD_SCHEMA_NAME)
                .ForWebsite(website.WebsiteChannelName, includeUrlPath: true))
            .Parameters(p => p
                .Columns(
                    nameof(WebPageFields.WebPageItemID),
                    nameof(WebPageFields.WebPageItemGUID),
                    nameof(WebPageFields.WebPageItemName),
                    nameof(WebPageFields.WebPageItemOrder),
                    nameof(WebPageFields.WebPageItemParentID),
                    nameof(WebPageFields.WebPageItemTreePath),
                    nameof(WebPageFields.WebPageUrlPath),
                    nameof(WebPageFields.WebPageItemWebsiteChannelId),
                    nameof(WebPageFields.WebPageUrlPathContentLanguageID),
                    "ContentItemCommonDataLastPublishedWhen",
                    "ContentItemCommonDataContentLanguageID",
                    "ContentItemCommonDataVersionStatus",
                    "ContentItemContentTypeID",
                    "ContentItemGUID",
                    "ContentItemID",
                    "ContentItemIsSecured",
                    "ContentItemName",
                    nameof(IMetaDataSchema.MetaDataSchema__ExcludeFromSitemap)
                ));

        var pages = await executor.GetWebPageResult(b, c =>
        {
            bool isExcluded = c.TryGetValue(nameof(IMetaDataSchema.MetaDataSchema__ExcludeFromSitemap), out bool val) && val;

            c.TryGetValue("ContentItemCommonDataLastPublishedWhen", out DateTime lastPublished);

            return new SitemapPage(new()
            {
                WebPageItemID = c.WebPageItemID,
                WebPageItemGUID = c.WebPageItemGUID,
                WebPageItemName = c.WebPageItemName,
                WebPageItemOrder = c.WebPageItemOrder,
                WebPageItemParentID = c.WebPageItemParentID,
                WebPageItemTreePath = c.WebPageItemTreePath,
                WebPageUrlPath = c.WebPageUrlPath,
                WebPageItemWebsiteChannelId = c.WebPageItemWebsiteChannelID,
                WebPageUrlPathContentLanguageID = c.ContentItemCommonDataContentLanguageID,

                ContentItemCommonDataLastPublishedWhen = lastPublished != default ? lastPublished : null,
                ContentItemCommonDataContentLanguageID = c.ContentItemCommonDataContentLanguageID,
                ContentItemCommonDataVersionStatus = c.ContentItemCommonDataVersionStatus,
                ContentItemContentTypeID = c.ContentItemContentTypeID,
                ContentItemGUID = c.ContentItemGUID,
                ContentItemID = c.ContentItemID,
                ContentItemIsSecured = c.ContentItemIsSecured,
                ContentItemName = c.ContentItemName
            }, isInSitemap: !isExcluded);
        });

        foreach (var page in pages)
        {
            if (!page.IsInSitemap)
                continue;

            var pageUrl = await urlRetriever.Retrieve(page);
            var relativePath = pageUrl.RelativePathTrimmed();

            if (string.IsNullOrWhiteSpace(relativePath))
                continue;

            if (!relativePath.StartsWith("/"))
                relativePath = "/" + relativePath;

            string absoluteUrl = string.IsNullOrEmpty(baseUrl) ? relativePath : $"{baseUrl}{relativePath}";

            if (!addedUrls.Add(absoluteUrl))
                continue;

            DateTime? lastModified = null;

            if (page.SystemFields.ContentItemCommonDataLastPublishedWhen.HasValue &&
                page.SystemFields.ContentItemCommonDataLastPublishedWhen.Value != default)
            {
                lastModified = page.SystemFields.ContentItemCommonDataLastPublishedWhen.Value;
            }
            else if (lastModifiedDates.TryGetValue(page.SystemFields.ContentItemID, out var modifiedDate) 
                && modifiedDate != DateTime.MinValue)
            {
                lastModified = modifiedDate; 
            }

            var node = new SitemapNode(absoluteUrl)
            {
                LastModified = lastModified,
                ChangeFrequency = ChangeFrequency.Weekly,
            };

            nodes.Add(node);
        }

        return nodes;
    }

    public struct SitemapPage(WebPageFields systemFields, bool isInSitemap) : IWebPageFieldsSource
    {
        public WebPageFields SystemFields { get; set; } = systemFields;
        public bool IsInSitemap { get; set; } = isInSitemap;
    }
}