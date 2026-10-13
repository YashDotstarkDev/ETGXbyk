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
        new() { ForPreview = true, IncludeSecuredItems = true };

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
        string treePath = "";

        try
        {
            var page = new ObjectQuery("cms.webpageitem").WhereEquals("WebPageItemGUID", itemGuid).FirstOrDefault();
            if (page != null)
            {
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
                .TopN(1));

        var results = await queryExecutor.GetWebPageResult(query, contentQueryResultMapper.Map<ETG.Core.Content.Article>, BackgroundQueryOptions);
        var article = results.FirstOrDefault();
        if (article is null) return null;

        string url = treePath.ToLowerInvariant();
        var heroImage = article.ArticleHeroImage?.FirstOrDefault();
        var imageUrl = heroImage is MediaFile mf ? mf.LegacyMediaFileAsset?.Url ?? string.Empty : string.Empty;

        var destinationIds = article.ArticleDestinations?.Select(d => d.WebPageGuid).ToList() ?? new List<Guid>();
        var destinationNames = OmniResolveGuids(destinationIds);

        var categoryIds = article.ArticleCategories?.ToList() ?? new List<Guid>();
        var categoryNames = OmniResolveGuids(categoryIds);

        var experienceIds = article.ArticleExperiences?.Select(e => e.WebPageGuid).ToList() ?? new List<Guid>();
        var experienceNames = OmniResolveGuids(experienceIds);

        return [new JObject
        {
            [FIELD_TITLE]       = article.ArticleTitle ?? article.DocumentName ?? "",
            [FIELD_SUMMARY]     = article.ArticleSummary ?? "",
            [FIELD_URL]         = url,
            [FIELD_IMAGE]       = imageUrl,
            [FIELD_DATE]        = article.ArticleDate.ToString("o"),
            [FIELD_AUTHOR]      = "",
            [FIELD_DESTINATION] = new JArray(destinationNames),
            [FIELD_CATEGORY]    = new JArray(categoryNames),
            [FIELD_EXPERIENCE]  = new JArray(experienceNames)
        }];
    }

    private List<string> OmniResolveGuids(List<Guid> guids)
    {
        if (!guids.Any()) return new List<string>();
        var names = new List<string>();

        try
        {
            var pages = new ObjectQuery("cms.webpageitem").WhereIn("WebPageItemGUID", guids.ToArray()).ToList();
            foreach (var page in pages) names.Add(CleanName(page.GetStringValue("WebPageItemName", "")));
        }
        catch { }

        try
        {
            var items = new ObjectQuery("cms.contentitem").WhereIn("ContentItemGUID", guids.ToArray()).ToList();
            foreach (var item in items) names.Add(CleanName(item.GetStringValue("ContentItemName", "")));
        }
        catch { }

        try
        {
            var tags = new ObjectQuery("cms.taxonomytag").WhereIn("TagGUID", guids.ToArray()).ToList();
            foreach (var tag in tags) names.Add(CleanName(tag.GetStringValue("TagTitle", "")));
        }
        catch { }

        try
        {
            var inClause = string.Join(",", guids.Select(g => $"'{g}'"));
            var sql = $"SELECT ArticleCategoryName FROM ETG_ArticleCategory WHERE ArticleCategoryGuid IN ({inClause})";
            var ds = ConnectionHelper.ExecuteQuery(sql, null, QueryTypeEnum.SQLQuery);
            if (ds != null && ds.Tables.Count > 0)
            {
                foreach (System.Data.DataRow row in ds.Tables[0].Rows)
                {
                    string name = row["ArticleCategoryName"]?.ToString() ?? "";
                    names.Add(CleanName(name));
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to resolve Category via SQL");
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
