using System.ComponentModel;
using CMS.ContentEngine;
using CSharpFunctionalExtensions;
using ETG.Core.Components;
using ETG.Core.Content;
using ETG.Web.Components.PageBuilder.Widgets.ImageWithContent;
using ETG.Web.Rendering;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Websites.FormAnnotations;
using Microsoft.AspNetCore.Mvc;
using XperienceCommunity.KenticoComponentIcons;

[assembly: RegisterWidget(
    identifier: ImageWithContentWidget.IDENTIFIER,
    viewComponentType: typeof(ImageWithContentWidget),
    name: ImageWithContentWidget.NAME,
    propertiesType: typeof(ImageWithContentWidgetProperties),
    Description = "Image with content and optional CTA",
    IconClass = KenticoIcons.PICTURE,
    AllowCache = true)]

namespace ETG.Web.Components.PageBuilder.Widgets.ImageWithContent;

public class ImageWithContentWidget(IContentRetriever contentRetriever) : ViewComponent
{
    public const string IDENTIFIER = "Website.ImageWithContentWidget";
    public const string NAME = "ImageWithContent";

    private readonly IContentRetriever contentRetriever = contentRetriever;

    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<ImageWithContentWidgetProperties> cvm)
    {
        var props = cvm.Properties;

        var image = await contentRetriever
            .RetrieveContentByGuids<MediaFile>(props.SelectedImages.Select(i => i.Identifier))
            .TryFirst();

        return Validate(props, image)
            .Match(
                vm => View("~/Components/PageBuilder/Widgets/ImageWithContent/ImageWithContent.cshtml", vm),
                vm => View("~/Components/ComponentError.cshtml", vm));
    }

    private static Result<ImageWithContentWidgetViewModel, ComponentErrorViewModel> Validate(ImageWithContentWidgetProperties props, Maybe<MediaFile> image)
    {
        if (props.SelectedImages.FirstOrDefault() is null)
        {
            return Result.Failure<ImageWithContentWidgetViewModel, ComponentErrorViewModel>(new ComponentErrorViewModel(NAME, ComponentType.Widget, "No image has been selected."));
        }

        if (!image.TryGetValue(out var img) || img.LegacyMediaFileAsset is null)
        {
            return Result.Failure<ImageWithContentWidgetViewModel, ComponentErrorViewModel>(new ComponentErrorViewModel(NAME, ComponentType.Widget, "The selected content item or image file no longer exists."));
        }

        return new ImageWithContentWidgetViewModel(ImageAssetViewModel.Create(img), props);
    }
}

[FormCategory(Label = "Content", Order = 1)]
[FormCategory(Label = "Display", Order = 3)]
public class ImageWithContentWidgetProperties : BaseWidgetProperties
{
    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Selected image",
        MinimumItems = 1,
        MaximumItems = 1,
        DefaultViewMode = Kentico.Xperience.Admin.Base.Forms.ContentItemSelectorViewMode.Grid,
        AllowContentItemCreation = true,
        Order = 2)]
    public IEnumerable<ContentItemReference> SelectedImages { get; set; } = [];

    [TextInputComponent(
        Label = "Heading",
        Order = 3)]
    public string Heading { get; set; } = "";

    // Using a simple TextArea component for rich content in test env; prefer a proper RichText component in production
    [RichTextEditorComponent(
        Label = "Content",
        Order = 4)]
    public string Content { get; set; } = "";

    [TextInputComponent(
        Label = "CTA Text",
        Order = 5)]
    public string CtaText { get; set; } = "";

    [UrlSelectorComponent(
        Label = "CTA URL",
        Order = 6)]
    public string CtaUrl { get; set; } = "";

    [DropDownComponent(
        Label = "Layout",
        ExplanationText = "Layout of the image relative to the content",
        DataProviderType = typeof(EnumDropDownOptionsProvider<ImageWithContentLayouts>),
        Order = 7
    )]
    public string Layout { get; set; } = nameof(ImageWithContentLayouts.ImageLeft);
    public ImageWithContentLayouts LayoutParsed => EnumDropDownOptionsProvider<ImageWithContentLayouts>.Parse(Layout, ImageWithContentLayouts.ImageLeft);

    [DropDownComponent(
        Label = "Background",
        ExplanationText = "Background styling",
        DataProviderType = typeof(EnumDropDownOptionsProvider<BackgroundOptions>),
        Order = 8
    )]
    public string Background { get; set; } = nameof(BackgroundOptions.Transparent);
    public BackgroundOptions BackgroundParsed => EnumDropDownOptionsProvider<BackgroundOptions>.Parse(Background, BackgroundOptions.Transparent);

    [DropDownComponent(
        Label = "Top Margin",
        ExplanationText = "Top margin size",
        DataProviderType = typeof(EnumDropDownOptionsProvider<ImageWithContentMarginSizes>),
        Order = 9
    )]
    public string TopMargin { get; set; } = nameof(ImageWithContentMarginSizes.None);
    public ImageWithContentMarginSizes TopMarginParsed => EnumDropDownOptionsProvider<ImageWithContentMarginSizes>.Parse(TopMargin, ImageWithContentMarginSizes.None);

    [DropDownComponent(
        Label = "Bottom Margin",
        ExplanationText = "Bottom margin size",
        DataProviderType = typeof(EnumDropDownOptionsProvider<ImageWithContentMarginSizes>),
        Order = 10
    )]
    public string BottomMargin { get; set; } = nameof(ImageWithContentMarginSizes.None);
    public ImageWithContentMarginSizes BottomMarginParsed => EnumDropDownOptionsProvider<ImageWithContentMarginSizes>.Parse(BottomMargin, ImageWithContentMarginSizes.None);

    [CheckBoxComponent(
        Label = "Show CTA",
        Order = 11)]
    public bool ShowCta { get; set; } = false;
}

public class ImageWithContentWidgetViewModel : BaseWidgetViewModel
{
    protected override string WidgetName { get; } = ImageWithContentWidget.NAME;

    public ImageAssetViewModel Image { get; }
    public string Heading { get; }
    public string Content { get; }
    public string CtaText { get; }
    public string CtaUrl { get; }
    public ImageWithContentLayouts Layout { get; }
    public BackgroundOptions Background { get; }
    public ImageWithContentMarginSizes TopMargin { get; }
    public ImageWithContentMarginSizes BottomMargin { get; }
    public bool ShowCta { get; }

    public ImageWithContentWidgetViewModel(ImageAssetViewModel image, ImageWithContentWidgetProperties props)
    {
        Image = image;
        Heading = props.Heading;
        Content = props.Content;
        CtaText = props.CtaText;
        CtaUrl = props.CtaUrl;
        Layout = props.LayoutParsed;
        Background = props.BackgroundParsed;
        TopMargin = props.TopMarginParsed;
        BottomMargin = props.BottomMarginParsed;
        ShowCta = props.ShowCta;
    }
}

public enum ImageWithContentLayouts { ImageLeft, ImageRight, ImageAbove }
public enum BackgroundOptions { Transparent, Light, Dark }
public enum ImageWithContentMarginSizes { None, Small, Medium, Large }
