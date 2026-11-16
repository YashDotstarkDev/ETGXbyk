using Algolia.Search.Models.Settings;
using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Websites;
using ETG.Core.Content;
using Kentico.Xperience.Algolia.Indexing;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ETG.Web;
using Microsoft.Extensions.DependencyInjection;

namespace ETG.Web.Features.Search;

public class ArticleIndexingStrategy(
    IContentQueryResultMapper contentQueryResultMapper,
    IContentQueryExecutor queryExecutor,
    ILogger<ArticleIndexingStrategy> logger)
    : DefaultAlgoliaIndexingStrategy
{
    public const string FIELD_TITLE = "title";
    public const string FIELD_SUMMARY = "summary";
    public const string FIELD_URL = "url";
    public const string FIELD_IMAGE = "image";
    public const string FIELD_DATE = "date";
    public const string FIELD_AUTHOR = "author";
    public const string FIELD_DESTINATION = "destination";
    public const string FIELD_CATEGORY = "category";
    public const string FIELD_EXPERIENCE = "experience";

    private static readonly ContentQueryExecutionOptions BackgroundQueryOptions =
        new() { ForPreview = false, IncludeSecuredItems = true };

    public override IndexSettings GetAlgoliaIndexSettings() => new()
    {
        AttributesToRetrieve = [FIELD_TITLE, FIELD_SUMMARY, FIELD_URL, FIELD_IMAGE, FIELD_DATE, FIELD_AUTHOR, FIELD_DESTINATION, FIELD_CATEGORY, FIELD_EXPERIENCE],
        SearchableAttributes = [FIELD_TITLE, FIELD_SUMMARY, FIELD_DESTINATION, FIELD_CATEGORY, FIELD_EXPERIENCE],
        AttributesForFaceting = [$"searchable({FIELD_DESTINATION})", $"searchable({FIELD_CATEGORY})", $"searchable({FIELD_EXPERIENCE})"],
        Ranking = ["desc(date)", "typo", "geo", "words", "filters", "proximity", "attribute", "exact", "custom"]
    };

    public override async Task<IEnumerable<JObject>?> MapToAlgoliaJObjectsOrNull(IIndexEventItemModel item)
    {
        try
        {
            return await MapArticleAsync(item.ItemGuid);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ArticleIndex: failed to map Article item {Guid}", item.ItemGuid);
            return null;
        }
    }

    private async Task<IEnumerable<JObject>?> MapArticleAsync(Guid itemGuid)
    {
        int contentItemId = 0;
        int webPageItemId = 0;
        string treePath = "";

        try
        {
            var page = new ObjectQuery("cms.webpageitem").WhereEquals("WebPageItemGUID", itemGuid).FirstOrDefault();
            if (page != null)
            {
                webPageItemId = page.GetIntegerValue("WebPageItemID", 0);
                contentItemId = page.GetIntegerValue("WebPageItemContentItemID", 0);
                treePath = page.GetStringValue("WebPageItemTreePath", "");
            }

            if (contentItemId == 0)
            {
                var cItem = new ObjectQuery("cms.contentitem").WhereEquals("ContentItemGUID", itemGuid).FirstOrDefault();
                if (cItem != null) contentItemId = cItem.GetIntegerValue("ContentItemID", 0);
            }
        }
        catch { }

        if (contentItemId == 0) return null;

        var query = new ContentItemQueryBuilder()
           .ForContentType("ETG.Article", cfg => cfg
                .Where(w => w.WhereEquals("ContentItemID", contentItemId))
                .WithLinkedItems(2)
                .TopN(1));

        var results = await queryExecutor.GetMappedWebPageResult<ETG.Core.Content.Article>(query, BackgroundQueryOptions);
        var article = results.FirstOrDefault();
        if (article is null) return null;

        string url = treePath.ToLowerInvariant();
        var heroImage = article.ArticleHeroImage?.OfType<MediaFile>().FirstOrDefault() ?? article.ArticleHeroImage?.FirstOrDefault() as MediaFile;
        var asset = heroImage?.LegacyMediaFileAsset;
        string imageUrl = string.Empty;

        if (asset != null)
        {
            if (!string.IsNullOrWhiteSpace(asset.Url))
            {
                imageUrl = asset.Url.TrimStart('~');
            }
            else if (asset.Metadata != null)
            {
                imageUrl = $"/getContentAsset/{asset.Metadata.Identifier}/{asset.Metadata.Name}";
            }
        }

        var destinationIds = article.ArticleDestinations?.Select(d => d.WebPageGuid).ToList() ?? new List<Guid>();
        var destinationNames = OmniResolveGuids(destinationIds);

        var categoryIds = article.ArticleCategories?.ToList() ?? new List<Guid>();
        var categoryNames = OmniResolveGuids(categoryIds);

        var experienceIds = article.ArticleExperiences?.Select(e => e.WebPageGuid).ToList() ?? new List<Guid>();
        var experienceNames = OmniResolveGuids(experienceIds);

        string articleUrl = string.Empty;
        if (webPageItemId > 0)
        {
            try
            {
                var urlPathRecord = new ObjectQuery("cms.webpageurlpath")
                    .WhereEquals("WebPageUrlPathWebPageItemID", webPageItemId)
                    .WhereTrue("WebPageUrlPathIsLatest")
                    .OrderByDescending("WebPageUrlPathIsDraft")
                    .FirstOrDefault();

                if (urlPathRecord != null)
                {
                    string path = urlPathRecord.GetStringValue("WebPageUrlPath", "");
                    if (path.StartsWith("en/", StringComparison.OrdinalIgnoreCase))
                    {
                        path = path.Substring(3);
                    }
                    articleUrl = path;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to resolve URL via cms.webpageurlpath for WebPageItemID {WebPageItemID}", webPageItemId);
            }
        }

        articleUrl = articleUrl.TrimStart('~');

        if (string.IsNullOrWhiteSpace(articleUrl))
        {
            articleUrl = "/" + url.Trim('~', '/').Split('/').Last();
        }
        else if (!articleUrl.StartsWith("/"))
        {
            articleUrl = "/" + articleUrl;
        }

        articleUrl = articleUrl.ToLowerInvariant();

        return [new JObject
        {
            [FIELD_TITLE]       = article.ArticleTitle ?? article.DocumentName ?? "",
            [FIELD_SUMMARY]     = article.ArticleSummary ?? "",
            [FIELD_URL]         = articleUrl,
            [FIELD_IMAGE]       = imageUrl.Imgixify(),
            [FIELD_DATE]        = article.ArticleDate.ToString("o"),
            [FIELD_AUTHOR]      = "",
            [FIELD_DESTINATION] = new JArray(destinationNames),
            [FIELD_CATEGORY]    = new JArray(categoryNames),
            [FIELD_EXPERIENCE]  = new JArray(experienceNames)
        }];
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, string> GuidNameCache = new();

    private List<string> OmniResolveGuids(List<Guid> guids)
    {
        if (guids == null || !guids.Any()) return new List<string>();

        var names = new List<string>();
        var uncachedGuids = new List<Guid>();

        foreach (var guid in guids)
        {
            if (GuidNameCache.TryGetValue(guid, out var cachedName))
            {
                if (!string.IsNullOrWhiteSpace(cachedName))
                {
                    names.Add(cachedName);
                }
            }
            else
            {
                uncachedGuids.Add(guid);
            }
        }

        if (!uncachedGuids.Any())
        {
            return names.Distinct().ToList();
        }

        try
        {
            var pages = new ObjectQuery("cms.webpageitem").WhereIn("WebPageItemGUID", uncachedGuids.ToArray()).ToList();
            foreach (var page in pages)
            {
                var guid = page.GetGuidValue("WebPageItemGUID", Guid.Empty);
                var clean = CleanName(page.GetStringValue("WebPageItemName", ""));
                if (guid != Guid.Empty && !string.IsNullOrWhiteSpace(clean))
                {
                    GuidNameCache[guid] = clean;
                    names.Add(clean);
                    uncachedGuids.Remove(guid);
                }
            }
        }
        catch { }

        if (uncachedGuids.Any())
        {
            try
            {
                var items = new ObjectQuery("cms.contentitem").WhereIn("ContentItemGUID", uncachedGuids.ToArray()).ToList();
                foreach (var item in items)
                {
                    var guid = item.GetGuidValue("ContentItemGUID", Guid.Empty);
                    var clean = CleanName(item.GetStringValue("ContentItemName", ""));
                    if (guid != Guid.Empty && !string.IsNullOrWhiteSpace(clean))
                    {
                        GuidNameCache[guid] = clean;
                        names.Add(clean);
                        uncachedGuids.Remove(guid);
                    }
                }
            }
            catch { }
        }

        if (uncachedGuids.Any())
        {
            try
            {
                var tags = new ObjectQuery("cms.taxonomytag").WhereIn("TagGUID", uncachedGuids.ToArray()).ToList();
                foreach (var tag in tags)
                {
                    var guid = tag.GetGuidValue("TagGUID", Guid.Empty);
                    var clean = CleanName(tag.GetStringValue("TagTitle", ""));
                    if (guid != Guid.Empty && !string.IsNullOrWhiteSpace(clean))
                    {
                        GuidNameCache[guid] = clean;
                        names.Add(clean);
                        uncachedGuids.Remove(guid);
                    }
                }
            }
            catch { }
        }

        if (uncachedGuids.Any())
        {
            try
            {
                var inClause = string.Join(",", uncachedGuids.Select(g => $"'{g}'"));
                var sql = $"SELECT ArticleCategoryGuid, ArticleCategoryName FROM ETG_ArticleCategory WHERE ArticleCategoryGuid IN ({inClause})";
                var ds = ConnectionHelper.ExecuteQuery(sql, null, QueryTypeEnum.SQLQuery);
                if (ds != null && ds.Tables.Count > 0)
                {
                    foreach (System.Data.DataRow row in ds.Tables[0].Rows)
                    {
                        if (Guid.TryParse(row["ArticleCategoryGuid"]?.ToString(), out var g))
                        {
                            string name = row["ArticleCategoryName"]?.ToString() ?? "";
                            var clean = CleanName(name);
                            if (!string.IsNullOrWhiteSpace(clean))
                            {
                                GuidNameCache[g] = clean;
                                names.Add(clean);
                                uncachedGuids.Remove(g);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to resolve Category via SQL");
            }
        }

        foreach (var remaining in uncachedGuids)
        {
            GuidNameCache.TryAdd(remaining, string.Empty);
        }

        return names.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToList();
    }

    private string CleanName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";
        if (Guid.TryParse(name, out _)) return "";

        var match = Regex.Match(name, @"^([A-Za-z\s_]+)-[a-z0-9]{8}$");
        if (match.Success) return match.Groups[1].Value.Replace("_", " ").Trim();

        return name.Trim();
    }

    public override Task<IEnumerable<IIndexEventItemModel>> FindItemsToReindex(IndexEventReusableItemModel changedItem) =>
        Task.FromResult<IEnumerable<IIndexEventItemModel>>([]);
}
