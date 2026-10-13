using CMS.Websites;
using ETG.Core.Content;
using ETG.Core.Content.Pages;
using ETG.Web.Rendering;
using Kentico.Content.Web.Mvc;

namespace ETG.Web.Features.Destinations;

public class DestinationLandingPageViewModel
{
    public DestinationLandingPageDetailsViewModel Page { get; init; } = new();
    public string ContainerJsonSchema { get; init; } = string.Empty;

    public IReadOnlyList<DestinationLandingItemViewModel> Items { get; init; } = [];

    public static async Task<DestinationLandingPageViewModel> CreateAsync(
        Container container,
        IReadOnlyList<Destination> destinations,
        IWebPageUrlRetriever urlRetriever)
    {
        var items = new List<DestinationLandingItemViewModel>();

        foreach (var destination in destinations)
        {
            items.Add(await DestinationLandingItemViewModel.CreateAsync(
                destination,
                urlRetriever));
        }

        return new DestinationLandingPageViewModel
        {
            Page = DestinationLandingPageDetailsViewModel.Create(container),
            Items = items,
            ContainerJsonSchema = container.ContainerJsonSchema ?? string.Empty,
        };
    }
}

public class DestinationLandingPageDetailsViewModel
{
    public string Heading { get; init; } = "";

    public string HeadingText { get; init; } = "";

    public ImageAssetViewModel? HeroImage { get; init; }

    public string HeroImageAltText { get; init; } = "";


    public static DestinationLandingPageDetailsViewModel Create(Container container)
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
            HeroImageAltText = container.ContainerHeroImageAltTag ?? ""
        };
    }
}

public class DestinationLandingItemViewModel
{
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
            Heading = !string.IsNullOrWhiteSpace(destination.DestinationName)
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