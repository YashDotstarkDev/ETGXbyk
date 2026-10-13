using CMS.Websites;
using ETG.Web.Rendering;
using Kentico.Content.Web.Mvc;

using ContainerContent = ETG.Core.Content.Container;
using MediaFile = ETG.Core.Content.MediaFile;

namespace ETG.Web.Features.CampaignPage;

public sealed class CampaignPageViewModel
{
    public string PageName { get; init; } = string.Empty;
    public string BannerLabel { get; init; } = string.Empty;

    public CampaignPageHeroViewModel PageHero { get; init; } = new();

    public IEnumerable<ETG.Web.Features.ArticleViewModel> RelatedArticles
    { get; init; } = [];

    public static async Task<CampaignPageViewModel> CreateAsync(
        ContainerContent page,
        IWebPageUrlRetriever urlRetriever)
    {
        var pageName = page.ContainerName ?? string.Empty;

        var heading = string.IsNullOrWhiteSpace(page.ContainerHeading)
            ? pageName
            : page.ContainerHeading;

        var bannerName = string.IsNullOrWhiteSpace(pageName)
            ? "Campaign"
            : pageName;

        var media = page.ContainerHeroImage?
            .OfType<MediaFile>()
            .FirstOrDefault();

        var contactPage = page.ContainerHeroContactUsCtaPath?
            .OfType<IWebPageFieldsSource>()
            .FirstOrDefault();

        var contactUrl = string.Empty;

        if (contactPage is not null)
        {
            var url = await urlRetriever.Retrieve(contactPage);
            contactUrl = url.RelativePath;
        }

        return new CampaignPageViewModel
        {
            PageName = pageName,
            BannerLabel = $"{bannerName} hero banner",

            PageHero = new CampaignPageHeroViewModel
            {
                Heading = heading,

                HeadingSummary =
                    page.ContainerHeadingText ?? string.Empty,

                HeroImage = media is null
                    ? null
                    : ImageAssetViewModel.Create(media),

                HeroImageAltText =
                    page.ContainerHeroImageAltTag ?? string.Empty,

                HeroCaption =
                    page.ContainerHeroAccreditation ?? string.Empty,

                HeroContactUsCtaPath = contactUrl,

                HeroPhone =
                    page.ContainerHeroPhone ?? string.Empty
            }
        };
    }
}

public sealed class CampaignPageHeroViewModel
{
    public string Heading { get; init; } = string.Empty;
    public string HeadingSummary { get; init; } = string.Empty;

    public ImageAssetViewModel? HeroImage { get; init; }

    public string HeroImageAltText { get; init; } = string.Empty;
    public string HeroCaption { get; init; } = string.Empty;
    public string HeroContactUsCtaPath { get; init; } = string.Empty;
    public string HeroPhone { get; init; } = string.Empty;
    public bool DisableOverlay { get; init; }
}