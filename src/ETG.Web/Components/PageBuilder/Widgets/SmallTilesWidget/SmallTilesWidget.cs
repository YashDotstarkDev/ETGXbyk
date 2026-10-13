using CMS.ContentEngine;
using CSharpFunctionalExtensions;
using ETG.Core.Components;
using ETG.Core.Content;
using ETG.Web.Components.PageBuilder.Widgets.SmallTiles;
using ETG.Web.Rendering;
using Kentico.Content.Web.Mvc;
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

public class SmallTilesWidget(IContentRetriever contentRetriever) : ViewComponent
{
    public const string IDENTIFIER = "ETG.Web.Widget.SmallTiles";
    public const string NAME = "Small Tiles";

    private readonly IContentRetriever contentRetriever = contentRetriever;

    public async Task<IViewComponentResult> InvokeAsync(
        ComponentViewModel<SmallTilesWidgetProperties> cvm)
    {
        var props = cvm.Properties;

        var imageGuids = props.Path?
            .Where(image => image.Identifier != Guid.Empty)
            .Select(image => image.Identifier)
            .ToArray() ?? [];

        if (imageGuids.Length == 0)
        {
            return View(
                "~/Components/ComponentError.cshtml",
                new ComponentErrorViewModel(
                    NAME,
                    ComponentType.Widget,
                    "No tiles/images have been selected."));
        }

        var retrievedImages = await contentRetriever
            .RetrieveContentByGuids<MediaFile>(imageGuids);

        return Validate(props, retrievedImages)
            .Match(
                vm => View(
                    "~/Components/PageBuilder/Widgets/SmallTilesWidget/SmallTiles.cshtml",
                    vm),
                vm => View("~/Components/ComponentError.cshtml", vm));
    }

    private static Result<SmallTilesWidgetViewModel, ComponentErrorViewModel> Validate(
        SmallTilesWidgetProperties props,
        IEnumerable<MediaFile> retrievedImages)
    {
        var imageAssets = retrievedImages?
            .Where(image => image.LegacyMediaFileAsset is not null)
            .Select(ImageAssetViewModel.Create)
            .ToList() ?? [];

        if (imageAssets.Count == 0)
        {
            return Result.Failure<SmallTilesWidgetViewModel, ComponentErrorViewModel>(
                new ComponentErrorViewModel(
                    NAME,
                    ComponentType.Widget,
                    "Selected items no longer exist or lack image assets."));
        }

        return new SmallTilesWidgetViewModel(props, imageAssets);
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
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Path",
        ExplanationText = "Select Image Tiles",
        MinimumItems = 1,
        MaximumItems = 50,
        DefaultViewMode =
            Kentico.Xperience.Admin.Base.Forms.ContentItemSelectorViewMode.Grid,
        AllowContentItemCreation = true,
        Order = 2)]
    public IEnumerable<ContentItemReference> Path { get; set; } = [];

    [TextInputComponent(
        Label = "Section ID",
        ExplanationText = "Optional HTML anchor ID.",
        Order = 3)]
    public string SectionId { get; set; } = string.Empty;

    [DropDownComponent(
        Label = "Theme",
        ExplanationText = "Select layout style (Small Cards is default).",
        DataProviderType = typeof(EnumDropDownOptionsProvider<TileThemeOptions>),
        Order = 4)]
    public string Theme { get; set; } =
        nameof(TileThemeOptions.SmallCards);

    public TileThemeOptions ThemeParsed =>
        EnumDropDownOptionsProvider<TileThemeOptions>
            .Parse(Theme, TileThemeOptions.SmallCards);
}

public class SmallTilesWidgetViewModel : BaseWidgetViewModel
{
    protected override string WidgetName { get; } = SmallTilesWidget.NAME;

    public string Heading { get; }
    public string SectionId { get; }
    public TileThemeOptions Theme { get; }
    public IReadOnlyList<ImageAssetViewModel> Images { get; }

    public SmallTilesWidgetViewModel(
        SmallTilesWidgetProperties props,
        IReadOnlyList<ImageAssetViewModel> images)
    {
        Heading = props.Heading;
        SectionId = props.SectionId;
        Theme = props.ThemeParsed;
        Images = images;
    }
}