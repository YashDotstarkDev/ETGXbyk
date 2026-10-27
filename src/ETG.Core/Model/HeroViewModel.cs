namespace ETG.Core.Models;

public record HeroGalleryImageItem(string ImageUrl, string? AltText = "", string? Caption = "");

public class HeroViewModel
{
    public string HeroImageUrl { get; init; } = "";
    public string HeroImageAltText { get; init; } = "";

    public IReadOnlyList<HeroGalleryImageItem> GalleryImages { get; init; } = [];

    public string Heading { get; init; } = "";
    public string? HeadingText { get; init; }
    public string? HeroCaption { get; init; }
    public string? HeroAccreditation { get; init; }
    public string? HeroContactUsPath { get; init; }
    public string? HeroPhone { get; init; }
    public string? HeroForegroundImageUrl { get; init; }
    public string? HeroImageAccreditation { get; init; }
    public bool? DisableOverlay { get; init; }
    public string? HeroIconSvg { get; init; }
}