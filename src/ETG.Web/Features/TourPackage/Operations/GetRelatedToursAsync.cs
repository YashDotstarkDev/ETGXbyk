using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Content;
using MediatR;

namespace ETG.Web.Features.TourPackage.Operations;

public sealed record GetRelatedToursQuery(Tour CurrentTour)
    : IRequest<List<Tour>>;


public sealed class GetRelatedToursQueryHandler(
    IContentQueryExecutor executor,
    IWebsiteChannelContext channelContext)
    : IRequestHandler<GetRelatedToursQuery, List<Tour>>
{
    private const int MaxTours = 8;

    public async Task<List<Tour>> Handle(
        GetRelatedToursQuery request,
        CancellationToken cancellationToken)
    {
        var currentTour = request.CurrentTour;

        if (currentTour?.SystemFields == null)
        {
            return [];
        }

        var currentExperienceGuids = GetExperienceGuids(currentTour);
        var currentTourTypes = GetTourTypes(currentTour);

        var candidateTours = await GetToursFromRelatedTourCodes(
            currentTour,
            cancellationToken);

        var usingExplicitRelatedTours = candidateTours.Count > 0;

        if (!usingExplicitRelatedTours)
        {
            candidateTours = await GetToursFromDestinations(
                currentTour,
                cancellationToken);
        }

        candidateTours = RemoveCurrentTour(
            candidateTours,
            currentTour);

        if (candidateTours.Count == 0)
        {
            return [];
        }

        // OPTIMIZED: Work with bounded result set
        var relatedTours = candidateTours
            .Where(tour =>
                HasCommonExperience(
                    tour,
                    currentExperienceGuids) &&
                HasCommonTourType(
                    tour,
                    currentTourTypes))
            .Take(MaxTours)
            .ToList();

        if (relatedTours.Count < MaxTours)
        {
            var remainingSlots = MaxTours - relatedTours.Count;
            var existingCodes = GetExistingTourCodes(relatedTours);

            var additionalTours = candidateTours
                .Where(tour =>
                    !existingCodes.Contains(tour.TourCode) &&
                    HasCommonExperience(tour, currentExperienceGuids))
                .Take(remainingSlots)
                .ToList();

            relatedTours.AddRange(additionalTours);
        }

        if (relatedTours.Count < MaxTours)
        {
            var remainingSlots = MaxTours - relatedTours.Count;
            var existingCodes = GetExistingTourCodes(relatedTours);

            var additionalTours = candidateTours
                .Where(tour => !existingCodes.Contains(tour.TourCode))
                .Take(remainingSlots)
                .ToList();

            relatedTours.AddRange(additionalTours);
        }

        if (relatedTours.Count == 0)
        {
            return [];
        }

        // Hydrate with full linked items if using destination-based candidates
        if (!usingExplicitRelatedTours)
        {
            relatedTours = await HydrateFinalTours(
                relatedTours,
                cancellationToken);
        }

        return relatedTours
            .Take(MaxTours)
            .ToList();
    }

    private async Task<List<Tour>> GetToursFromRelatedTourCodes(
        Tour currentTour,
        CancellationToken cancellationToken)
    {
        var relatedGuids = currentTour.TourRelatedTourCodes?
            .Where(x =>
                x != null &&
                x.WebPageGuid != Guid.Empty)
            .Select(x => x.WebPageGuid)
            .Distinct()
            .ToArray();

        if (relatedGuids == null || relatedGuids.Length == 0)
        {
            return [];
        }

        var query = new ContentItemQueryBuilder()
            .ForContentType(
                Tour.CONTENT_TYPE_NAME,
                config => config
                    .ForWebsite(
                        channelContext.WebsiteChannelName)
                    .WithLinkedItems(3)
                    .Where(where =>
                        where.WhereIn(
                            "WebPageItemGUID",
                            relatedGuids)));

        var tours = await executor.GetMappedWebPageResult<Tour>(
            query,
            cancellationToken: cancellationToken);

        return tours
            .Where(IsValidTour)
            .GroupBy(
                tour => tour.TourCode,
                StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Take(MaxTours)
            .ToList();
    }

    // In-memory indexed destination-to-tour map: ChannelName -> (Expiry, Map: DestinationGuid -> List<TourWebPageGuid>)
    private static readonly ConcurrentDictionary<string, (DateTime Expiry, Dictionary<Guid, List<Guid>> Map)> DestinationTourMapCache =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly SemaphoreSlim CacheLock = new(1, 1);
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);

    private async Task<Dictionary<Guid, List<Guid>>> GetDestinationTourMapAsync(
        string channelName,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        if (DestinationTourMapCache.TryGetValue(channelName, out var cached) && cached.Expiry > now)
        {
            return cached.Map;
        }

        await CacheLock.WaitAsync(cancellationToken);
        try
        {
            if (DestinationTourMapCache.TryGetValue(channelName, out cached) && cached.Expiry > DateTime.UtcNow)
            {
                return cached.Map;
            }

            var query = new ContentItemQueryBuilder()
                .ForContentType(
                    Tour.CONTENT_TYPE_NAME,
                    config => config
                        .ForWebsite(channelName)
                        .WithLinkedItems(1));

            var tours = await executor.GetMappedWebPageResult<Tour>(
                query,
                cancellationToken: cancellationToken);

            var map = new Dictionary<Guid, List<Guid>>();

            foreach (var tour in tours.Where(IsValidTour))
            {
                var tourWebPageGuid = tour.SystemFields?.WebPageItemGUID ?? Guid.Empty;
                if (tourWebPageGuid == Guid.Empty)
                {
                    continue;
                }

                var tourDestGuids = GetDestinationGuids(tour);
                foreach (var destGuid in tourDestGuids)
                {
                    if (!map.TryGetValue(destGuid, out var tourList))
                    {
                        tourList = new List<Guid>();
                        map[destGuid] = tourList;
                    }
                    tourList.Add(tourWebPageGuid);
                }
            }

            DestinationTourMapCache[channelName] = (DateTime.UtcNow.Add(CacheDuration), map);
            return map;
        }
        finally
        {
            CacheLock.Release();
        }
    }

    private async Task<List<Tour>> GetToursFromDestinations(
        Tour currentTour,
        CancellationToken cancellationToken)
    {
        var destinationGuids = GetDestinationGuids(
            currentTour);

        if (destinationGuids.Count == 0)
        {
            return [];
        }

        var map = await GetDestinationTourMapAsync(
            channelContext.WebsiteChannelName,
            cancellationToken);

        var currentTourGuid = currentTour.SystemFields?.WebPageItemGUID ?? Guid.Empty;

        // Prioritize primary destination matches first, then sub-country matches
        var primaryCountryGuid = currentTour.TourPrimaryCountry?
            .FirstOrDefault(x => x != null && x.WebPageGuid != Guid.Empty)?.WebPageGuid ?? Guid.Empty;

        var matchingTourGuids = new List<Guid>();
        var seenTourGuids = new HashSet<Guid>();

        void AddToursForDestination(Guid destGuid)
        {
            if (destGuid != Guid.Empty && map.TryGetValue(destGuid, out var tourGuids))
            {
                foreach (var guid in tourGuids)
                {
                    if (guid != currentTourGuid && seenTourGuids.Add(guid))
                    {
                        matchingTourGuids.Add(guid);
                    }
                }
            }
        }

        if (primaryCountryGuid != Guid.Empty)
        {
            AddToursForDestination(primaryCountryGuid);
        }

        foreach (var destGuid in destinationGuids)
        {
            if (destGuid != primaryCountryGuid)
            {
                AddToursForDestination(destGuid);
            }
        }

        if (matchingTourGuids.Count == 0)
        {
            return [];
        }

        // Bounded candidate set: take up to (MaxTours + 20) candidates
        var candidateGuids = matchingTourGuids
            .Take(MaxTours + 20)
            .ToArray();

        // Indexed seek by WebPageItemGUID instead of leading-wildcard WhereLike
        var query = new ContentItemQueryBuilder()
            .ForContentType(
                Tour.CONTENT_TYPE_NAME,
                config => config
                    .ForWebsite(
                        channelContext.WebsiteChannelName)
                    .WithLinkedItems(1)
                    .Where(where => where.WhereIn("WebPageItemGUID", candidateGuids)));

        var tours = await executor.GetMappedWebPageResult<Tour>(
            query,
            cancellationToken: cancellationToken);

        var tourLookup = tours
            .Where(IsValidTour)
            .GroupBy(tour => tour.SystemFields.WebPageItemGUID)
            .ToDictionary(
                group => group.Key,
                group => group.First());

        // Preserve candidate relevance order and deduplicate by TourCode
        return candidateGuids
            .Where(tourLookup.ContainsKey)
            .Select(guid => tourLookup[guid])
            .GroupBy(
                tour => tour.TourCode,
                StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Take(MaxTours)
            .ToList();
    }

    private async Task<List<Tour>> HydrateFinalTours(
        List<Tour> selectedTours,
        CancellationToken cancellationToken)
    {
        var selectedGuids = selectedTours
            .Where(tour =>
                tour?.SystemFields != null &&
                tour.SystemFields.WebPageItemGUID != Guid.Empty)
            .Select(tour =>
                tour.SystemFields.WebPageItemGUID)
            .Distinct()
            .ToArray();

        if (selectedGuids.Length == 0)
        {
            return selectedTours;
        }

        var query = new ContentItemQueryBuilder()
            .ForContentType(
                Tour.CONTENT_TYPE_NAME,
                config => config
                    .ForWebsite(
                        channelContext.WebsiteChannelName)
                    .WithLinkedItems(3)
                    .Where(where =>
                        where.WhereIn(
                            "WebPageItemGUID",
                            selectedGuids)));

        var hydratedTours =
            await executor.GetMappedWebPageResult<Tour>(
                query,
                cancellationToken: cancellationToken);

        var hydratedLookup = hydratedTours
            .Where(IsValidTour)
            .GroupBy(
                tour => tour.SystemFields.WebPageItemGUID)
            .ToDictionary(
                group => group.Key,
                group => group.First());


        return selectedTours
            .Select(tour =>
            {
                if (tour?.SystemFields == null)
                {
                    return tour;
                }

                return hydratedLookup.TryGetValue(
                    tour.SystemFields.WebPageItemGUID,
                    out var hydratedTour)
                    ? hydratedTour
                    : tour;
            })
            .Where(tour => tour != null)
            .ToList()!;
    }


    private static HashSet<Guid> GetDestinationGuids(
        Tour tour)
    {
        var guids = new HashSet<Guid>();

        // Primary Country
        var primaryCountry = tour.TourPrimaryCountry?
            .FirstOrDefault(x =>
                x != null &&
                x.WebPageGuid != Guid.Empty);

        if (primaryCountry != null)
        {
            guids.Add(
                primaryCountry.WebPageGuid);
        }

        // Sub Countries
        if (tour.TourSubCountries != null)
        {
            foreach (var destination in tour.TourSubCountries)
            {
                if (destination != null &&
                    destination.WebPageGuid != Guid.Empty)
                {
                    guids.Add(
                        destination.WebPageGuid);
                }
            }
        }

        return guids;
    }


    private static HashSet<Guid> GetExperienceGuids(
        Tour tour)
    {
        return tour.TourExperiences?
            .Where(x =>
                x != null &&
                x.WebPageGuid != Guid.Empty)
            .Select(x => x.WebPageGuid)
            .ToHashSet()
            ?? [];
    }


    private static bool HasCommonExperience(
        Tour candidate,
        HashSet<Guid> currentExperienceGuids)
    {
        if (currentExperienceGuids.Count == 0)
        {
            return false;
        }

        return candidate.TourExperiences?
            .Any(x =>
                x != null &&
                x.WebPageGuid != Guid.Empty &&
                currentExperienceGuids.Contains(
                    x.WebPageGuid))
            == true;
    }


    private static HashSet<string> GetTourTypes(
        Tour tour)
    {
        return tour.TourTypes?
            .Where(type =>
                !string.IsNullOrWhiteSpace(type))
            .Select(type => type.Trim())
            .ToHashSet(
                StringComparer.OrdinalIgnoreCase)
            ?? [];
    }


    private static bool HasCommonTourType(
        Tour candidate,
        HashSet<string> currentTourTypes)
    {
        if (currentTourTypes.Count == 0)
        {
            return false;
        }

        return candidate.TourTypes?
            .Any(type =>
                !string.IsNullOrWhiteSpace(type) &&
                currentTourTypes.Contains(
                    type.Trim()))
            == true;
    }


    private static List<Tour> RemoveCurrentTour(
        IEnumerable<Tour> tours,
        Tour currentTour)
    {
        return tours
            .Where(IsValidTour)
            .Where(tour =>
                !string.Equals(
                    tour.TourCode,
                    currentTour.TourCode,
                    StringComparison.OrdinalIgnoreCase))
            .GroupBy(
                tour => tour.TourCode,
                StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }


    private static HashSet<string> GetExistingTourCodes(
        IEnumerable<Tour> tours)
    {
        return tours
            .Where(tour =>
                !string.IsNullOrWhiteSpace(
                    tour.TourCode))
            .Select(tour =>
                tour.TourCode.Trim())
            .ToHashSet(
                StringComparer.OrdinalIgnoreCase);
    }


    private static bool IsValidTour(Tour? tour)
    {
        if (tour?.SystemFields == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(
                tour.TourCode))
        {
            return false;
        }

        // Do not display hidden upgrade tours.
        if (string.Equals(
                tour.TourIsUpgrade,
                "HiddenUpgradeTour",
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }
}