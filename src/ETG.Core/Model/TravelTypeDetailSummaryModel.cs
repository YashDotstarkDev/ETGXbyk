namespace ETG.Web.Features.TravelType;

/// <summary>
/// Reusable summary data for a Travel Type Detail callout card.
/// </summary>
public class TravelTypeDetailSummaryModel
{
    /// <summary>
    /// Card heading — from TravelTypeDetailHeading.
    /// </summary>
    public string Heading { get; init; } = "";

    /// <summary>
    /// Card summary — from TravelTypeDetailSummary.
    /// </summary>
    public string Summary { get; init; } = "";

    /// <summary>
    /// Resolved hero-image URL — from TravelTypeDetailHeroImage.
    /// </summary>
    public string HeroImageUrl { get; init; } = "";

    /// <summary>
    /// Hero-image alt text — from TravelTypeDetailHeroImageAltTag.
    /// </summary>
    public string HeroImageAltText { get; init; } = "";

    /// <summary>
    /// Detail-page URL for the call-to-action.
    /// </summary>
    public string Path { get; init; } = "#";
}