using ETG.Web.Rendering;

namespace ETG.Web.Features.GenericPage;

public sealed class GenericPageViewModel
{
    public string PageName { get; init; } = string.Empty;

    public GenericPageHeroViewModel PageHero { get; init; } = new();

    public IReadOnlyList<GenericPageLinkViewModel> ContainerPages
    { get; init; } = [];

    public int ContainerPageCount => ContainerPages.Count;
}

public sealed class GenericPageHeroViewModel
{
    public string Heading { get; init; } = string.Empty;
    public string HeadingSummary { get; init; } = string.Empty;
    public ImageAssetViewModel? HeroImage { get; init; }
    public string HeroImageAltText { get; init; } = string.Empty;
    public string HeroCaption { get; init; } = string.Empty;
    public string HeroContactUsCtaPath { get; init; } = string.Empty;
    public string HeroPhone { get; init; } = string.Empty;
}

public sealed class GenericPageLinkViewModel
{
    public string Name { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string TreePath { get; init; } = string.Empty;
}