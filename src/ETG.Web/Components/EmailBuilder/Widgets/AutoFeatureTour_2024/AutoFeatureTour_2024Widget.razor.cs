using CMS.Base;
using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Content;
using ETG.Core.Models.Tour;
using ETG.Web.Components.EmailBuilder.Widgets.AutoFeatureTour_2024;
using ETG.Web.Features.TourPackage.Operations;
using ETG.Web.Services.Tour;
using Kentico.Content.Web.Mvc;
using Kentico.EmailBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using MediatR;
using Microsoft.AspNetCore.Components;
using System.Globalization;
using TourContent = ETG.Core.Content.Tour;

[assembly: RegisterEmailWidget(
    identifier: AutoFeatureTour_2024Widget.IDENTIFIER,
    name: AutoFeatureTour_2024Widget.NAME,
    componentType: typeof(AutoFeatureTour_2024Widget),
    PropertiesType = typeof(AutoFeatureTour_2024WidgetProperties),
    IconClass = "icon-cogwheel-square",
    Description = "Auto Feature Tour (2024) widget.")]

namespace ETG.Web.Components.EmailBuilder.Widgets.AutoFeatureTour_2024;

public partial class AutoFeatureTour_2024Widget : ComponentBase
{
    public const string IDENTIFIER = "ETG.Web.EmailWidget.AutoFeatureTour_2024";
    public const string NAME = "Auto Feature Tour (2024)";

    private static readonly Uri PublicSiteUri =
        new("https://entiretravel.com.au/");

    private int loadVersion;

    [Inject]
    private IContentRetriever ContentRetriever { get; set; } = default!;

    [Inject]
    private IContentQueryExecutor Executor { get; set; } = default!;

    [Inject]
    private IWebPageUrlRetriever UrlRetriever { get; set; } = default!;

    [Inject]
    private ITourSummaryService TourSummaryService { get; set; } = default!;

    [Inject]
    private IMediator Mediator { get; set; } = default!;

    [Parameter]
    public AutoFeatureTour_2024WidgetProperties Properties { get; set; } = new();

    protected AutoFeatureTour_2024WidgetModel Model { get; set; } = new();

    public override Task SetParametersAsync(ParameterView parameters) =>
        ContextUtils.PropagateCurrent(
            async () => await base.SetParametersAsync(parameters))();

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        var currentLoadVersion = ++loadVersion;

        var model = new AutoFeatureTour_2024WidgetModel
        {
            TourCode = Properties?.TourCode?.Trim() ?? string.Empty,
            IsFeature = Properties?.IsFeature ?? false,
            CTALabel = Properties?.CTALabel ?? string.Empty,
            SavingsMessage = Properties?.SavingsMessage ?? string.Empty,
            TimerUrl = ToAbsoluteUrl(Properties?.TimerUrl)
        };

        Model = model;

        if (string.IsNullOrWhiteSpace(model.TourCode))
        {
            return;
        }

        await LoadTourAsync(model, currentLoadVersion);
    }

    private async Task LoadTourAsync(
        AutoFeatureTour_2024WidgetModel model,
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

        var pageUrl = await UrlRetriever.Retrieve(tour);

        var summary = await TourSummaryService.MapToViewModelAsync(
            tour,
            ToAbsoluteUrl(pageUrl?.RelativePath),
            null,
            countryName);

        if (summary is null || currentLoadVersion != loadVersion)
        {
            return;
        }

        // Preserve the original asset URL for Imgixify in the Razor view.
        model.ImageUrl = summary.HeroImageUrl ?? string.Empty;

        model.CountryName = countryName.Trim().ToUpperInvariant();

        model.ImageAlt = !string.IsNullOrWhiteSpace(summary.HeroImageAlt)
            ? summary.HeroImageAlt
            : summary.Name ?? string.Empty;

        model.DepartureCity = tour.TourDepartureCity ?? string.Empty;
        model.DestinationCity = tour.TourDestinationCity ?? string.Empty;

        if (string.IsNullOrWhiteSpace(model.SavingsMessage) &&
            decimal.TryParse(
                summary.OriginalPrice,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var originalPrice) &&
            decimal.TryParse(
                summary.CurrentPrice,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var currentPrice) &&
            originalPrice > currentPrice)
        {
            model.SavingsMessage =
                $"SAVE {summary.CurrencySymbol}" +
                (originalPrice - currentPrice)
                    .ToString("N0", CultureInfo.InvariantCulture);
        }

        // Reuse the same promotion eligibility query as the tour page.
        var promotion = await Mediator.Send(
            new GetActiveTourPromotionQuery(tour.TourCode));

        if (currentLoadVersion != loadVersion)
        {
            return;
        }

        model.PromotionName = promotion?.PromotionName ?? string.Empty;
        model.Tour = summary;
    }

    private static string ToAbsoluteUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return Uri.TryCreate(PublicSiteUri, value.Trim(), out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttps ||
                uri.Scheme == Uri.UriSchemeHttp)
            ? uri.AbsoluteUri
            : string.Empty;
    }
}

public class AutoFeatureTour_2024WidgetProperties : IEmailWidgetProperties
{
    [TextInputComponent(Label = "Tour Code", Order = 1)]
    public string TourCode { get; set; } = string.Empty;

    [CheckBoxComponent(Label = "Is Featured", Order = 2)]
    public bool IsFeature { get; set; }

    [TextInputComponent(Label = "CTA Label", Order = 3)]
    public string CTALabel { get; set; } = string.Empty;

    [TextInputComponent(Label = "Savings Message (Override)", Order = 4)]
    public string SavingsMessage { get; set; } = string.Empty;

    [TextInputComponent(Label = "Timer Url", Order = 5)]
    public string TimerUrl { get; set; } = string.Empty;
}

public class AutoFeatureTour_2024WidgetModel
{
    public string TourCode { get; set; } = string.Empty;
    public bool IsFeature { get; set; }
    public string CTALabel { get; set; } = string.Empty;
    public string SavingsMessage { get; set; } = string.Empty;
    public string TimerUrl { get; set; } = string.Empty;

    public TourSummaryViewModel? Tour { get; set; }

    public string ImageUrl { get; set; } = string.Empty;
    public string ImageAlt { get; set; } = string.Empty;
    public string CountryName { get; set; } = string.Empty;
    public string DepartureCity { get; set; } = string.Empty;
    public string DestinationCity { get; set; } = string.Empty;

    public string PromotionName { get; set; } = string.Empty;
}