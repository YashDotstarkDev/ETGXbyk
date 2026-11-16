using CMS.Base;
using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Websites;
using ETG.Core.Content;
using ETG.Core.Models.Tour;
using ETG.Core.Modules;
using ETG.Web.Components.EmailBuilder.Widgets.AutoTour;
using ETG.Web.Services.Tour;
using Kentico.Content.Web.Mvc;
using Kentico.EmailBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using TourContent = ETG.Core.Content.Tour;

[assembly: RegisterEmailWidget(
    identifier: AutoTourWidget.IDENTIFIER,
    name: AutoTourWidget.NAME,
    componentType: typeof(AutoTourWidget),
    PropertiesType = typeof(AutoTourWidgetProperties),
    IconClass = "icon-cogwheel-square",
    Description = "Auto Tour widget.")]

namespace ETG.Web.Components.EmailBuilder.Widgets.AutoTour;

public partial class AutoTourWidget : ComponentBase
{
    public const string IDENTIFIER = "ETG.Web.EmailWidget.AutoTour";
    public const string NAME = "Auto Tour";

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
    public AutoTourWidgetProperties Properties { get; set; } = new();

    protected AutoTourWidgetModel Model { get; set; } = new();

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

        var model = new AutoTourWidgetModel
        {
            TourCode = Properties?.TourCode?.Trim() ?? string.Empty
        };

        Model = model;

        if (string.IsNullOrWhiteSpace(model.TourCode))
        {
            return;
        }

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

        // Keep the original URL; transform it with Imgixify in Razor.
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

    private async Task<IReadOnlyList<AutoTourDetailModel>>
        LoadExperiencesAsync(TourContent tour)
    {
        var guids = tour.TourExperiences?
            .Where(item =>
                item is not null &&
                item.WebPageGuid != Guid.Empty)
            .Select(item => item.WebPageGuid)
            .Distinct()
            .ToArray() ?? [];

        if (guids.Length == 0)
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
                        guids)));

        var experiences = await Executor
            .GetMappedWebPageResult<Experience>(query);

        var lookup = experiences?
            .Where(item => item?.SystemFields is not null)
            .GroupBy(item => item.SystemFields.WebPageItemGUID)
            .ToDictionary(
                group => group.Key,
                group => group.First());

        var result = new List<AutoTourDetailModel>();

        foreach (var guid in guids)
        {
            if (lookup is null ||
                !lookup.TryGetValue(guid, out var experience) ||
                string.IsNullOrWhiteSpace(experience.ExperienceName))
            {
                continue;
            }

            var icon = experience.ExperienceHeadingIconImage?
                .OfType<MediaFile>()
                .FirstOrDefault(item =>
                    !string.IsNullOrWhiteSpace(
                        item.LegacyMediaFileAsset?.Url));

            result.Add(new AutoTourDetailModel
            {
                Name = experience.ExperienceName,
                IconUrl = icon?.LegacyMediaFileAsset?.Url ?? string.Empty
            });
        }

        return result;
    }

    private async Task<IReadOnlyList<AutoTourDetailModel>>
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

        var result = new List<AutoTourDetailModel>();

        foreach (var code in codes)
        {
            if (!lookup.TryGetValue(code, out var type))
            {
                continue;
            }

            // Read the collection declared in your final TourTypeInfo class.
            // It must already contain mapped MediaFile objects.
            var icon = type.TourTypeBlackIconImage?
                .OfType<MediaFile>()
                .FirstOrDefault(file =>
                    !string.IsNullOrWhiteSpace(
                        file.LegacyMediaFileAsset?.Url));

            result.Add(new AutoTourDetailModel
            {
                Name = type.TourTypeName,
                IconUrl = icon?.LegacyMediaFileAsset?.Url ?? string.Empty
            });
        }

        return result;
    }
}

public class AutoTourWidgetProperties : IEmailWidgetProperties
{
    [TextInputComponent(Label = "Tour Code", Order = 1)]
    public string TourCode { get; set; } = string.Empty;
}

public class AutoTourWidgetModel
{
    public string TourCode { get; set; } = string.Empty;

    public TourSummaryViewModel? Tour { get; set; }

    public int Nights { get; set; }
    public string TravelPeriod { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string ImageAlt { get; set; } = string.Empty;
    public string EnquiryUrl { get; set; } = string.Empty;

    public IReadOnlyList<AutoTourDetailModel> Experiences { get; set; } = [];

    public IReadOnlyList<AutoTourDetailModel> TravelStyles { get; set; } = [];
}

public class AutoTourDetailModel
{
    public string Name { get; set; } = string.Empty;
    public string IconUrl { get; set; } = string.Empty;
}