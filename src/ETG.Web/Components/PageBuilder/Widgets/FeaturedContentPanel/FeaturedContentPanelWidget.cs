using System.ComponentModel;
using CMS.ContentEngine;
using CSharpFunctionalExtensions;
using ETG.Core.Components;
using ETG.Core.Content;
using ETG.Web.Components.PageBuilder.Widgets.FeaturedContentPanel;
using ETG.Web.Rendering;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Websites.FormAnnotations;
using Microsoft.AspNetCore.Mvc;
using XperienceCommunity.KenticoComponentIcons;

[assembly: RegisterWidget(
    identifier: FeaturedContentPanelWidget.IDENTIFIER,
    viewComponentType: typeof(FeaturedContentPanelWidget),
    name: FeaturedContentPanelWidget.NAME,
    propertiesType: typeof(FeaturedContentPanelProperties),
    Description = "Displays a featured content panel with a background image, heading, text content, image caption, and call-to-action button.",
    IconClass = KenticoIcons.CAROUSEL,
    AllowCache = true)]

namespace ETG.Web.Components.PageBuilder.Widgets.FeaturedContentPanel;

public class FeaturedContentPanelWidget(IContentRetriever contentRetriever) : ViewComponent
{
    // Identifier preserved from Kentico 13 widget registration
    public const string IDENTIFIER = "ETG.Web.Widget.FeaturedContentPanel";
    public const string NAME = "Feature";

    private readonly IContentRetriever contentRetriever = contentRetriever;

    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<FeaturedContentPanelProperties> cvm)
    {
        var props = cvm.Properties;

        var image = await contentRetriever
            .RetrieveContentByGuids<MediaFile>(props.Images.Select(i => i.Identifier))
            .TryFirst();

        return Validate(props, image)
            .Match(
                vm => View("~/Components/PageBuilder/Widgets/FeaturedContentPanel/FeaturedContentPanel.cshtml", vm),
                vm => View("~/Components/ComponentError.cshtml", vm));
    }

    private static Result<FeaturedContentPanelWidgetViewModel, ComponentErrorViewModel> Validate(FeaturedContentPanelProperties props, Maybe<MediaFile> image)
    {
        if (props.Images.FirstOrDefault() is null)
        {
            return Result.Failure<FeaturedContentPanelWidgetViewModel, ComponentErrorViewModel>(
                new ComponentErrorViewModel(NAME, ComponentType.Widget, "No image has been selected."));
        }

        if (!image.TryGetValue(out var img) || img.LegacyMediaFileAsset is null)
        {
            return Result.Failure<FeaturedContentPanelWidgetViewModel, ComponentErrorViewModel>(
                new ComponentErrorViewModel(NAME, ComponentType.Widget, "The selected content item or image file no longer exists."));
        }

        return new FeaturedContentPanelWidgetViewModel(ImageAssetViewModel.Create(img), props);
    }
}

[FormCategory(Label = "Content", Order = 1)]
public class FeaturedContentPanelProperties : BaseWidgetProperties
{
    // Field name kept as 'Images' to match Kentico 13 database schema
    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Image (1500 x 600px)",
        MinimumItems = 1,
        MaximumItems = 1,
        DefaultViewMode = Kentico.Xperience.Admin.Base.Forms.ContentItemSelectorViewMode.Grid,
        AllowContentItemCreation = true,
        Order = 1)]
    public IEnumerable<ContentItemReference> Images { get; set; } = [];

    [TextInputComponent(
        Label = "Heading",
        Order = 2)]
    public string Heading { get; set; } = "";

    [TextAreaComponent(
        Label = "Text Content",
        Order = 3)]
    public string TextContent { get; set; } = "";

    [TextInputComponent(
        Label = "CTA Label",
        Order = 4)]
    public string CTALabel { get; set; } = "";

    [TextInputComponent(
        Label = "ImageCaption",
        Order = 5)]
    public string ImageCaption { get; set; } = "";

    [TextInputComponent(
        Label = "Path",
        Order = 6)]
    public string Path { get; set; } = "";
}

public class FeaturedContentPanelWidgetViewModel : BaseWidgetViewModel
{
    protected override string WidgetName { get; } = FeaturedContentPanelWidget.NAME;

    public ImageAssetViewModel Image { get; }
    public string Heading { get; }
    public string TextContent { get; }
    public string CTALabel { get; }
    public string ImageCaption { get; }
    public string Path { get; }

    public FeaturedContentPanelWidgetViewModel(ImageAssetViewModel image, FeaturedContentPanelProperties props)
    {
        Image = image;
        Heading = props.Heading;
        TextContent = props.TextContent;
        CTALabel = props.CTALabel;
        ImageCaption = props.ImageCaption;
        Path = props.Path;
    }
}