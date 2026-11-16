using ETG.Core.Models.GoogleReviews;

namespace ETG.Core.Models;

public sealed class CampaignStickyBarViewModel
{
    public bool DisplaySticky { get; init; }
    public string CampaignDetail1 { get; init; } = string.Empty;
    public string CampaignDetail2 { get; init; } = string.Empty;
    public string CampaignDetail3 { get; init; } = string.Empty;
    public string CountdownLabel { get; init; } = string.Empty;
    public string EndOfCampaign { get; init; } = string.Empty;

    public ContactUsViewModel? CTA { get; init; } = new();
    public ReviewListingResponse? Reviews { get; init; }
}