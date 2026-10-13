namespace ETG.Web.Components.ViewComponents.Shared;

/// <summary>
/// Resolved, view-ready shape of the WebsiteLayoutContent page.
/// BrandLogoSvg and AwardsImageSvg hold raw inline SVG markup — always render
/// with @Html.Raw(...) in views, never as an image src.
/// </summary>
public class WebsiteLayoutContentViewModel
{
    public string? BrandLogoSvg { get; init; }
    public string? BrandLogoAltText { get; init; }
    public string? ContactNumber { get; init; }
    public string? NavCtaText { get; init; }
    public string? OneTreeLogoUrl { get; init; }
    public string? OneTreeLogoAltText { get; init; }
    public string? SustainableHeading { get; init; }
    public string? SustainableDescription { get; init; }
    public string? FooterLogoUrl { get; init; }
    public string? FooterLogoAltText { get; init; }
    public string? ContactUsText { get; init; }
    public string? AwardsImageSvg { get; init; }
    public string? AwardsImageAltText { get; init; }
    public string? CopyrightText { get; init; }

    public string? SearchPageUrl { get; init; }
    public string? ContactPageUrl { get; init; }
}