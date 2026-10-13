using System.ComponentModel;
using CMS.ContentEngine;
using CSharpFunctionalExtensions;
using ETG.Core.Components;
using ETG.Core.Content;
using ETG.Web.Components.PageBuilder.Widgets.FullWidthImage;
using ETG.Web.Rendering;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Websites.FormAnnotations;
using Microsoft.AspNetCore.Mvc;
using XperienceCommunity.KenticoComponentIcons;

[assembly: RegisterWidget(
    identifier: FullWidthImageWidget.IDENTIFIER,
    viewComponentType: typeof(FullWidthImageWidget),
    name: FullWidthImageWidget.NAME,
    propertiesType: typeof(FullWidthImageProperties),
    Description = "Displays a full width image with an optional caption.",
    IconClass = KenticoIcons.PICTURE,
    AllowCache = true)]

namespace ETG.Web.Components.PageBuilder.Widgets.FullWidthImage;

public class FullWidthImageWidget(IContentRetriever contentRetriever) : ViewComponent
{
    public const string IDENTIFIER = "ETG.Web.Widget.FullWidthImage";
    public const string NAME = "Full width Image";

    private readonly IContentRetriever contentRetriever = contentRetriever;

    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<FullWidthImageProperties> cvm)
    {
        var props = cvm.Properties;

        var image = await contentRetriever
            .RetrieveContentByGuids<MediaFile>(props.Image.Select(i => i.Identifier))
            .TryFirst();

        return Validate(props, image)
            .Match(
                vm => View("~/Components/PageBuilder/Widgets/FullWidthImage/FullWidthImage.cshtml", vm),
                vm => View("~/Components/ComponentError.cshtml", vm));
    }

    private static Result<FullWidthImageWidgetViewModel, ComponentErrorViewModel> Validate(FullWidthImageProperties props, Maybe<MediaFile> image)
    {
        if (props.Image.FirstOrDefault() is null)
        {
            return Result.Failure<FullWidthImageWidgetViewModel, ComponentErrorViewModel>(
                new ComponentErrorViewModel(NAME, ComponentType.Widget, "No image has been selected."));
        }

        if (!image.TryGetValue(out var img) || img.LegacyMediaFileAsset is null)
        {
            return Result.Failure<FullWidthImageWidgetViewModel, ComponentErrorViewModel>(
                new ComponentErrorViewModel(NAME, ComponentType.Widget, "The selected content item or image file no longer exists."));
        }

        return new FullWidthImageWidgetViewModel(ImageAssetViewModel.Create(img), props);
    }
}

[FormCategory(Label = "Content", Order = 1)]
public class FullWidthImageProperties : BaseWidgetProperties
{
    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Image",
        MinimumItems = 1,
        MaximumItems = 1,
        DefaultViewMode = Kentico.Xperience.Admin.Base.Forms.ContentItemSelectorViewMode.Grid,
        AllowContentItemCreation = true,
        Order = 1)]
    public IEnumerable<ContentItemReference> Image { get; set; } = [];

    [TextInputComponent(
        Label = "Caption",
        Order = 2)]
    public string Caption { get; set; } = "";
}

public class FullWidthImageWidgetViewModel : BaseWidgetViewModel
{
    protected override string WidgetName { get; } = FullWidthImageWidget.NAME;

    public ImageAssetViewModel Image { get; }
    public string Caption { get; }

    public FullWidthImageWidgetViewModel(ImageAssetViewModel image, FullWidthImageProperties props)
    {
        Image = image;
        Caption = props.Caption;
    }
}