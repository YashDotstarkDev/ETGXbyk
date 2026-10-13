namespace ETG.Core.Models.Tour;

/// <summary>
/// View model for a single tour card, mapped from <see cref="ETG.Tour.Tour"/>.
/// </summary>
public class TourSummaryViewModel
{
    /// <summary>Tour display name — from TourName.</summary>
    public string Name { get; init; } = "";

    /// <summary>Tour code — from TourCode.</summary>
    public string TourCode { get; init; } = "";

    /// <summary>Relative URL of the Tour web page.</summary>
    public string Path { get; init; } = "#";

    /// <summary>Card body paragraph — from TourSummary.</summary>
    public string Description { get; init; } = "";

    /// <summary>Hero image URL — from TourHeroImage asset.</summary>
    public string HeroImageUrl { get; init; } = "";

    /// <summary>Hero image alt text — from TourHeroImageCaption or TourName.</summary>
    public string HeroImageAlt { get; init; } = "";

    /// <summary>Location badge on the card — from TourDestinationCity (uppercased).</summary>
    public string Badge { get; init; } = "";

    /// <summary>Days label e.g. "8 DAYS FROM" — derived from TourNights + 1.</summary>
    public string DaysFromLabel { get; init; } = "";

    /// <summary>Original / strikethrough price e.g. "10,250".</summary>
    public string OriginalPrice { get; init; } = "";

    /// <summary>Current / sale price e.g. "$4,789".</summary>
    public string CurrentPrice { get; init; } = "";

    /// <summary>Currency code — from TourPriceCurrency. Defaults to "AUD".</summary>
    public string Currency { get; init; } = "AUD";

    /// <summary>Price type label e.g. "Per person twin share" — from TourPriceTypeLabel.</summary>
    public string PriceTypeLabel { get; init; } = "";

    /// <summary>
    /// Inclusion icons parsed from TourPriceInclusions (semicolon-separated).
    /// Each value maps to a Font Awesome icon class.
    /// </summary>
    public IReadOnlyList<InclusionIcon> InclusionIcons { get; init; } = [];

    // ── Static helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Parses the semicolon-separated TourPriceInclusions field value into
    /// a list of <see cref="InclusionIcon"/> using Font Awesome icon classes.
    /// Values: Flights, Accommodation, Meals, Transfer
    /// </summary>
    public static IReadOnlyList<InclusionIcon> ParseInclusions(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return [];

        return raw
              .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => value.Trim().ToLowerInvariant() switch
            {
                "flights" => new InclusionIcon("Flights", "fa-solid fa-plane"),
                "accommodation" => new InclusionIcon("Accommodation", "fa-solid fa-bed"),
                "meals" => new InclusionIcon("Meals", "fa-solid fa-utensils"),
                "transfer" => new InclusionIcon("Transfer", "fa-solid fa-bus-simple"),
                _ => null
            })
            .Where(i => i is not null)
            .Select(i => i!)
            .ToList();
    }
}

/// <summary>
/// A single price-inclusion icon rendered with Font Awesome.
/// </summary>
public class InclusionIcon(string label, string faClass)
{
    /// <summary>Tooltip / accessible label e.g. "Flights".</summary>
    public string Label { get; } = label;

    /// <summary>Font Awesome CSS classes e.g. "fa-solid fa-plane".</summary>
    public string FaClass { get; } = faClass;
}

/// <summary>
/// View model passed to Partial/Tour/_TourListing.cshtml.
/// </summary>
public class TourListingViewModel
{
    public string Heading { get; init; } = "";
    public string? SectionId { get; init; }
    public IReadOnlyList<TourSummaryViewModel> Tours { get; init; } = [];
    public bool HideViewAllButton { get; init; }
    public string ViewAllUrl { get; init; } = "#";
    public string ViewAllButtonLabel { get; init; } = "View All";
}