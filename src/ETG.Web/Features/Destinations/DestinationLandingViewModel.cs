using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Content;
using ETG.Core.Content.Pages;
using ETG.Web.Rendering;
using Kentico.Content.Web.Mvc;

namespace ETG.Web.Features.Destinations;

public class DestinationLandingPageViewModel
{
    public DestinationLandingPageDetailsViewModel Page { get; init; } =
        new();

    public string ContainerJsonSchema { get; init; } = string.Empty;

    public IReadOnlyList<DestinationLandingItemViewModel> Items
    {
        get;
        init;
    } = [];

    public IReadOnlyList<DestinationLandingCategoryViewModel> Categories
    {
        get;
        init;
    } = [];

    public static async Task<DestinationLandingPageViewModel> CreateAsync(
        Container container,
        IReadOnlyList<Destination> destinations,
        IWebPageUrlRetriever urlRetriever,
        ITaxonomyRetriever taxonomyRetriever,
        string languageName)
    {
        var items = new List<DestinationLandingItemViewModel>();

        // Preserve the existing destination order and URL mapping.
        foreach (var destination in destinations)
        {
            items.Add(
                await DestinationLandingItemViewModel.CreateAsync(
                    destination,
                    urlRetriever));
        }

        // Collect the selected category tag identifiers.
        var tagIdentifiers = destinations
            .SelectMany(destination =>
                destination.DestinationCategory
                    ?? Enumerable.Empty<TagReference>())
            .Select(reference => reference.Identifier)
            .Distinct()
            .ToArray();

        // Resolve tags once for the whole listing.
        var tags = tagIdentifiers.Length == 0
            ? new List<Tag>()
            : (await taxonomyRetriever.RetrieveTags(
                    tagIdentifiers,
                    languageName))
                .ToList();

        var tagsByIdentifier = tags
            .GroupBy(tag => tag.Identifier)
            .ToDictionary(
                group => group.Key,
                group => group.First());

        var groups =
            new List<DestinationLandingCategoryViewModel>();

        var groupsByIdentifier =
            new Dictionary<Guid, DestinationLandingCategoryViewModel>();

        var uncategorizedItems =
            new List<DestinationLandingItemViewModel>();

        for (var index = 0; index < destinations.Count; index++)
        {
            var destination = destinations[index];
            var item = items[index];

            var destinationTagIdentifiers =
                (destination.DestinationCategory
                    ?? Enumerable.Empty<TagReference>())
                .Select(reference => reference.Identifier)
                .Distinct();

            var assignedToCategory = false;

            foreach (var identifier in destinationTagIdentifiers)
            {
                if (!tagsByIdentifier.TryGetValue(
                        identifier,
                        out var tag))
                {
                    continue;
                }

                if (!groupsByIdentifier.TryGetValue(
                        identifier,
                        out var categoryGroup))
                {
                    categoryGroup =
                        new DestinationLandingCategoryViewModel
                        {
                            Heading = tag.Title ?? string.Empty,
                            Items =
                                new List<DestinationLandingItemViewModel>()
                        };

                    groupsByIdentifier.Add(identifier, categoryGroup);
                    groups.Add(categoryGroup);
                }

                // Items is initialized as a List above.
                ((List<DestinationLandingItemViewModel>)
                    categoryGroup.Items).Add(item);

                assignedToCategory = true;
            }

            if (!assignedToCategory)
            {
                uncategorizedItems.Add(item);
            }
        }

        if (uncategorizedItems.Count > 0)
        {
            groups.Add(new DestinationLandingCategoryViewModel
            {
                Heading = "Other Destinations",
                Items = uncategorizedItems
            });
        }

        return new DestinationLandingPageViewModel
        {
            Page = DestinationLandingPageDetailsViewModel.Create(container),
            Items = items,
            Categories = groups,
            ContainerJsonSchema =
                container.ContainerJsonSchema ?? string.Empty
        };
    }
}

public class DestinationLandingCategoryViewModel
{
    public string Heading { get; init; } = string.Empty;

    public IReadOnlyList<DestinationLandingItemViewModel> Items
    {
        get;
        init;
    } = [];
}

public class DestinationLandingPageDetailsViewModel
{
    public string Heading { get; init; } = "";

    public string HeadingText { get; init; } = "";

    public ImageAssetViewModel? HeroImage { get; init; }

    public string HeroImageAltText { get; init; } = "";

    public static DestinationLandingPageDetailsViewModel Create(
        Container container)
    {
        var heroImage = container.ContainerHeroImage?
            .OfType<MediaFile>()
            .FirstOrDefault();

        return new DestinationLandingPageDetailsViewModel
        {
            Heading = container.ContainerHeading ?? "",
            HeadingText = container.ContainerHeadingText ?? "",
            HeroImage = heroImage is null
                ? null
                : ImageAssetViewModel.Create(heroImage),
            HeroImageAltText =
                container.ContainerHeroImageAltTag ?? ""
        };
    }
}

public class DestinationLandingItemViewModel
{
    public Guid ContentItemGuid { get; init; }

    public string Heading { get; init; } = "";

    public string Summary { get; init; } = "";

    public string Path { get; init; } = "";

    public ImageAssetViewModel? Image { get; init; }

    public static async Task<DestinationLandingItemViewModel> CreateAsync(
        Destination destination,
        IWebPageUrlRetriever urlRetriever)
    {
        var heroImage = destination.DestinationHeroImage?
            .OfType<MediaFile>()
            .FirstOrDefault();

        var url = await urlRetriever.Retrieve(destination);

        return new DestinationLandingItemViewModel
        {
            ContentItemGuid =
                destination.SystemFields.ContentItemGUID,

            Heading =
                !string.IsNullOrWhiteSpace(destination.DestinationName)
                    ? destination.DestinationName
                    : destination.DestinationHeading ?? "",

            Summary = destination.DestinationSummary ?? "",

            Path = url.RelativePath,

            Image = heroImage is null
                ? null
                : ImageAssetViewModel.Create(heroImage)
        };
    }
}