using CMS.Base;
using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Websites;
using ETG.Core.Content;
using ETG.Core.Models.Tour;
using ETG.Core.Modules;
using ETG.Web.Components.EmailBuilder.Widgets.AutoFeatureTour;
using ETG.Web.Services.Tour;
using Kentico.Content.Web.Mvc;
using Kentico.EmailBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Microsoft.AspNetCore.Components;
using TourContent = ETG.Core.Content.Tour;

[assembly: RegisterEmailWidget(
    identifier: AutoFeatureTourWidget.IDENTIFIER,
    name: AutoFeatureTourWidget.NAME,
    componentType: typeof(AutoFeatureTourWidget),
    PropertiesType = typeof(AutoFeatureTourWidgetProperties),
    IconClass = "icon-cogwheel-square",
    Description = "Auto Feature Tour widget.")]

namespace ETG.Web.Components.EmailBuilder.Widgets.AutoFeatureTour;

public partial class AutoFeatureTourWidget : ComponentBase
{
    public const string IDENTIFIER = "ETG.Web.EmailWidget.AutoFeatureTour";
    public const string NAME = "Auto Feature Tour";

    // Existing enquiry-link domain; not used for image URLs.
    //private static readonly Uri PublicSiteUri =
    //    new("https://entiretravel.com.au/");

    private int loadVersion;
    [Inject]
    private IHttpContextAccessor HttpContextAccessor { get; set; } = default!;

    [Inject]
    private IContentRetriever ContentRetriever { get; set; } = default!;

    [Inject]
    private IContentQueryExecutor Executor { get; set; } = default!;

    [Inject]
    private ITourSummaryService TourSummaryService { get; set; } = default!;

    [Inject]
    private IInfoProvider<TourTypeInfo> TourTypeProvider { get; set; } = default!;

    [Parameter]
    public AutoFeatureTourWidgetProperties Properties { get; set; } = new();

    protected AutoFeatureTourWidgetModel Model { get; set; } = new();

    public override Task SetParametersAsync(ParameterView parameters) =>
        ContextUtils.PropagateCurrent(
            async () => await base.SetParametersAsync(parameters))();

    private Uri PublicSiteUri
    {
        get
        {
            var request = HttpContextAccessor.HttpContext?.Request
                ?? throw new InvalidOperationException(
                    "Cannot determine the website URL without an active HTTP request.");

            return new Uri(
                $"{request.Scheme}://{request.Host}{request.PathBase}/");
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        var currentLoadVersion = ++loadVersion;

        var model = new AutoFeatureTourWidgetModel
        {
            TourCode = Properties?.TourCode?.Trim() ?? string.Empty,
            heading = Properties?.heading ?? string.Empty,
            ctaCopy = Properties?.ctaCopy ?? string.Empty
        };

        Model = model;

        if (string.IsNullOrWhiteSpace(model.TourCode))
        {
            return;
        }

        await LoadTourAsync(model, currentLoadVersion);
    }

    private async Task LoadTourAsync(
        AutoFeatureTourWidgetModel model,
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

        var summary = await TourSummaryService.MapToViewModelAsync(
            tour,
            enquiryUrl,
            null);

        if (summary is null || currentLoadVersion != loadVersion)
        {
            return;
        }

        var experiences = await LoadExperiencesAsync(tour);
        var travelStyles = await LoadTravelStylesAsync(tour);

        if (currentLoadVersion != loadVersion)
        {
            return;
        }

        // Keep the original asset URL for Imgixify in the Razor view.
        model.ImageUrl = summary.HeroImageUrl ?? string.Empty;

        model.ImageAlt = !string.IsNullOrWhiteSpace(summary.HeroImageAlt)
            ? summary.HeroImageAlt
            : summary.Name ?? string.Empty;

        model.Nights = tour.TourNights;
        model.TravelPeriod = tour.TourTravelDates ?? string.Empty;
        model.EnquiryUrl = enquiryUrl;
        model.Experiences = experiences;
        model.TravelStyles = travelStyles;
        model.Tour = summary;
    }

    private async Task<IReadOnlyList<AutoFeatureTourExperienceModel>>
        LoadExperiencesAsync(TourContent tour)
    {
        var experienceGuids = tour.TourExperiences?
            .Where(item =>
                item is not null &&
                item.WebPageGuid != Guid.Empty)
            .Select(item => item.WebPageGuid)
            .Distinct()
            .ToArray() ?? [];

        if (experienceGuids.Length == 0)
        {
            return [];
        }

        var query = new ContentItemQueryBuilder()
            .ForContentType(
                Experience.CONTENT_TYPE_NAME,
                config => config
                    .ForWebsite("ETG")
                    .WithLinkedItems(2)
                    .Where(where => where.WhereIn(
                        "WebPageItemGUID",
                        experienceGuids)));

        var experiences = await Executor
            .GetMappedWebPageResult<Experience>(query);

        var lookup = experiences?
            .Where(experience =>
                experience?.SystemFields is not null)
            .GroupBy(experience =>
                experience.SystemFields.WebPageItemGUID)
            .ToDictionary(
                group => group.Key,
                group => group.First());

        var result = new List<AutoFeatureTourExperienceModel>();

        foreach (var guid in experienceGuids)
        {
            if (lookup is null ||
                !lookup.TryGetValue(guid, out var experience) ||
                string.IsNullOrWhiteSpace(experience.ExperienceName))
            {
                continue;
            }

            var icon = experience.ExperienceHeadingWhiteIconImage?
                .OfType<MediaFile>()
                .FirstOrDefault();

            icon ??= experience.ExperienceHeadingIconImage?
                .OfType<MediaFile>()
                .FirstOrDefault();

            result.Add(new AutoFeatureTourExperienceModel
            {
                Name = experience.ExperienceName,
                IconUrl = icon?.LegacyMediaFileAsset?.Url ?? string.Empty
            });
        }

        return result;
    }

    private async Task<IReadOnlyList<AutoFeatureTourTravelStyleModel>>
        LoadTravelStylesAsync(TourContent tour)
    {
        var codes = tour.TourTypes?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];

        if (codes.Length == 0)
        {
            return [];
        }

        var types = await TourTypeProvider
            .Get()
            .WhereIn(nameof(TourTypeInfo.TourTypeCodeName), codes)
            .GetEnumerableTypedResultAsync();

        var lookup = types
            .Where(type =>
                type is not null &&
                !string.IsNullOrWhiteSpace(type.TourTypeCodeName) &&
                !string.IsNullOrWhiteSpace(type.TourTypeName))
            .GroupBy(
                type => type.TourTypeCodeName.Trim(),
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First(),
                StringComparer.OrdinalIgnoreCase);

        var result = new List<AutoFeatureTourTravelStyleModel>();

        foreach (var code in codes)
        {
            if (!lookup.TryGetValue(code, out var type))
            {
                continue;
            }

            // Prefer the white icon for the dark background.
            var icon = type.TourTypeWhiteIconImage?
                .OfType<MediaFile>()
                .FirstOrDefault(file =>
                    !string.IsNullOrWhiteSpace(
                        file.LegacyMediaFileAsset?.Url));

            icon ??= type.TourTypeBlackIconImage?
                .OfType<MediaFile>()
                .FirstOrDefault(file =>
                    !string.IsNullOrWhiteSpace(
                        file.LegacyMediaFileAsset?.Url));

            result.Add(new AutoFeatureTourTravelStyleModel
            {
                Name = type.TourTypeName,
                IconUrl = icon?.LegacyMediaFileAsset?.Url ?? string.Empty
            });
        }

        return result;
    }
}

public class AutoFeatureTourWidgetProperties : IEmailWidgetProperties
{
    [TextInputComponent(Label = "Tour Code", Order = 1)]
    public string TourCode { get; set; } = string.Empty;

    [TextInputComponent(Label = "heading", Order = 2)]
    public string heading { get; set; } = string.Empty;

    [TextInputComponent(Label = "CTA copy", Order = 3)]
    public string ctaCopy { get; set; } = string.Empty;
}

public class AutoFeatureTourWidgetModel
{
    public string TourCode { get; set; } = string.Empty;
    public string heading { get; set; } = string.Empty;
    public string ctaCopy { get; set; } = string.Empty;

    public TourSummaryViewModel? Tour { get; set; }

    public string ImageUrl { get; set; } = string.Empty;
    public string ImageAlt { get; set; } = string.Empty;
    public int Nights { get; set; }
    public string TravelPeriod { get; set; } = string.Empty;
    public string EnquiryUrl { get; set; } = string.Empty;

    public IReadOnlyList<AutoFeatureTourExperienceModel> Experiences
    {
        get;
        set;
    } = [];

    public IReadOnlyList<AutoFeatureTourTravelStyleModel> TravelStyles
    {
        get;
        set;
    } = [];
}

public class AutoFeatureTourExperienceModel
{
    public string Name { get; set; } = string.Empty;
    public string IconUrl { get; set; } = string.Empty;
}

public class AutoFeatureTourTravelStyleModel
{
    public string Name { get; set; } = string.Empty;
    public string IconUrl { get; set; } = string.Empty;
}