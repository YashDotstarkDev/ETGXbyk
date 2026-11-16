using CMS.Base;
using CMS.ContentEngine;
using CMS.Websites;
using ETG;
using ETG.Core.Content;
using ETG.Core.Models.Tour;
using ETG.Web.Components.EmailBuilder.Widgets.AutoFeatureTourWithGallery;
using ETG.Web.Services.Tour;
using Kentico.Content.Web.Mvc;
using Kentico.EmailBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Microsoft.AspNetCore.Components;
using TourContent = ETG.Core.Content.Tour;

[assembly: RegisterEmailWidget(
    identifier: AutoFeatureTourWithGalleryWidget.IDENTIFIER,
    name: AutoFeatureTourWithGalleryWidget.NAME,
    componentType: typeof(AutoFeatureTourWithGalleryWidget),
    PropertiesType = typeof(AutoFeatureTourWithGalleryWidgetProperties),
    IconClass = "icon-cogwheel-square",
    Description = "Auto Feature Tour With Gallery widget.")]

namespace ETG.Web.Components.EmailBuilder.Widgets.AutoFeatureTourWithGallery;

public partial class AutoFeatureTourWithGalleryWidget : ComponentBase
{
    public const string IDENTIFIER = "ETG.Web.EmailWidget.AutoFeatureTourWithGallery";
    public const string NAME = "Auto Feature Tour With Gallery";

    private static readonly Uri PublicSiteUri =
        new("https://entiretravel.com.au/");

    private int loadVersion;

    [Inject]
    private IContentRetriever ContentRetriever { get; set; } = default!;

    [Inject]
    private IContentQueryExecutor Executor { get; set; } = default!;

    [Inject]
    private ITourSummaryService TourSummaryService { get; set; } = default!;

    [Parameter]
    public AutoFeatureTourWithGalleryWidgetProperties Properties { get; set; } = new();

    protected AutoFeatureTourWithGalleryWidgetModel Model { get; set; } = new();

    public override Task SetParametersAsync(ParameterView parameters) =>
        ContextUtils.PropagateCurrent(
            async () => await base.SetParametersAsync(parameters))();

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        var currentLoadVersion = ++loadVersion;

        var model = new AutoFeatureTourWithGalleryWidgetModel
        {
            TourCode = Properties?.TourCode?.Trim() ?? string.Empty,
            heading = Properties?.heading?.Trim() ?? string.Empty,
            ctaCopy = Properties?.ctaCopy?.Trim() ?? string.Empty
        };

        Model = model;

        if (string.IsNullOrWhiteSpace(model.TourCode))
        {
            return;
        }

        await LoadTourAsync(model, currentLoadVersion);
    }

    private async Task LoadTourAsync(
        AutoFeatureTourWithGalleryWidgetModel model,
        int currentLoadVersion)
    {
        var query = new ContentItemQueryBuilder()
            .ForContentType(
                TourContent.CONTENT_TYPE_NAME,
                config => config
                    .ForWebsite("ETG")
                    .WithLinkedItems(4)
                    .Where(where => where.WhereEquals(
                        nameof(TourContent.TourCode),
                        model.TourCode)));

        var tours = await Executor
            .GetMappedWebPageResult<TourContent>(query);

        var tour = tours?.FirstOrDefault();

        if (tour is null || currentLoadVersion != loadVersion)
        {
            return;
        }

        var enquiryUrl = new Uri(
            PublicSiteUri,
            $"enquire/{Uri.EscapeDataString(tour.TourCode ?? model.TourCode)}")
            .AbsoluteUri;

        // Resolve Destination / Country Name
        var countryName = tour.TourPrimaryCountryOverride ?? string.Empty;

        if (string.IsNullOrWhiteSpace(countryName))
        {
            var countryGuid = tour.TourPrimaryCountry?
                .FirstOrDefault(item =>
                    item is not null &&
                    item.WebPageGuid != Guid.Empty)?
                .WebPageGuid ?? Guid.Empty;

            if (countryGuid != Guid.Empty)
            {
                var destinations = await ContentRetriever
                    .RetrievePagesByGuids<Destination>([countryGuid]);

                countryName = destinations?
                    .FirstOrDefault()?
                    .DestinationName ?? string.Empty;
            }
        }

        // Reuses the tour listing's image and pricing mapping.
        var summary = await TourSummaryService.MapToViewModelAsync(
            tour,
            enquiryUrl,
            null,
            countryName);

        if (summary is null || currentLoadVersion != loadVersion)
        {
            return;
        }

        // 1. Hero / Main Image Logic:
        // Priority 1: User-selected Primary Image override
        var primaryImageGuid = Properties.PrimaryImage?
            .FirstOrDefault()?.Identifier ?? Guid.Empty;

        string primaryImageUrl = string.Empty;
        if (primaryImageGuid != Guid.Empty)
        {
            var mediaFiles = await ContentRetriever
                .RetrieveContentByGuids<MediaFile>([primaryImageGuid]);
            primaryImageUrl = NormalizeMediaUrl(mediaFiles?.FirstOrDefault()?.LegacyMediaFileAsset?.Url);
        }

        // Priority 2: Tour's Hero Image (TourHeroImage)
        string heroImageUrl = string.Empty;
        var heroMedia = tour.TourHeroImage?.OfType<MediaFile>().FirstOrDefault();
        if (heroMedia == null && tour.TourHeroImage != null)
        {
            var heroGuids = tour.TourHeroImage
                .Select(x => (x as IContentItemFieldsSource)?.SystemFields?.ContentItemGUID ?? Guid.Empty)
                .Where(g => g != Guid.Empty)
                .ToList();

            if (heroGuids.Count > 0)
            {
                var files = await ContentRetriever.RetrieveContentByGuids<MediaFile>(heroGuids);
                heroMedia = files?.FirstOrDefault();
            }
        }

        heroImageUrl = NormalizeMediaUrl(heroMedia?.LegacyMediaFileAsset?.Url);

        if (string.IsNullOrWhiteSpace(heroImageUrl))
        {
            heroImageUrl = NormalizeMediaUrl(summary.HeroImageUrl);
        }

        model.ImageUrl = !string.IsNullOrWhiteSpace(primaryImageUrl)
            ? primaryImageUrl
            : heroImageUrl;

        model.ImageAlt = !string.IsNullOrWhiteSpace(summary.HeroImageAlt)
            ? summary.HeroImageAlt
            : (tour.TourName ?? string.Empty);

        // 2. Title (Country/Destination or Tour Name)
        model.Title = !string.IsNullOrWhiteSpace(countryName)
            ? countryName
            : (tour.TourName ?? string.Empty);

        // 3. Duration Label & Pricing & Inclusions
        model.DurationLabel = tour.TourNights > 0
            ? $"{tour.TourNights + 1} days"
            : string.Empty;

        model.TourPriceInclusions = tour.TourPriceInclusions ?? string.Empty;
        model.TravelPeriod = tour.TourTravelDates ?? string.Empty;
        model.Description = tour.TourSummary ?? string.Empty;
        model.EnquiryUrl = enquiryUrl;

        // Currency: "AUD" instead of "$"
        model.Currency = !string.IsNullOrWhiteSpace(tour.TourPriceCurrency)
            ? tour.TourPriceCurrency
            : (!string.IsNullOrWhiteSpace(summary.Currency) ? summary.Currency : "AUD");

        model.CurrentPrice = summary.CurrentPrice ?? string.Empty;
        model.PriceTypeLabel = summary.PriceTypeLabel ?? string.Empty;

        // 4. Tags: Primary Experience + Travel Style
        var tags = await LoadTagsAsync(tour);
        if (currentLoadVersion != loadVersion)
        {
            return;
        }
        model.Tags = tags;

        // 5. Gallery Images (Top 3 thumbnail images from Selected Image Folder)
        var galleryImages = await LoadGalleryImagesAsync(tour);
        if (currentLoadVersion != loadVersion)
        {
            return;
        }
        model.GalleryImages = galleryImages;

        // 6. Inclusions Bullet List
        var inclusions = await LoadInclusionsAsync(tour);
        if (currentLoadVersion != loadVersion)
        {
            return;
        }
        model.Inclusions = inclusions;

        model.Tour = summary;
    }

    private async Task<IReadOnlyList<AutoFeatureTourTagModel>> LoadTagsAsync(TourContent tour)
    {
        var tags = new List<AutoFeatureTourTagModel>();

        var experienceGuids = tour.TourExperiences?
            .Where(item =>
                item is not null &&
                item.WebPageGuid != Guid.Empty)
            .Select(item => item.WebPageGuid)
            .Distinct()
            .ToArray() ?? [];

        if (experienceGuids.Length > 0)
        {
            var query = new ContentItemQueryBuilder()
                .ForContentType(
                    Experience.CONTENT_TYPE_NAME,
                    config => config
                        .ForWebsite("ETG")
                        .WithLinkedItems(2)
                        .Where(where => where.WhereIn(
                            "WebPageItemGUID",
                            experienceGuids)));

            var experiences = await Executor.GetMappedWebPageResult<Experience>(query);

            var lookup = experiences?
                .Where(exp => exp?.SystemFields is not null)
                .GroupBy(exp => exp.SystemFields.WebPageItemGUID)
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var guid in experienceGuids)
            {
                if (lookup is null || !lookup.TryGetValue(guid, out var exp) || string.IsNullOrWhiteSpace(exp.ExperienceName))
                {
                    continue;
                }

                var icon = exp.ExperienceHeadingWhiteIconImage?.OfType<MediaFile>().FirstOrDefault()
                    ?? exp.ExperienceHeadingIconImage?.OfType<MediaFile>().FirstOrDefault();

                tags.Add(new AutoFeatureTourTagModel
                {
                    Name = exp.ExperienceName.Trim(),
                    IconUrl = NormalizeMediaUrl(icon?.LegacyMediaFileAsset?.Url)
                });

                // Primary experience only on the summary bar
                break;
            }
        }

        if (tour.TourTypes != null)
        {
            foreach (var tourType in tour.TourTypes)
            {
                if (!string.IsNullOrWhiteSpace(tourType))
                {
                    tags.Add(new AutoFeatureTourTagModel
                    {
                        Name = tourType.Trim(),
                        IconUrl = string.Empty
                    });
                    break;
                }
            }
        }

        return tags;
    }

    private async Task<IReadOnlyList<string>> LoadGalleryImagesAsync(TourContent tour)
    {
        var galleryImages = new List<string>();

        // 1. First attempt: Read from already hydrated SelectedImageFolders
        if (tour.SelectedImageFolders != null)
        {
            foreach (var folder in tour.SelectedImageFolders)
            {
                if (folder?.SelectedImages != null)
                {
                    foreach (var img in folder.SelectedImages)
                    {
                        var media = img?.ImageFilePath?.OfType<MediaFile>().FirstOrDefault();
                        var url = NormalizeMediaUrl(media?.LegacyMediaFileAsset?.Url);

                        if (string.IsNullOrWhiteSpace(url) && img?.ImageFilePath != null)
                        {
                            var mediaGuids = img.ImageFilePath
                                .Select(x => (x as IContentItemFieldsSource)?.SystemFields?.ContentItemGUID ?? Guid.Empty)
                                .Where(g => g != Guid.Empty)
                                .ToList();

                            if (mediaGuids.Count > 0)
                            {
                                var files = await ContentRetriever.RetrieveContentByGuids<MediaFile>(mediaGuids);
                                url = NormalizeMediaUrl(files?.FirstOrDefault()?.LegacyMediaFileAsset?.Url);
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(url) && !galleryImages.Contains(url))
                        {
                            galleryImages.Add(url);
                            if (galleryImages.Count >= 3)
                            {
                                break;
                            }
                        }
                    }
                }
                if (galleryImages.Count >= 3)
                {
                    break;
                }
            }
        }

        // 2. Second attempt: Explicitly query ImageFolder items by GUID with linked items
        if (galleryImages.Count < 3 && tour.SelectedImageFolders != null)
        {
            var folderGuids = tour.SelectedImageFolders
                .Where(f => f?.SystemFields != null && f.SystemFields.ContentItemGUID != Guid.Empty)
                .Select(f => f.SystemFields.ContentItemGUID)
                .Distinct()
                .ToArray();

            if (folderGuids.Length > 0)
            {
                var folderQuery = new ContentItemQueryBuilder()
                    .ForContentType(ImageFolder.CONTENT_TYPE_NAME, c => c
                        .WithLinkedItems(3)
                        .Where(w => w.WhereIn(nameof(ContentItemFields.ContentItemGUID), folderGuids)));

                var folders = await Executor.GetMappedResult<ImageFolder>(folderQuery);
                foreach (var folder in folders ?? [])
                {
                    foreach (var img in folder?.SelectedImages ?? [])
                    {
                        var media = img?.ImageFilePath?.OfType<MediaFile>().FirstOrDefault();
                        var url = NormalizeMediaUrl(media?.LegacyMediaFileAsset?.Url);

                        if (string.IsNullOrWhiteSpace(url) && img?.ImageFilePath != null)
                        {
                            var mediaGuids = img.ImageFilePath
                                .Select(x => (x as IContentItemFieldsSource)?.SystemFields?.ContentItemGUID ?? Guid.Empty)
                                .Where(g => g != Guid.Empty)
                                .ToList();

                            if (mediaGuids.Count > 0)
                            {
                                var files = await ContentRetriever.RetrieveContentByGuids<MediaFile>(mediaGuids);
                                url = NormalizeMediaUrl(files?.FirstOrDefault()?.LegacyMediaFileAsset?.Url);
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(url) && !galleryImages.Contains(url))
                        {
                            galleryImages.Add(url);
                            if (galleryImages.Count >= 3)
                            {
                                break;
                            }
                        }
                    }
                    if (galleryImages.Count >= 3)
                    {
                        break;
                    }
                }
            }
        }

        return galleryImages;
    }

    private async Task<IReadOnlyList<string>> LoadInclusionsAsync(TourContent tour)
    {
        var inclusions = new List<string>();

        if (tour.SelectedInclusionFolders != null)
        {
            foreach (var folder in tour.SelectedInclusionFolders)
            {
                if (folder?.SelectedTourInclusions != null)
                {
                    foreach (var inc in folder.SelectedTourInclusions)
                    {
                        if (!string.IsNullOrWhiteSpace(inc?.TourInclusionLabel))
                        {
                            inclusions.Add(inc.TourInclusionLabel.Trim());
                        }
                    }
                }
            }
        }

        if (inclusions.Count == 0 && tour.SelectedInclusionFolders != null)
        {
            var incFolderGuids = tour.SelectedInclusionFolders
                .Where(f => f?.SystemFields != null && f.SystemFields.ContentItemGUID != Guid.Empty)
                .Select(f => f.SystemFields.ContentItemGUID)
                .Distinct()
                .ToArray();

            if (incFolderGuids.Length > 0)
            {
                var incQuery = new ContentItemQueryBuilder()
                    .ForContentType(InclusionFolder.CONTENT_TYPE_NAME, c => c
                        .WithLinkedItems(2)
                        .Where(w => w.WhereIn(nameof(ContentItemFields.ContentItemGUID), incFolderGuids)));

                var incFolders = await Executor.GetMappedResult<InclusionFolder>(incQuery);
                foreach (var folder in incFolders ?? [])
                {
                    foreach (var inc in folder?.SelectedTourInclusions ?? [])
                    {
                        if (!string.IsNullOrWhiteSpace(inc?.TourInclusionLabel))
                        {
                            inclusions.Add(inc.TourInclusionLabel.Trim());
                        }
                    }
                }
            }
        }

        return inclusions;
    }

    private static string NormalizeMediaUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var path = value.Trim();

        if (path.StartsWith("~/", StringComparison.Ordinal))
        {
            path = path[1..];
        }

        return path;
    }
}

public class AutoFeatureTourWithGalleryWidgetProperties : IEmailWidgetProperties
{
    [RequiredValidationRule]
    [TextInputComponent(Label = "Tour Code*", Order = 1)]
    public string TourCode { get; set; } = string.Empty;

    [TextInputComponent(Label = "heading", Order = 2)]
    public string heading { get; set; } = string.Empty;

    [TextInputComponent(Label = "CTA copy", Order = 3)]
    public string ctaCopy { get; set; } = string.Empty;

    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Primary Image",
        MaximumItems = 1,
        DefaultViewMode = ContentItemSelectorViewMode.Grid,
        AllowContentItemCreation = true,
        Order = 4)]
    public IEnumerable<ContentItemReference> PrimaryImage { get; set; } = [];
}

public class AutoFeatureTourWithGalleryWidgetModel
{
    public string TourCode { get; set; } = string.Empty;
    public string heading { get; set; } = string.Empty;
    public string ctaCopy { get; set; } = string.Empty;

    public TourSummaryViewModel? Tour { get; set; }

    public string ImageUrl { get; set; } = string.Empty;
    public string ImageAlt { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string DurationLabel { get; set; } = string.Empty;
    public string TourPriceInclusions { get; set; } = string.Empty;
    public string TravelPeriod { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string EnquiryUrl { get; set; } = string.Empty;

    public string Currency { get; set; } = "AUD";
    public string CurrentPrice { get; set; } = string.Empty;
    public string PriceTypeLabel { get; set; } = string.Empty;

    public IReadOnlyList<AutoFeatureTourTagModel> Tags { get; set; } = [];
    public IReadOnlyList<string> GalleryImages { get; set; } = [];
    public IReadOnlyList<string> Inclusions { get; set; } = [];
}

public class AutoFeatureTourTagModel
{
    public string Name { get; set; } = string.Empty;
    public string IconUrl { get; set; } = string.Empty;
}
