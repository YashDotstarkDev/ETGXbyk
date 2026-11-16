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
using ETG.Web.Services.Tour;
using Microsoft.Extensions.DependencyInjection;
using ETG.Web;

namespace ETG.Web.Features.Search;

public class TourIndexingStrategy(
    IContentQueryResultMapper contentQueryResultMapper,
    IContentQueryExecutor queryExecutor,
    ILogger<TourIndexingStrategy> logger,
    IServiceProvider serviceProvider
    )
    : DefaultAlgoliaIndexingStrategy
{
    public const string FIELD_TITLE = "TourName";
    public const string FIELD_DESC = "Description";
    public const string FIELD_PRICE = "Price";
    public const string FIELD_DURATION = "Duration";
    public const string FIELD_DESTINATIONS = "Destinations";
    public const string FIELD_EXPERIENCES = "Experiences";
    public const string FIELD_TRAVEL_STYLES = "TravelStyles";
    public const string FIELD_START_LOCATION = "StartLocation";
    public const string FIELD_END_LOCATION = "EndLocation";
    public const string FIELD_IMAGE = "ImageUrl";
    public const string FIELD_IMAGE_ALT = "ImageAlt";
    public const string FIELD_PRICE_INCLUSIONS = "PriceInclusions";
    public const string FIELD_MORE_OPTIONS = "MoreOptions";
    public const string FIELD_TOUR_CODE = "TourCode";

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, string> GuidNameCache = new();
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, string> ChannelNameCache = new();

    private static readonly ContentQueryExecutionOptions BackgroundQueryOptions =
        new() { ForPreview = false, IncludeSecuredItems = true };

    public override IndexSettings GetAlgoliaIndexSettings() => new()
    {
        AttributesToRetrieve = [FIELD_TITLE, FIELD_DESC, FIELD_TOUR_CODE, FIELD_PRICE, FIELD_DURATION, FIELD_DESTINATIONS, FIELD_EXPERIENCES, FIELD_TRAVEL_STYLES, FIELD_START_LOCATION, FIELD_END_LOCATION, FIELD_IMAGE, FIELD_IMAGE_ALT, FIELD_PRICE_INCLUSIONS, FIELD_MORE_OPTIONS, "OriginalPrice", "HasDiscount", "DiscountAmount"],
        SearchableAttributes = [FIELD_TOUR_CODE, FIELD_TITLE, FIELD_DESC, FIELD_DESTINATIONS, FIELD_EXPERIENCES, FIELD_TRAVEL_STYLES],
        DisableTypoToleranceOnAttributes = [FIELD_TOUR_CODE],
        AttributesForFaceting = [$"searchable({FIELD_DESTINATIONS})", $"searchable({FIELD_EXPERIENCES})", $"searchable({FIELD_TRAVEL_STYLES})", $"searchable({FIELD_START_LOCATION})", $"searchable({FIELD_END_LOCATION})", FIELD_MORE_OPTIONS, FIELD_PRICE, FIELD_DURATION],
    };

    public override async Task<IEnumerable<JObject>?> MapToAlgoliaJObjectsOrNull(IIndexEventItemModel item)
    {
        try
        {
            return await MapTourAsync(item.ItemGuid);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "TourIndex: failed to map Tour item {Guid}", item.ItemGuid);
            return null;
        }
    }

    private async Task<IEnumerable<JObject>?> MapTourAsync(Guid itemGuid)
    {
        int contentItemId = 0;
        int webPageItemId = 0;
        string treePath = "";
        string? channelName = null;

        try
        {
            var page = new ObjectQuery("cms.webpageitem").WhereEquals("WebPageItemGUID", itemGuid).FirstOrDefault();
            if (page != null)
            {
                webPageItemId = page.GetIntegerValue("WebPageItemID", 0);
                contentItemId = page.GetIntegerValue("WebPageItemContentItemID", 0);
                treePath = page.GetStringValue("WebPageItemTreePath", "");

                int channelId = page.GetIntegerValue("WebPageItemWebsiteChannelID", 0);
                if (channelId > 0)
                {
                    if (ChannelNameCache.TryGetValue(channelId, out var cachedCh))
                    {
                        channelName = cachedCh;
                    }
                    else
                    {
                        var websiteChannel = new ObjectQuery("cms.websitechannel").WhereEquals("WebsiteChannelID", channelId).FirstOrDefault();
                        if (websiteChannel != null)
                        {
                            int cId = websiteChannel.GetIntegerValue("WebsiteChannelChannelID", 0);
                            var channel = new ObjectQuery("cms.channel").WhereEquals("ChannelID", cId).FirstOrDefault();
                            if (channel != null)
                            {
                                channelName = channel.GetStringValue("ChannelName", null);
                                if (!string.IsNullOrEmpty(channelName))
                                {
                                    ChannelNameCache[channelId] = channelName;
                                }
                            }
                        }
                    }
                }
            }

            if (contentItemId == 0)
            {
                var cItem = new ObjectQuery("cms.contentitem").WhereEquals("ContentItemGUID", itemGuid).FirstOrDefault();
                if (cItem != null) contentItemId = cItem.GetIntegerValue("ContentItemID", 0);
            }
        }
        catch { }

        if (contentItemId == 0) return null;

        // 1. Primary Query: Depth 4 to retrieve SelectedImageFolders (Tour -> ImageFolder -> Image -> MediaFile -> Asset)
        var depth4Query = new ContentItemQueryBuilder()
           .ForContentType("ETG.Tour", cfg =>
           {
               if (!string.IsNullOrEmpty(channelName))
               {
                   cfg.ForWebsite(channelName);
               }
               cfg.Where(w => w.WhereEquals("ContentItemID", contentItemId))
                  .WithLinkedItems(4)
                  .TopN(1);
           });

        IEnumerable<ETG.Core.Content.Tour> results;
        try
        {
            results = await queryExecutor.GetMappedWebPageResult<ETG.Core.Content.Tour>(depth4Query, BackgroundQueryOptions);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "TourIndex: Depth 4 query failed for ContentItemID {ContentItemId}. Retrying with depth 2 fallback.", contentItemId);

            var depth2Query = new ContentItemQueryBuilder()
               .ForContentType("ETG.Tour", cfg =>
               {
                   if (!string.IsNullOrEmpty(channelName))
                   {
                       cfg.ForWebsite(channelName);
                   }
                   cfg.Where(w => w.WhereEquals("ContentItemID", contentItemId))
                      .WithLinkedItems(2)
                      .TopN(1);
               });

            results = await queryExecutor.GetMappedWebPageResult<ETG.Core.Content.Tour>(depth2Query, BackgroundQueryOptions);
        }

        var tour = results.FirstOrDefault();
        if (tour is null) return null;

        string url = treePath.ToLowerInvariant();

        string imageUrl = string.Empty;

        // 1. Primary: TourHeroImage (hero asset)
        if (tour.TourHeroImage != null)
        {
            var heroItem = tour.TourHeroImage.FirstOrDefault();
            if (heroItem != null)
            {
                MediaFile? heroMediaFile = heroItem switch
                {
                    MediaFile mf => mf,
                    ETG.Image img => img.ImageFilePath?.OfType<MediaFile>().FirstOrDefault()
                                  ?? img.ImageFilePath?.FirstOrDefault() as MediaFile,
                    _ => null
                };

                var heroAsset = heroMediaFile?.LegacyMediaFileAsset;
                if (heroAsset != null)
                {
                    if (!string.IsNullOrWhiteSpace(heroAsset.Url))
                    {
                        imageUrl = heroAsset.Url.TrimStart('~');
                    }
                    else if (heroAsset.Metadata != null)
                    {
                        imageUrl = $"/getContentAsset/{heroAsset.Metadata.Identifier}/{heroAsset.Metadata.Name}";
                    }
                }
            }
        }

        // 2. Fallback: Depth 4 SelectedImageFolders (Tour -> ImageFolder -> Image -> MediaFile -> Asset)
        ETG.Image? firstImage = null;
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            firstImage = tour.SelectedImageFolders?
                .Where(f => f?.SelectedImages != null)
                .SelectMany(f => f.SelectedImages)
                .OfType<ETG.Image>()
                .FirstOrDefault(img => img != null);

            if (firstImage != null)
            {
                var mediaFile = firstImage.ImageFilePath?
                    .OfType<MediaFile>()
                    .FirstOrDefault()
                    ?? firstImage.ImageFilePath?.FirstOrDefault() as MediaFile;

                var asset = mediaFile?.LegacyMediaFileAsset;
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
            }
        }

        // 3. Alt Tag: TourHeroImageAltTag, with fallback to firstImage?.ImageAltText or TourName
        string imageAlt = !string.IsNullOrWhiteSpace(tour.TourHeroImageAltTag)
            ? tour.TourHeroImageAltTag.Trim()
            : (!string.IsNullOrWhiteSpace(firstImage?.ImageAltText)
                ? firstImage.ImageAltText.Trim()
                : (tour.TourName ?? ""));

        // 1. FIX: Destinations now mapped from TourPrimaryCountry, not TourSubCountries
        var destinationIds = tour.TourPrimaryCountry?.Select(x => x.WebPageGuid).ToList() ?? new List<Guid>();
        var destinationNames = OmniResolveGuids(destinationIds);
        if (!destinationNames.Any() && !string.IsNullOrWhiteSpace(tour.TourPrimaryCountryOverride))
        {
            destinationNames.Add(tour.TourPrimaryCountryOverride.Trim());
        }

        // 2. TourExperiences remains IEnumerable<WebPageRelatedItem>
        var experienceIds = tour.TourExperiences?.Select(x => x.WebPageGuid).ToList() ?? new List<Guid>();
        var experienceNames = OmniResolveGuids(experienceIds);

        // 3. FIX: TourTypes is now IEnumerable<string>, plus TourCruiseType
        var travelStyleIds = new List<Guid>();
        var travelStyleRawNames = new List<string>();
        foreach (var t in tour.TourTypes ?? Enumerable.Empty<string>())
        {
            if (Guid.TryParse(t, out var g)) travelStyleIds.Add(g);
            else if (!string.IsNullOrWhiteSpace(t) && !int.TryParse(t.Trim(), out _)) travelStyleRawNames.Add(t.Trim());
        }
        var travelStyleNames = OmniResolveGuids(travelStyleIds).Concat(travelStyleRawNames).ToList();

        if (tour.TourIsCruise && !string.IsNullOrWhiteSpace(tour.TourCruiseType))
        {
            var cruiseVal = tour.TourCruiseType.Trim();
            var cruiseTypeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "1", "Self Drive Cruising" },
                { "2", "Barge Cruising" },
                { "3", "River Cruising" },
                { "4", "Ocean Cruising" }
            };

            if (cruiseTypeMap.TryGetValue(cruiseVal, out var cruiseName))
            {
                travelStyleNames.Add(cruiseName);
            }
            else if (cruiseVal != "0" && !int.TryParse(cruiseVal, out _))
            {
                travelStyleNames.Add(cruiseVal);
            }
        }

        travelStyleNames = travelStyleNames
            .Where(s => !string.IsNullOrWhiteSpace(s) && !int.TryParse(s.Trim(), out _))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var priceInclusionsList = (tour.TourPriceInclusions ?? "")
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .ToList();

        using var scope = serviceProvider.CreateScope();
        var tourPricingService = scope.ServiceProvider.GetRequiredService<ITourPricingService>();
        var priceResult = await tourPricingService.GetPriceAsync(tour);

        int originalPrice = priceResult.OriginalPrice;
        int currentPrice = priceResult.CurrentPrice;

        // Fallback: If priceResult is 0, check if the tour has a CMS package/cabin price in the database
        if (currentPrice <= 0)
        {
            int cmsPrice = GetCmsLowestPrice(tour.TourCode);
            if (cmsPrice > 0)
            {
                originalPrice = cmsPrice;
                currentPrice = cmsPrice;
            }
        }

        // Kentico 13 Parity: Exclude tours that have no price or expired pricing from Algolia search
        if (currentPrice <= 0)
        {
            return null;
        }

        var isOnSale = IsTourOnSale(contentItemId, priceResult);
        var hasDiscount = isOnSale || priceResult.HasDiscount || (originalPrice > 0 && currentPrice < originalPrice);
        var discountAmount = priceResult.DiscountAmount > 0
            ? priceResult.DiscountAmount
            : (hasDiscount && originalPrice > currentPrice ? originalPrice - currentPrice : 0);

        string tourUrl = string.Empty;
        try
        {
            var urlRetriever = scope.ServiceProvider.GetService<IWebPageUrlRetriever>();
            if (urlRetriever != null)
            {
                var webPageUrl = await urlRetriever.Retrieve(tour);
                if (webPageUrl != null && !string.IsNullOrWhiteSpace(webPageUrl.RelativePath))
                {
                    tourUrl = webPageUrl.RelativePath;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to resolve URL via IWebPageUrlRetriever for Tour {TourName}", tour.TourName);
        }

        if (string.IsNullOrWhiteSpace(tourUrl) && webPageItemId > 0)
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
                    tourUrl = path;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to resolve URL via cms.webpageurlpath for WebPageItemID {WebPageItemID}", webPageItemId);
            }
        }

        tourUrl = tourUrl.TrimStart('~');

        if (string.IsNullOrWhiteSpace(tourUrl))
        {
            tourUrl = "/" + url.Trim('~', '/').Split('/').Last();
        }
        else if (!tourUrl.StartsWith("/"))
        {
            tourUrl = "/" + tourUrl;
        }

        tourUrl = tourUrl.ToLowerInvariant();

        var moreOptions = new List<string>();
        if (tour.TourHasFreedomOfChoice) moreOptions.Add("Freedom of choice");
        if (tour.TourHasPeaceOfMindGuarantee) moreOptions.Add("Peace of mind");
        if (tour.TourHasSafeTravel) moreOptions.Add("Sustainable Travel");
        if (isOnSale) moreOptions.Add("On Sale");

        var jObject = new JObject
        {
            [FIELD_TITLE] = tour.TourName ?? "",
            [FIELD_TOUR_CODE] = tour.TourCode ?? "",
            [FIELD_DESC] = tour.TourSummary ?? "",
            [FIELD_DURATION] = tour.TourNights > 0 ? tour.TourNights + 1 : 0,
            [FIELD_START_LOCATION] = tour.TourDepartureCity ?? "",
            [FIELD_END_LOCATION] = !string.IsNullOrWhiteSpace(tour.TourDestinationCity) ? tour.TourDestinationCity : (tour.TourDepartureCity ?? ""),
            [FIELD_IMAGE] = imageUrl.Imgixify(),
            [FIELD_IMAGE_ALT] = imageAlt,
            [FIELD_DESTINATIONS] = new JArray(destinationNames),
            [FIELD_EXPERIENCES] = new JArray(experienceNames),
            [FIELD_TRAVEL_STYLES] = new JArray(travelStyleNames),
            [FIELD_PRICE_INCLUSIONS] = new JArray(priceInclusionsList),
            [FIELD_MORE_OPTIONS] = new JArray(moreOptions),
            ["BookNow"] = tour.TourBookNowEnabled,
            ["FreedomOfChoice"] = tour.TourHasFreedomOfChoice,
            ["PeaceOfMind"] = tour.TourHasPeaceOfMindGuarantee,
            ["SafeTravel"] = tour.TourHasSafeTravel,
            ["OnSale"] = isOnSale,
            ["OriginalPrice"] = originalPrice,
            ["HasDiscount"] = hasDiscount,
            ["DiscountAmount"] = discountAmount,
            [FIELD_PRICE] = currentPrice,
            ["Url"] = tourUrl,
            ["Date"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        return [jObject];
    }

    private static HashSet<int>? _onSaleContentItemIds;
    private static DateTime _onSaleCacheTime = DateTime.MinValue;
    private static readonly object _onSaleLock = new();

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, int> _cmsPriceCache = new(StringComparer.OrdinalIgnoreCase);
    private static DateTime _cmsPriceCacheTime = DateTime.MinValue;
    private static readonly object _cmsPriceLock = new();

    private static int GetCmsLowestPrice(string? tourCode)
    {
        if (string.IsNullOrWhiteSpace(tourCode)) return 0;

        lock (_cmsPriceLock)
        {
            if (_cmsPriceCache.Count == 0 || (DateTime.UtcNow - _cmsPriceCacheTime).TotalMinutes >= 10)
            {
                try
                {
                    const string sql = @"
SELECT 
    t.TourCode,
    MIN(v.Price) AS LowestPrice
FROM ETG_Tour t
JOIN CMS_ContentItemCommonData cd ON t.ContentItemDataCommonDataID = cd.ContentItemCommonDataID
CROSS APPLY OPENJSON(t.SelectedTourPricingFolders) WITH (Identifier UNIQUEIDENTIFIER '$.Identifier') f_guid
JOIN CMS_ContentItem ci_f ON ci_f.ContentItemGUID = f_guid.Identifier
JOIN CMS_ContentItemCommonData cd_f ON cd_f.ContentItemCommonDataContentItemID = ci_f.ContentItemID AND cd_f.ContentItemCommonDataIsLatest = 1
JOIN ETG_TourPricingFolder pf ON pf.ContentItemDataCommonDataID = cd_f.ContentItemCommonDataID
CROSS APPLY OPENJSON(pf.SelectedTourPricings) WITH (Identifier UNIQUEIDENTIFIER '$.Identifier') p_guid
JOIN CMS_ContentItem ci_p ON ci_p.ContentItemGUID = p_guid.Identifier
JOIN CMS_ContentItemCommonData cd_p ON cd_p.ContentItemCommonDataContentItemID = ci_p.ContentItemID AND cd_p.ContentItemCommonDataIsLatest = 1
JOIN ETG_TourPricing p ON p.ContentItemDataCommonDataID = cd_p.ContentItemCommonDataID
CROSS APPLY (
    SELECT CASE 
        WHEN p.TourPricingOverridePrice > 0 THEN p.TourPricingOverridePrice
        ELSE (
            SELECT MIN(val) FROM (VALUES 
                (p.TourPricingEconomyPrice1), (p.TourPricingPremiumPrice1), (p.TourPricingBusinessPrice1), (p.TourPricingFirstClassPrice1),
                (p.TourPricingEconomyPrice2), (p.TourPricingPremiumPrice2), (p.TourPricingBusinessPrice2), (p.TourPricingFirstClassPrice2),
                (p.TourPricingEconomyPrice3), (p.TourPricingPremiumPrice3), (p.TourPricingBusinessPrice3), (p.TourPricingFirstClassPrice3),
                (p.TourPricingEconomyPrice4), (p.TourPricingPremiumPrice4), (p.TourPricingBusinessPrice4), (p.TourPricingFirstClassPrice4)
            ) AS v(val) WHERE val > 0
        )
    END AS Price
) v
WHERE cd.ContentItemCommonDataIsLatest = 1
  AND v.Price > 0
GROUP BY t.TourCode";

                    var ds = ConnectionHelper.ExecuteQuery(sql, null, QueryTypeEnum.SQLQuery);
                    if (ds != null && ds.Tables.Count > 0)
                    {
                        _cmsPriceCache.Clear();
                        foreach (System.Data.DataRow row in ds.Tables[0].Rows)
                        {
                            var code = row[0]?.ToString();
                            if (!string.IsNullOrWhiteSpace(code) && row[1] != DBNull.Value)
                            {
                                _cmsPriceCache[code] = Convert.ToInt32(row[1]);
                            }
                        }
                        _cmsPriceCacheTime = DateTime.UtcNow;
                    }
                }
                catch
                {
                    // Ignore fallback error
                }
            }
        }

        if (_cmsPriceCache.TryGetValue(tourCode, out var cachedPrice) && cachedPrice > 0)
        {
            return cachedPrice;
        }

        return 0;
    }

    private HashSet<int> GetOnSaleContentItemIds()
    {
        lock (_onSaleLock)
        {
            if (_onSaleContentItemIds != null && (DateTime.UtcNow - _onSaleCacheTime).TotalMinutes < 5)
            {
                return _onSaleContentItemIds;
            }

            var ids = new HashSet<int>();
            try
            {
                const string sql = @"
SELECT cd.ContentItemCommonDataContentItemID
FROM ETG_Tour t
JOIN CMS_ContentItemCommonData cd ON t.ContentItemDataCommonDataID = cd.ContentItemCommonDataID
JOIN CMS_ContentItem ci ON t.TourDiscountAndOffer LIKE '%' + CAST(ci.ContentItemGUID AS VARCHAR(50)) + '%'
JOIN CMS_ContentItemCommonData cd_d ON cd_d.ContentItemCommonDataContentItemID = ci.ContentItemID
JOIN ETG_DiscountAndOffer d ON d.ContentItemDataCommonDataID = cd_d.ContentItemCommonDataID
WHERE d.DiscountIsOnSaleNow = 1 AND (d.DiscountExpiry IS NULL OR d.DiscountExpiry >= GETDATE())";

                var ds = ConnectionHelper.ExecuteQuery(sql, null, QueryTypeEnum.SQLQuery);
                if (ds != null && ds.Tables.Count > 0)
                {
                    foreach (System.Data.DataRow row in ds.Tables[0].Rows)
                    {
                        if (row[0] != DBNull.Value)
                        {
                            ids.Add(Convert.ToInt32(row[0]));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to resolve OnSale Tour ContentItemIDs via SQL");
            }

            _onSaleContentItemIds = ids;
            _onSaleCacheTime = DateTime.UtcNow;
            return ids;
        }
    }

    private bool IsTourOnSale(int contentItemId, TourPricingResult priceResult)
    {
        if (priceResult.HasDiscount) return true;
        if (contentItemId <= 0) return false;

        return GetOnSaleContentItemIds().Contains(contentItemId);
    }

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
