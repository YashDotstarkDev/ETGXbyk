using CMS.ContentEngine;
using CMS.Websites;
using CSharpFunctionalExtensions;
using ETG.Core.Components;
using ETG.Core.Content;
using ETG.Web.Components.PageBuilder.Widgets.SmallTiles;
using ETG.Web.Rendering;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Websites.FormAnnotations;
using Microsoft.AspNetCore.Mvc;
using XperienceCommunity.KenticoComponentIcons;

[assembly: RegisterWidget(
    identifier: SmallTilesWidget.IDENTIFIER,
    viewComponentType: typeof(SmallTilesWidget),
    name: SmallTilesWidget.NAME,
    propertiesType: typeof(SmallTilesWidgetProperties),
    Description = "Displays small or large destination tiles.",
    IconClass = KenticoIcons.BOXES,
    AllowCache = true)]

namespace ETG.Web.Components.PageBuilder.Widgets.SmallTiles;

public class SmallTilesWidget(
    IContentRetriever contentRetriever,
    IWebPageUrlRetriever urlRetriever,
    IPreferredLanguageRetriever preferredLanguageRetriever) : ViewComponent
{
    public const string IDENTIFIER = "ETG.Web.Widget.SmallTiles";
    public const string NAME = "Small Tiles";

    private readonly IContentRetriever contentRetriever = contentRetriever;
    private readonly IWebPageUrlRetriever urlRetriever = urlRetriever;

    public async Task<IViewComponentResult> InvokeAsync(
        ComponentViewModel<SmallTilesWidgetProperties> cvm)
    {
        var props = cvm.Properties;

        var folderGuids = props.Path?
            .Where(item => item.Identifier != Guid.Empty)
            .Select(item => item.Identifier)
            .ToArray() ?? [];

        if (folderGuids.Length == 0)
        {
            return View(
                "~/Components/ComponentError.cshtml",
                new ComponentErrorViewModel(
                    NAME,
                    ComponentType.Widget,
                    "No tiles have been selected."));
        }

        var folders = await contentRetriever

       .RetrieveContentByGuids<ImageTileCtaFolder>(

           folderGuids,

           new RetrieveContentParameters
           {

               LinkedItemsMaxLevel = 2

           });
        var retrievedTiles = folders
            .SelectMany(folder => folder.SelectedImageTileCtas ?? [])
            .OfType<ImageTileCta>()
            .ToList();

        var tileTasks = retrievedTiles
            .Where(tile => tile.Image?.Any() == true)
            .Select(async tile => new SmallTileViewModel(
                !string.IsNullOrWhiteSpace(tile.Caption) ? tile.Caption : (tile.DocumentName ?? string.Empty),
                tile.SubCaption ?? string.Empty,
                GetImageUrl(tile),
                await GetTileUrlAsync(tile),
                tile.CtaLabel ?? string.Empty));

        var tiles = (await Task.WhenAll(tileTasks))
            .Where(t => !string.IsNullOrWhiteSpace(t.ImageUrl))
            .ToList();
        var ctaButtonUrl = await GetPageUrlAsync(props.CtaButtonUrl);

        if (tiles.Count == 0)
        {
            return View(
                "~/Components/ComponentError.cshtml",
                new ComponentErrorViewModel(
                    NAME,
                    ComponentType.Widget,
                    "Selected tiles no longer exist or do not contain an image."));
        }

        return View(
            "~/Components/PageBuilder/Widgets/SmallTilesWidget/SmallTiles.cshtml",
            new SmallTilesWidgetViewModel(props, tiles, ctaButtonUrl));
    }

    // Resolves Folder WebPage to its real URL path
    private async Task<string> GetTileUrlAsync(ImageTileCta tile)
    {
        var targetPage = tile.CtaUrl?
            .OfType<IWebPageFieldsSource>()
            .FirstOrDefault();

        if (targetPage is not null)
        {
            var pageUrl = await urlRetriever.Retrieve(targetPage);
            return pageUrl?.RelativePath ?? "#";
        }

        return "#";
    }
    // Resolves the page chosen in the page selector to its URL; empty if none/unpublished
    private async Task<string> GetPageUrlAsync(IEnumerable<WebPageRelatedItem> pages)
    {
        var page = pages?.FirstOrDefault();
        if (page is null)
        {
            return string.Empty;
        }

        try
        {
            var pageUrl = await urlRetriever.Retrieve(
                page.WebPageGuid,
                preferredLanguageRetriever.Get());

            return Url.Content(pageUrl.RelativePath);
        }
        catch
        {
            // Page deleted, unpublished, or without a URL in this language
            return string.Empty;
        }
    }

    // Image is a linked item. Finds its asset field dynamically
    private static string GetImageUrl(ImageTileCta tile)
    {
        var mediaItem = tile.Image?.FirstOrDefault();
        if (mediaItem is null)
        {
            return string.Empty;
        }

        var asset = mediaItem.GetType()
            .GetProperties()
            .Where(p => p.PropertyType == typeof(ContentItemAsset))
            .Select(p => p.GetValue(mediaItem) as ContentItemAsset)
            .FirstOrDefault(a => a is not null);

        return asset?.Url ?? string.Empty;
    }
}

public enum TileThemeOptions
{
    SmallCards,
    LargeCards
}

[FormCategory(Label = "Content", Order = 1)]
public class SmallTilesWidgetProperties : BaseWidgetProperties
{
    [TextInputComponent(Label = "Heading", Order = 1)]
    public string Heading { get; set; } = string.Empty;

    [ContentItemSelectorComponent(
        ImageTileCtaFolder.CONTENT_TYPE_NAME,
        Label = "Path",
        ExplanationText = "Select Image Tiles",
        MinimumItems = 1,
        MaximumItems = 50,
        DefaultViewMode = Kentico.Xperience.Admin.Base.Forms.ContentItemSelectorViewMode.Grid,
        AllowContentItemCreation = true,
        Order = 2)]
    public IEnumerable<ContentItemReference> Path { get; set; } = [];

    [DropDownComponent(
        Label = "Theme",
        ExplanationText = "Select layout style (Small Cards is default).",
        DataProviderType = typeof(EnumDropDownOptionsProvider<TileThemeOptions>),
        Order = 3)]
    public string Theme { get; set; } = nameof(TileThemeOptions.SmallCards);

    public TileThemeOptions ThemeParsed =>
        EnumDropDownOptionsProvider<TileThemeOptions>
            .Parse(Theme, TileThemeOptions.SmallCards);

    [TextInputComponent(
        Label = "Section ID",
        ExplanationText = "Optional HTML anchor ID.",
        Order = 4)]
    public string SectionId { get; set; } = string.Empty;

    [TextAreaComponent(
    Label = "Short Description",
    ExplanationText = "Optional text shown under the heading.",
    Order = 5)]
    public string ShortDescription { get; set; } = string.Empty;

    [TextInputComponent(
        Label = "CTA Button Text",
        ExplanationText = "Text on the button. Defaults to View All.",
        Order = 6)]
    public string CtaButtonText { get; set; } = "View All";

    [WebPageSelectorComponent(
        Label = "CTA Button URL",
        ExplanationText = "Optional. Select the page the button links to. If empty, the button expands the remaining tiles.",
        MaximumPages = 1,
        Order = 7)]
    public IEnumerable<WebPageRelatedItem> CtaButtonUrl { get; set; } = [];
}

public class SmallTileViewModel
{
    public string Caption { get; }
    public string SubCaption { get; }
    public string ImageUrl { get; }
    public string CtaUrl { get; }
    public string CtaLabel { get; }

    public SmallTileViewModel(
        string caption,
        string subCaption,
        string imageUrl,
        string ctaUrl,
        string ctaLabel)
    {
        Caption = caption;
        SubCaption = subCaption;
        ImageUrl = imageUrl;
        CtaUrl = ctaUrl;
        CtaLabel = ctaLabel;
    }
}

public class SmallTilesWidgetViewModel : BaseWidgetViewModel
{
    protected override string WidgetName => SmallTilesWidget.NAME;

    public string Heading { get; }
    public string SectionId { get; }
    public TileThemeOptions Theme { get; }
    public IReadOnlyList<SmallTileViewModel> Tiles { get; }
    public string ShortDescription { get; }
    public string CtaButtonText { get; }
    public string CtaButtonUrl { get; }

    public SmallTilesWidgetViewModel(
        SmallTilesWidgetProperties props,
        IReadOnlyList<SmallTileViewModel> tiles,
        string ctaButtonUrl)
    {
        Heading = props.Heading;
        SectionId = props.SectionId;
        Theme = props.ThemeParsed;
        Tiles = tiles;
        ShortDescription = props.ShortDescription;
        CtaButtonText = string.IsNullOrWhiteSpace(props.CtaButtonText)
            ? "View All"
            : props.CtaButtonText;
        CtaButtonUrl = ctaButtonUrl;
    }
}
