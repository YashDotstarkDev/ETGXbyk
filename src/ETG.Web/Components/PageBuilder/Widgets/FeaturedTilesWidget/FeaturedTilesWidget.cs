using CMS.ContentEngine;
using CMS.Websites;
using CSharpFunctionalExtensions;
using ETG.Core.Components;
using ETG.Core.Content;
using ETG.Web.Components.PageBuilder.Widgets.FeaturedTiles;
using ETG.Web.Rendering;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Websites.FormAnnotations;
using Microsoft.AspNetCore.Mvc;
using XperienceCommunity.KenticoComponentIcons;

[assembly: RegisterWidget(
    identifier: FeaturedTilesWidget.IDENTIFIER,
    viewComponentType: typeof(FeaturedTilesWidget),
    name: FeaturedTilesWidget.NAME,
    propertiesType: typeof(FeaturedTilesWidgetProperties),
    Description = "Displays featured image tiles with multiple theme layouts.",
    IconClass = KenticoIcons.BOXES,
    AllowCache = true)]

namespace ETG.Web.Components.PageBuilder.Widgets.FeaturedTiles;

public class FeaturedTilesWidget(
    IContentRetriever contentRetriever,
    IWebPageUrlRetriever webPageUrlRetriever,
    IPreferredLanguageRetriever preferredLanguageRetriever) : ViewComponent
{
    public const string IDENTIFIER = "ETG.Web.Widget.FeaturedTiles";
    public const string NAME = "Featured Tiles";

    public async Task<IViewComponentResult> InvokeAsync(
        ComponentViewModel<FeaturedTilesWidgetProperties> cvm)
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

        var result = await BuildViewModel(props, retrievedTiles);

        return result.Match(
            vm => View(
                "~/Components/PageBuilder/Widgets/FeaturedTilesWidget/FeaturedTiles.cshtml",
                vm),
            error => View(
                "~/Components/ComponentError.cshtml",
                error));
    }

    private async Task<Result<FeaturedTilesWidgetViewModel, ComponentErrorViewModel>> BuildViewModel(
        FeaturedTilesWidgetProperties props,
        IEnumerable<ImageTileCta> retrievedTiles)
    {
        var languageName = preferredLanguageRetriever.Get();
        var tiles = new List<ImageTileViewModel>();

        foreach (var tile in retrievedTiles)
        {
            var imageUrl = GetImageUrl(tile);

            if (string.IsNullOrEmpty(imageUrl))
            {
                continue;
            }

            var ctaUrl = await GetCtaUrl(tile, languageName);

            tiles.Add(new ImageTileViewModel(
                tile.Caption ?? string.Empty,
                tile.SubCaption ?? string.Empty,
                imageUrl,
                ctaUrl,
                tile.CtaLabel ?? string.Empty,
                tile.CardTag ?? string.Empty));
        }

        if (tiles.Count == 0)
        {
            return Result.Failure<FeaturedTilesWidgetViewModel, ComponentErrorViewModel>(
                new ComponentErrorViewModel(
                    NAME,
                    ComponentType.Widget,
                    "Selected tiles no longer exist or do not contain an image."));
        }

        return new FeaturedTilesWidgetViewModel(props, tiles);
    }

    private static string GetImageUrl(ImageTileCta tile) =>
        tile.Image?
            .OfType<MediaFile>()
            .FirstOrDefault()?
            .LegacyMediaFileAsset?.Url ?? string.Empty;
    private async Task<string> GetCtaUrl(ImageTileCta tile, string languageName)
    {
        var pageGuid = tile.CtaUrl?
            .Cast<object>()
            .Select(GetPageGuid)
            .FirstOrDefault(guid => guid.HasValue);

        if (pageGuid is null)
        {
            return string.Empty;
        }

        try
        {
            var pageUrl = await webPageUrlRetriever
                .Retrieve(pageGuid.Value, languageName);

            return Url.Content(pageUrl.RelativePath);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static Guid? GetPageGuid(object? item) => item switch
    {
        IWebPageFieldsSource page => page.SystemFields.WebPageItemGUID,
        WebPageRelatedItem related => related.WebPageGuid,
        _ => null
    };
}

[FormCategory(Label = "Content", Order = 1)]
public class FeaturedTilesWidgetProperties : BaseWidgetProperties
{
    [TextInputComponent(Label = "Heading", Order = 1)]
    public string Heading { get; set; } = string.Empty;

    [TextInputComponent(
        Label = "Description",
        ExplanationText = "Shown only when Card with Tag layout is selected.",
        Order = 2)]
    public string Description { get; set; } = string.Empty;

    [ContentItemSelectorComponent(
        ImageTileCtaFolder.CONTENT_TYPE_NAME,
        Label = "Path",
        ExplanationText = "Select Image Tiles",
        MinimumItems = 1,
        MaximumItems = 50,
        AllowContentItemCreation = true,
        Order = 3)]
    public IEnumerable<ContentItemReference> Path { get; set; } = [];

    [DropDownComponent(
        Label = "Display Layout",
        ExplanationText = "Select layout style (Large Text Card is default).",
        DataProviderType =
            typeof(EnumDropDownOptionsProvider<DisplayLayoutOptions>),
        Order = 4)]
    public string DisplayLayout { get; set; } =
        nameof(DisplayLayoutOptions.LargeTextCard);

    public DisplayLayoutOptions DisplayLayoutParsed =>
        EnumDropDownOptionsProvider<DisplayLayoutOptions>
            .Parse(DisplayLayout, DisplayLayoutOptions.LargeTextCard);

    [DropDownComponent(
        Label = "Tile Type",
        ExplanationText = "Select whether tiles act as links or static cards.",
        DataProviderType =
            typeof(EnumDropDownOptionsProvider<TileTypeOptions>),
        Order = 5)]
    public string TileType { get; set; } =
        nameof(TileTypeOptions.TilesAreLinks);

    public TileTypeOptions TileTypeParsed =>
        EnumDropDownOptionsProvider<TileTypeOptions>
            .Parse(TileType, TileTypeOptions.TilesAreLinks);

    [TextInputComponent(
        Label = "Section ID",
        ExplanationText = "Optional HTML anchor ID.",
        Order = 6)]
    public string SectionId { get; set; } = string.Empty;
}

public class ImageTileViewModel
{
    public string Caption { get; }
    public string SubCaption { get; }
    public string ImageUrl { get; }
    public string CtaUrl { get; }
    public string CtaLabel { get; }

    public string CardTag { get; }


    public bool HasLink => !string.IsNullOrWhiteSpace(CtaUrl);

    public ImageTileViewModel(
        string caption,
        string subCaption,
        string imageUrl,
        string ctaUrl,
        string ctaLabel,
        string cardTag)
    {
        Caption = caption;
        SubCaption = subCaption;
        ImageUrl = imageUrl;
        CtaUrl = ctaUrl;
        CtaLabel = ctaLabel;
        CardTag = cardTag;
    }
}

public class FeaturedTilesWidgetViewModel : BaseWidgetViewModel
{
    protected override string WidgetName => FeaturedTilesWidget.NAME;

    public string Heading { get; }
    public string Description { get; }
    public IEnumerable<ContentItemReference> Path { get; }
    public string SectionId { get; }
    public DisplayLayoutOptions DisplayLayout { get; }
    public TileTypeOptions TileType { get; }
    public IReadOnlyList<ImageTileViewModel> Tiles { get; }

    // Tiles become links only when the editor chose "Tiles are links"
    public bool TilesAreLinks => TileType == TileTypeOptions.TilesAreLinks;

    public FeaturedTilesWidgetViewModel(
        FeaturedTilesWidgetProperties props,
        IReadOnlyList<ImageTileViewModel> tiles)
    {
        Heading = props.Heading;
        Description = props.Description;
        Path = props.Path ?? [];
        SectionId = props.SectionId;
        DisplayLayout = props.DisplayLayoutParsed;
        TileType = props.TileTypeParsed;
        Tiles = tiles;
    }
}

public enum DisplayLayoutOptions
{
    LargeTextCard,
    CardWithTag,
    UnderTextCard
}

public enum TileTypeOptions
{
    TilesAreLinks,
    TilesAreNotLinks
}