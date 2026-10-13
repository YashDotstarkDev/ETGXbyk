using CMS.ContentEngine;
using CSharpFunctionalExtensions;
using ETG.Core.Components;
using ETG.Core.Content;
using ETG.Web.Components.PageBuilder.Widgets.Image;
using ETG.Web.Components.PageBuilder.Widgets.TwoSideBySideImage;
using ETG.Web.Rendering;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Microsoft.AspNetCore.Mvc;
using XperienceCommunity.KenticoComponentIcons;

[assembly: RegisterWidget(
    identifier: TwoSideBySideImageWidget.IDENTIFIER,
    viewComponentType: typeof(TwoSideBySideImageWidget),
    name: TwoSideBySideImageWidget.NAME,
    propertiesType: typeof(TwoSideBySideImageWidgetProperties),
    Description = "Adds two side by side images to the page",
    IconClass = KenticoIcons.PICTURE)]

namespace ETG.Web.Components.PageBuilder.Widgets.TwoSideBySideImage;

public class TwoSideBySideImageWidget(IContentRetriever contentRetriever) : ViewComponent
{
    public const string IDENTIFIER = "ETG.Web.Widget.TwoSideBySideImage";
    public const string NAME = "Two Side By Side Image";

    private readonly IContentRetriever contentRetriever = contentRetriever;

    public async Task<IViewComponentResult> InvokeAsync(
        ComponentViewModel<TwoSideBySideImageWidgetProperties> cvm)
    {
        var props = cvm.Properties;

        var image1 = await RetrieveImage(props.Image1);
        var image2 = await RetrieveImage(props.Image2);
        var image3 = await RetrieveImage(props.Image3);

        return Validate(props, image1, image2, image3)
            .Match(
                vm => View(
                    "~/Components/PageBuilder/Widgets/TwoSideBySideImage/TwoSideBySideImage.cshtml",
                    vm),
                vm => View("~/Components/ComponentError.cshtml", vm));
    }

    private async Task<Maybe<MediaFile>> RetrieveImage(
        IEnumerable<ContentItemReference>? imageReferences)
    {
        var identifiers = imageReferences?
            .Where(image => image.Identifier != Guid.Empty)
            .Select(image => image.Identifier)
            .ToList() ?? [];

        if (identifiers.Count == 0)
        {
            return Maybe<MediaFile>.None;
        }

        return await contentRetriever
            .RetrieveContentByGuids<MediaFile>(identifiers)
            .TryFirst();
    }

    private static Result<TwoSideBySideImageWidgetViewModel, ComponentErrorViewModel> Validate(
        TwoSideBySideImageWidgetProperties props,
        Maybe<MediaFile> image1,
        Maybe<MediaFile> image2,
        Maybe<MediaFile> image3)
    {
        if (string.IsNullOrWhiteSpace(props.Caption1))
        {
            return Result.Failure<TwoSideBySideImageWidgetViewModel, ComponentErrorViewModel>(
                new ComponentErrorViewModel(
                    NAME,
                    ComponentType.Widget,
                    "Caption 1 is required."));
        }

        var image1ViewModel = CreateImageViewModel(image1);

        if (image1ViewModel is null)
        {
            return Result.Failure<TwoSideBySideImageWidgetViewModel, ComponentErrorViewModel>(
                new ComponentErrorViewModel(
                    NAME,
                    ComponentType.Widget,
                    "Image 1 is required and must contain a valid image asset."));
        }

        return new TwoSideBySideImageWidgetViewModel(
            image1ViewModel,
            CreateImageViewModel(image2),
            CreateImageViewModel(image3),
            props);
    }

    private static ImageAssetViewModel? CreateImageViewModel(
        Maybe<MediaFile> image)
    {
        return image.TryGetValue(out var content) &&
               content.LegacyMediaFileAsset is not null
            ? ImageAssetViewModel.Create(content)
            : null;
    }
}

[FormCategory(Label = "Content", Order = 1)]
public class TwoSideBySideImageWidgetProperties : BaseWidgetProperties
{
    [TextInputComponent(Label = "Heading", Order = 0)]
    public string? Heading { get; set; }

    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Image 1",
        MinimumItems = 1,
        MaximumItems = 1,
        AllowContentItemCreation = true,
        Order = 1)]
    public IEnumerable<ContentItemReference> Image1 { get; set; } = [];

    [TextInputComponent(Label = "Caption 1", Order = 2)]
    public string Caption1 { get; set; } = string.Empty;

    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Image 2",
        MaximumItems = 1,
        AllowContentItemCreation = true,
        Order = 3)]
    public IEnumerable<ContentItemReference> Image2 { get; set; } = [];

    [TextInputComponent(Label = "Caption 2", Order = 4)]
    public string Caption2 { get; set; } = string.Empty;

    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Image 3",
        MaximumItems = 1,
        AllowContentItemCreation = true,
        Order = 5)]
    public IEnumerable<ContentItemReference> Image3 { get; set; } = [];

    [TextInputComponent(Label = "Caption 3", Order = 6)]
    public string Caption3 { get; set; } = string.Empty;

    [CheckBoxComponent(Label = "Add Side Padding", Order = 7)]
    public bool HasSidePadding { get; set; }
}

public class TwoSideBySideImageWidgetViewModel : BaseWidgetViewModel
{
    protected override string WidgetName { get; } = TwoSideBySideImageWidget.NAME;

    public string? Heading { get; } = "";
    public ImageAssetViewModel Image1 { get; }
    public string Caption1 { get; }
    public ImageAssetViewModel? Image2 { get; }
    public string Caption2 { get; }
    public ImageAssetViewModel? Image3 { get; }
    public string Caption3 { get; }
    public bool HasSidePadding { get; }

    public TwoSideBySideImageWidgetViewModel(
        ImageAssetViewModel image1,
        ImageAssetViewModel? image2,
        ImageAssetViewModel? image3,
        TwoSideBySideImageWidgetProperties props)
    {
        Heading = props.Heading;
        Image1 = image1;
        Caption1 = props.Caption1;
        Image2 = image2;
        Caption2 = props.Caption2;
        Image3 = image3;
        Caption3 = props.Caption3;
        HasSidePadding = props.HasSidePadding;
    }
}