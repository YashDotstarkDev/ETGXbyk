using ETG.Web.Features;
using ETG.Web.Rendering;
using System.Collections.Generic;
using System;

namespace ETG.Web.Features.Home;

public sealed class HomePageViewModel
{
    public string HeroCaption { get; init; } = string.Empty;
    public string HeroSubCaption { get; init; } = string.Empty;
    public ImageAssetViewModel? HeroImage { get; init; }
    public ImageAssetViewModel? HeroVideoPoster { get; init; }

    public string FeaturedTilesTitle { get; init; } = string.Empty;
    public string ProofPointsTitle { get; init; } = string.Empty;
    public string DestinationsTitle { get; init; } = string.Empty;

    public string FeatureToursTitle { get; init; } = string.Empty;
    public string FeatureToursTitle2 { get; init; } = string.Empty;
    public string FeatureToursViewAllUrl { get; init; } = string.Empty;
    public string FeatureToursViewAllUrl2 { get; init; } = string.Empty;
    public string FeatureToursViewAllLabel2 { get; init; } = string.Empty;

    public string BlogsTitle { get; init; } = string.Empty;
    public string BlogsDescription { get; init; } = string.Empty;
    public string BlogsViewAllUrl { get; init; } = string.Empty;
    public IEnumerable<ArticleViewModel> RelatedArticles { get; init; } = [];

    public IReadOnlyList<Guid> SelectedExperienceGuids { get; init; } = [];
    public IReadOnlyList<HomeExperienceViewModel> Experiences { get; init; } = [];
    public string HomepageJsonSchema { get; init; } = string.Empty;
    public bool ShowCompetitionPopup { get; init; }
    public string CompetitionTitle { get; init; } = string.Empty;

    // --- Algolia Search Properties ---
    public string SearchAppId { get; init; } = string.Empty;
    public string SearchApiKey { get; init; } = string.Empty;
    public string SearchIndexName { get; init; } = string.Empty;
}

public sealed class HomeExperienceViewModel
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string SvgIcon { get; init; } = string.Empty;
}
