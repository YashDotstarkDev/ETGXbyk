using ETG.Web.Rendering;

namespace ETG.Web.Features;

public class PrimaryLandingListingItemViewModel
{
    public string Title { get; init; } = "";
    public string Summary { get; init; } = "";
    public string LinkPath { get; init; } = "";
    public ImageAssetViewModel? Image { get; init; }
    public string ImageAlt { get; init; } = "";
    public string CtaText { get; init; } = "";
}

public class PrimaryLandingListingViewModel
{
    public IReadOnlyList<PrimaryLandingListingItemViewModel> Items { get; init; } = [];
}