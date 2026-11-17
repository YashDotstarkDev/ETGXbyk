using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using CSharpFunctionalExtensions;
using ETG.Core;
using ETG.Core.Content;
using ETG.Web.Components.PageBuilder.Widgets.Products;
using ETG.Web.Services.Tour;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using XperienceCommunity.KenticoComponentIcons;
using TourContent = ETG.Core.Content.Tour;
using System.Globalization;
using ETG.Core.Models.Tour;

[assembly: RegisterWidget(
    identifier: ProductsWidget.IDENTIFIER,
    name: ProductsWidget.NAME,
    viewComponentType: typeof(ProductsWidget),
    propertiesType: typeof(ProductsWidgetProperties),
    Description = "Displays a grid of tour/package cards sourced from ETG.Tour content.",
    IconClass = KenticoIcons.COLLAPSE_SCHEME,
    AllowCache = true)]

namespace ETG.Web.Components.PageBuilder.Widgets.Products;

public class ProductsWidget(
    IContentQueryExecutor executor,
    IWebPageUrlRetriever urlRetriever,
    IWebsiteChannelContext channelContext,
    ITourPricingService pricingService,
    IContentQueryExecutionOptionsCreator queryOptionsCreator, IContentRetriever contentRetriever) : ViewComponent
{
    public const string IDENTIFIER = "ETG.Web.Widget.ProductsWidget";
    public const string NAME = "Products Widget";

    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<ProductsWidgetProperties> cvm)
    {
        var props = cvm?.Properties;
        if (props is null)
            return Error("No properties have been set for this widget.");
        var codes = props.PackageCodes;
        if (codes.Count == 0)
            return Error("No package codes have been entered. Add comma-separated TourCodes in the widget properties.");

        var query = new ContentItemQueryBuilder()
            .ForContentType(
                TourContent.CONTENT_TYPE_NAME,
                q => q
                    .ForWebsite(channelContext.WebsiteChannelName)
                    .WithLinkedItems(4)
                    .Where(w => w.WhereIn(nameof(TourContent.TourCode), codes.ToArray())));

        var tours = await executor.GetMappedWebPageResult<TourContent>(query, queryOptionsCreator.Create());

        if (!tours.Any())
            return Error("No published tours were found for the entered codes. Verify TourCode values match published ETG.Tour pages.");

        var ordered = codes
                     .Select(code => tours.FirstOrDefault(t =>
                         string.Equals(t.TourCode, code, StringComparison.OrdinalIgnoreCase)))
                     .OfType<TourContent>()
                     .ToList();

        var bulkPricesTask = pricingService.GetPricesBulkAsync(ordered);

        var countryGuids = ordered
            .Select(t => t.TourPrimaryCountry?.FirstOrDefault()?.WebPageGuid)
            .Where(g => g.HasValue && g.Value != Guid.Empty)
            .Select(g => g!.Value)
            .Distinct()
            .ToArray();

        var countryTask = countryGuids.Length > 0
            ? contentRetriever.RetrievePagesByGuids<Destination>(countryGuids)
            : Task.FromResult<IEnumerable<Destination>>([]);

        var urlTasks = Task.WhenAll(ordered.Select(async tour =>
        {
            var url = await urlRetriever.Retrieve(tour!);
            return new { Tour = tour, Path = url?.RelativePath ?? string.Empty };
        }));

        await Task.WhenAll(bulkPricesTask, countryTask, urlTasks);

        var bulkPrices = await bulkPricesTask;
        var countries = await countryTask;
        var destinationLookup = countries.ToDictionary(
            d => d.SystemFields.WebPageItemGUID,
            d => d.DestinationName ?? string.Empty);
        var tourUrls = await urlTasks;

        var tourItems = tourUrls.Select(item =>
        {
            var priceInfo = bulkPrices.TryGetValue(item.Tour.TourCode, out var p)
                            ? p
                            : new TourPricingResult(0, 0);

            var countryGuid = item.Tour.TourPrimaryCountry?.FirstOrDefault()?.WebPageGuid ?? Guid.Empty;
            var countryName = destinationLookup.TryGetValue(countryGuid, out var name) ? name : string.Empty;

            return MapToViewModel(item.Tour, item.Path, priceInfo, countryName);
        }).ToList();

        var tourListing = new TourListingViewModel
        {
            Heading = props.Heading,
            SectionId = props.SectionId,
            HideViewAllButton = props.HideViewAll,
            ViewAllUrl = props.ViewAllUrl,
            Tours = tourItems
        };

        return View("~/Components/PageBuilder/Widgets/Products/Products.cshtml", new ProductsWidgetViewModel(props, tourListing));
    }

    private static TourSummaryViewModel MapToViewModel(TourContent tour, string path, TourPricingResult priceInfo, string primaryCountryName)
    {
        var heroImage = tour.TourHeroImage?.OfType<MediaFile>().FirstOrDefault();
        
        if (heroImage == null)
        {
            heroImage = tour.SelectedImageFolders?
                .SelectMany(folder => folder.SelectedImages ?? [])
                .SelectMany(image => image.ImageFilePath ?? [])
                .OfType<MediaFile>()
                .FirstOrDefault();
        }

        var badge = !string.IsNullOrWhiteSpace(primaryCountryName)
                    ? primaryCountryName.ToUpperInvariant()
                    : !string.IsNullOrWhiteSpace(tour.TourDestinationCity)
                    ? tour.TourDestinationCity.ToUpperInvariant()
                    : !string.IsNullOrWhiteSpace(tour.TourDepartureCity)
                    ? tour.TourDepartureCity.ToUpperInvariant()
                    : string.Empty;

        var daysLabel = tour.TourNights > 0
            ? $"{tour.TourNights + 1} DAYS"
            : string.Empty;

        var originalPriceText = priceInfo.OriginalPrice > 0
                                ? priceInfo.OriginalPrice.ToString("N0", CultureInfo.InvariantCulture)
                                : string.Empty;

        var currentPriceText = priceInfo.CurrentPrice > 0
            ? priceInfo.CurrentPrice.ToString("N0", CultureInfo.InvariantCulture)
            : string.Empty;

        var displayOriginalPrice = string.Empty;

        if (priceInfo.CurrentPrice > 0 &&
            priceInfo.OriginalPrice > 0 &&
            priceInfo.CurrentPrice < priceInfo.OriginalPrice)
        {
            displayOriginalPrice = originalPriceText;
        }


        return new TourSummaryViewModel
        {
            Name = tour.TourName ?? "",
            TourCode = tour.TourCode ?? "",
            Badge = badge,
            Path = path,
            Description = tour.TourSummary ?? "",
            HeroImageUrl = heroImage?.LegacyMediaFileAsset?.Url ?? "",
            HeroImageAlt = heroImage?.LegacyMediaFileTitle ?? "",
            DaysFromLabel = daysLabel,
            OriginalPrice = displayOriginalPrice,
            CurrentPrice = currentPriceText ?? "",
            CurrencySymbol = priceInfo.CurrencySymbol,
            Currency = priceInfo.CurrencyCode,
            PriceTypeLabel = tour.TourPriceTypeLabel ?? "",
            InclusionIcons = TourSummaryViewModel.ParseInclusions(tour.TourPriceInclusions)
        };
    }

    private IViewComponentResult Error(string message) =>
        View("~/Components/ComponentError.cshtml", new ComponentErrorViewModel(NAME, ComponentType.Widget, message));
}

[FormCategory(Label = "Content", Order = 1)]
public class ProductsWidgetProperties : BaseWidgetProperties
{
    [TextInputComponent(Label = "Heading", Order = 1)]
    public string Heading { get; set; } = "";

    [TextInputComponent(
        Label = "Package codes",
        ExplanationText = "Comma-separated TourCode values matching ETG.Tour pages",
        Order = 2)]
    public string TourCodes { get; set; } = "";

    [TextInputComponent(Label = "View all URL", Order = 3)]
    public string ViewAllUrl { get; set; } = "";

    [CheckBoxComponent(Label = "Hide View All Button", Order = 4)]
    public bool HideViewAll { get; set; }

    [TextInputComponent(Label = "Section ID", Order = 5)]
    public string SectionId { get; set; } = "";

    public IReadOnlyList<string> PackageCodes =>
        string.IsNullOrWhiteSpace(TourCodes)
            ? []
            : TourCodes
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .ToList();
}

public enum ProductTourType
{
    [Description("Package Tour")] PackageTour,
    [Description("Small group")] smallgrouptours,
    [Description("Escorted group")] escortedgrouptours,
    [Description("Private tours")] privatetours,
    [Description("Independent")] independenttours,
    [Description("Day tours")] daytours
}

public enum ProductCruiseType
{
    [Description("Self-Drive")] SelfDrive = 1,
    [Description("Barge")] Barge = 2,
    [Description("River")] River = 3,
    [Description("Ocean")] Ocean = 4
}

public class ProductsWidgetViewModel : BaseWidgetViewModel
{
    protected override string WidgetName { get; } = ProductsWidget.NAME;
    public string Heading { get; }
    public string TourCodes { get; }
    public string ViewAllUrl { get; }
    public bool HideViewAll { get; }
    public string SectionId { get; }
    public IReadOnlyList<string> PackageCodes { get; }
    public TourListingViewModel Tour { get; }

    public ProductsWidgetViewModel(ProductsWidgetProperties props, TourListingViewModel? tourListing = null)
    {
        Heading = props.Heading;
        TourCodes = props.TourCodes;
        ViewAllUrl = props.ViewAllUrl;
        HideViewAll = props.HideViewAll;
        SectionId = props.SectionId;
        PackageCodes = props.PackageCodes;
        Tour = tourListing ?? new TourListingViewModel
        {
            Heading = Heading,
            SectionId = SectionId,
            HideViewAllButton = HideViewAll,
            ViewAllUrl = ViewAllUrl,
            Tours = []
        };
    }
}
