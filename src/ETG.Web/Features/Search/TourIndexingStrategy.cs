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
    public const string FIELD_PRICE_INCLUSIONS = "PriceInclusions";
    public const string FIELD_MORE_OPTIONS = "MoreOptions";
    public const string FIELD_TOUR_CODE = "TourCode";

    private static readonly ContentQueryExecutionOptions BackgroundQueryOptions =
        new() { ForPreview = false, IncludeSecuredItems = true };

    public override IndexSettings GetAlgoliaIndexSettings() => new()
    {
        AttributesToRetrieve = [FIELD_TITLE, FIELD_DESC, FIELD_TOUR_CODE, FIELD_PRICE, FIELD_DURATION, FIELD_DESTINATIONS, FIELD_EXPERIENCES, FIELD_TRAVEL_STYLES, FIELD_START_LOCATION, FIELD_END_LOCATION, FIELD_IMAGE, FIELD_PRICE_INCLUSIONS, FIELD_MORE_OPTIONS, "OriginalPrice", "HasDiscount", "DiscountAmount"],
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
                    var websiteChannel = new ObjectQuery("cms.websitechannel").WhereEquals("WebsiteChannelID", channelId).FirstOrDefault();
                    if (websiteChannel != null)
                    {
                        int cId = websiteChannel.GetIntegerValue("WebsiteChannelChannelID", 0);
                        var channel = new ObjectQuery("cms.channel").WhereEquals("ChannelID", cId).FirstOrDefault();
                        if (channel != null)
                        {
                            channelName = channel.GetStringValue("ChannelName", null);
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

        // Retrieve first image from SelectedImageFolders (Tour -> ImageFolder -> Image -> MediaFile -> Asset)
        string imageUrl = string.Empty;
        var firstImage = tour.SelectedImageFolders?
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

        // Fallback to TourHeroImage if no image found in SelectedImageFolders
        if (string.IsNullOrWhiteSpace(imageUrl) && tour.TourHeroImage != null)
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

        var isOnSale = IsTourOnSale(contentItemId, priceResult);

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
            [FIELD_IMAGE] = imageUrl,
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
            ["OriginalPrice"] = priceResult.OriginalPrice,
            ["HasDiscount"] = isOnSale || priceResult.HasDiscount,
            ["DiscountAmount"] = priceResult.DiscountAmount,
            [FIELD_PRICE] = priceResult.CurrentPrice,
            ["Url"] = tourUrl,
            ["Date"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        return [jObject];
    }

    private static HashSet<int>? _onSaleContentItemIds;
    private static DateTime _onSaleCacheTime = DateTime.MinValue;
    private static readonly object _onSaleLock = new();

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