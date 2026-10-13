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

    private static readonly ContentQueryExecutionOptions BackgroundQueryOptions =
        new() { ForPreview = true, IncludeSecuredItems = true };

    public override IndexSettings GetAlgoliaIndexSettings() => new()
    {
        AttributesToRetrieve = [FIELD_TITLE, FIELD_DESC, FIELD_PRICE, FIELD_DURATION, FIELD_DESTINATIONS, FIELD_EXPERIENCES, FIELD_TRAVEL_STYLES, FIELD_START_LOCATION, FIELD_END_LOCATION, FIELD_IMAGE, FIELD_PRICE_INCLUSIONS],
        SearchableAttributes = [FIELD_TITLE, FIELD_DESC, FIELD_DESTINATIONS, FIELD_EXPERIENCES, FIELD_TRAVEL_STYLES],
        AttributesForFaceting = [$"searchable({FIELD_DESTINATIONS})", $"searchable({FIELD_EXPERIENCES})", $"searchable({FIELD_TRAVEL_STYLES})", $"searchable({FIELD_START_LOCATION})", $"searchable({FIELD_END_LOCATION})", FIELD_PRICE, FIELD_DURATION],
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
           .ForContentType("ETG.Tour", cfg => cfg
                .Where(w => w.WhereEquals("ContentItemID", contentItemId))
                .TopN(1));

        var results = await queryExecutor.GetWebPageResult(query, contentQueryResultMapper.Map<ETG.Core.Content.Tour>, BackgroundQueryOptions);
        var tour = results.FirstOrDefault();
        if (tour is null) return null;

        string url = treePath.ToLowerInvariant();
        var heroImage = tour.TourHeroImage?.FirstOrDefault();
        var imageUrl = heroImage is MediaFile mf ? mf.LegacyMediaFileAsset?.Url ?? string.Empty : string.Empty;

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
            else if (!string.IsNullOrWhiteSpace(t)) travelStyleRawNames.Add(t.Trim());
        }
        var travelStyleNames = OmniResolveGuids(travelStyleIds).Concat(travelStyleRawNames).ToList();

        if (tour.TourIsCruise && !string.IsNullOrWhiteSpace(tour.TourCruiseType))
        {
            travelStyleNames.Add(tour.TourCruiseType.Trim());
        }
        travelStyleNames = travelStyleNames.Distinct().ToList();

        var priceInclusionsList = (tour.TourPriceInclusions ?? "")
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .ToList();

        using var scope = serviceProvider.CreateScope();
        var tourPricingService = scope.ServiceProvider.GetRequiredService<ITourPricingService>();
        var priceResult = await tourPricingService.GetPriceAsync(tour);

        var jObject = new JObject
        {
            [FIELD_TITLE] = tour.TourName ?? "",
            [FIELD_DESC] = tour.TourSummary ?? "",
            [FIELD_DURATION] = tour.TourNights > 0 ? tour.TourNights + 1 : 0,
            [FIELD_START_LOCATION] = tour.TourDepartureCity ?? "",
            [FIELD_END_LOCATION] = !string.IsNullOrWhiteSpace(tour.TourDestinationCity) ? tour.TourDestinationCity : (tour.TourDepartureCity ?? ""),
            [FIELD_IMAGE] = imageUrl,
            [FIELD_DESTINATIONS] = new JArray(destinationNames),
            [FIELD_EXPERIENCES] = new JArray(experienceNames),
            [FIELD_TRAVEL_STYLES] = new JArray(travelStyleNames),
            [FIELD_PRICE_INCLUSIONS] = new JArray(priceInclusionsList),
            [FIELD_PRICE] = priceResult.CurrentPrice,
            ["Url"] = "/" + url.Trim('~', '/').Split('/').Last(),
            ["Date"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        return [jObject];
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






