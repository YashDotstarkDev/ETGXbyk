using CMS.DataEngine;
using CMS.Helpers;
using ETG.Core.Operations;
using ETGBooking;

namespace ETG.Web.Features.TourPackage.Operations;

public record GetActiveTourPromotionQuery(
    string TourCode
) : IQuery<PromotionInfo?>, ICacheByValueQuery
{
    public string CacheValueKey => TourCode ?? string.Empty;
}

public class GetActiveTourPromotionQueryHandler(
    DataItemQueryTools tools,
    IInfoProvider<PromotionInfo> promotionProvider
) : DataItemQueryHandler<GetActiveTourPromotionQuery, PromotionInfo?>(tools)
{
    public override async Task<PromotionInfo?> Handle(
        GetActiveTourPromotionQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TourCode))
        {
            return null;
        }

        var now = DateTime.UtcNow;

        // OPTIMIZED: Filter at database level before retrieving
        var promotions = await promotionProvider
            .Get()
            .WhereNotNull(nameof(PromotionInfo.PromotionPackageCodes))
            .WhereNotEmpty(nameof(PromotionInfo.PromotionPackageCodes))
            .WhereGreaterOrEquals(nameof(PromotionInfo.PromotionToDate), now)
            .WhereLessOrEquals(nameof(PromotionInfo.PromotionFromDate), now)
            .OrderByDescending(nameof(PromotionInfo.PromotionFromDate))
            .GetEnumerableTypedResultAsync();

        // Final filtering for tour code match (package codes stored as multi-line text)
        return promotions
            .FirstOrDefault(promotion =>
                promotion.PromotionPackageCodes
                    .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Any(x => string.Equals(x, request.TourCode, StringComparison.OrdinalIgnoreCase)));
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(
        GetActiveTourPromotionQuery query,
        PromotionInfo? result,
        ICacheDependencyKeysBuilder builder)
    {
        // CACHE DEPENDENCY FIX: Add dependencies even when no promotion found
        if (result is not null)
        {
            // Specific promotion found - depend on it
            builder = builder.Object(PromotionInfo.OBJECT_TYPE, result.PromotionID);
        }
        
        // CRITICAL FIX: Always depend on all active promotions
        // This ensures cache invalidates when:
        // - A new promotion is created that matches this tour code
        // - An existing promotion is edited to add/remove this tour code
        // - A promotion expires or becomes active based on date range
        builder = builder.AllObjects(PromotionInfo.OBJECT_TYPE);
        
        return builder;
    }

    public override CacheSettings CustomizeCacheSettings(
        DefaultQueryCacheSettings settings,
        IQueryCacheKeysCreator<GetActiveTourPromotionQuery, PromotionInfo?> creator,
        GetActiveTourPromotionQuery query)
    {
        // CACHE SETTINGS FIX: Promotions are time-sensitive (from/to dates)
        // Use absolute expiration with shorter duration to handle date-based activation/expiration
        // Cache will expire even if no edits occur, preventing stale promotions
        return new CacheSettings(
            cacheMinutes: 10,
            useSlidingExpiration: false,
            cacheItemNameParts: creator.ItemNameParts(query));
    }
}
