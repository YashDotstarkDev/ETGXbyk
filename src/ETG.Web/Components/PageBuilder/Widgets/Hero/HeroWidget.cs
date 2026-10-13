using System.ComponentModel;
using CMS.ContentEngine;
using CSharpFunctionalExtensions;
using ETG.Core.Components;
using ETG.Core.Content;
using ETG.Web.Components.PageBuilder.Widgets.Hero;
using ETG.Web.Rendering;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Websites.FormAnnotations;
using Microsoft.AspNetCore.Mvc;
using XperienceCommunity.KenticoComponentIcons;

[assembly: RegisterWidget(
    identifier: HeroWidget.IDENTIFIER,
    viewComponentType: typeof(HeroWidget),
    name: HeroWidget.NAME,
    propertiesType: typeof(HeroWidgetProperties),
    Description = "Displays a hero banner with image, title, subtitle and CTA.",
    IconClass = KenticoIcons.PICTURE,
    AllowCache = true)]

namespace ETG.Web.Components.PageBuilder.Widgets.Hero;

public class HeroWidget(IContentRetriever contentRetriever) : ViewComponent
{
    public const string IDENTIFIER = "Website.HeroWidget";
    public const string NAME = "Hero Banner";

    private readonly IContentRetriever contentRetriever = contentRetriever;

    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<HeroWidgetProperties> cvm)
    {
        var props = cvm.Properties;

        var imageContent = await contentRetriever
            .RetrieveContentByGuids<MediaFile>(props.SelectedImage.Select(i => i.Identifier))
            .TryFirst();

        return Validate(props, imageContent)
            .Match(
                vm => View("~/Components/PageBuilder/Widgets/Hero/Hero.cshtml", vm),
                vm => View("~/Components/ComponentError.cshtml", vm));
    }

    private static Result<HeroWidgetViewModel, ComponentErrorViewModel> Validate(HeroWidgetProperties props, Maybe<MediaFile> image)
    {
        if (string.IsNullOrWhiteSpace(props.Title))
        {
            return Result.Failure<HeroWidgetViewModel, ComponentErrorViewModel>(new ComponentErrorViewModel(NAME, ComponentType.Widget, "No title has been set for this widget."));
        }

        if (props.SelectedImage.FirstOrDefault() is null)
        {
            return Result.Failure<HeroWidgetViewModel, ComponentErrorViewModel>(new ComponentErrorViewModel(NAME, ComponentType.Widget, "No image has been selected."));
        }

        if (!image.TryGetValue(out var img) || img.LegacyMediaFileAsset is null)
        {
            return Result.Failure<HeroWidgetViewModel, ComponentErrorViewModel>(new ComponentErrorViewModel(NAME, ComponentType.Widget, "The selected content item or image file no longer exists."));
        }

        return new HeroWidgetViewModel(ImageAssetViewModel.Create(img), props);
    }
}

[FormCategory(Label = "Content", Order = 1)]
[FormCategory(Label = "Display", Order = 3)]
public class HeroWidgetProperties : BaseWidgetProperties
{
    [TextInputComponent(
        Label = "Title",
        ExplanationText = "Main heading displayed in the hero",
        WatermarkText = "Hero title",
        Order = 1)]
    public string Title { get; set; } = "";

    [TextInputComponent(
        Label = "Subtitle",
        ExplanationText = "Supporting text displayed below the title",
        WatermarkText = "Supporting text",
        Order = 2)]
    public string Subtitle { get; set; } = "";

    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Selected image",
        MinimumItems = 1,
        MaximumItems = 1,
        DefaultViewMode = Kentico.Xperience.Admin.Base.Forms.ContentItemSelectorViewMode.Grid,
        AllowContentItemCreation = true,
        Order = 3)]
    public IEnumerable<ContentItemReference> SelectedImage { get; set; } = [];

    [TextInputComponent(
        Label = "CTA text",
        ExplanationText = "Text for the primary call to action button",
        WatermarkText = "Learn more",
        Order = 4)]
    public string CTAButtonText { get; set; } = "";

    [UrlSelectorComponent(
        Label = "CTA URL",
        Order = 5)]
    public string CTAUrl { get; set; } = "";

    [DropDownComponent(
        Label = "Text alignment",
        ExplanationText = "Alignment for title and subtitle",
        DataProviderType = typeof(EnumDropDownOptionsProvider<HeroTextAlignments>),
        Order = 6
    )]
    public string TextAlignment { get; set; } = nameof(HeroTextAlignments.Center);
    public HeroTextAlignments TextAlignmentParsed => EnumDropDownOptionsProvider<HeroTextAlignments>.Parse(TextAlignment, HeroTextAlignments.Center);

    [DropDownComponent(
        Label = "Image overlay",
        ExplanationText = "Optional overlay color for better text contrast",
        DataProviderType = typeof(EnumDropDownOptionsProvider<HeroOverlayOptions>),
        Order = 7
    )]
    public string Overlay { get; set; } = nameof(HeroOverlayOptions.None);
    public HeroOverlayOptions OverlayParsed => EnumDropDownOptionsProvider<HeroOverlayOptions>.Parse(Overlay, HeroOverlayOptions.None);
    [DropDownComponent(
        Label = "Padding",
        ExplanationText = "Top and bottom padding size for the hero banner",
        Tooltip = "Select a padding size applied to both top and bottom",
        DataProviderType = typeof(EnumDropDownOptionsProvider<MarginSizes>),
        Order = 8
    )]
    public string Padding { get; set; } = nameof(MarginSizes.Medium);
    public MarginSizes PaddingParsed => EnumDropDownOptionsProvider<MarginSizes>.Parse(Padding, MarginSizes.Medium);
}

public enum HeroTextAlignments { Left, Center, Right }
public enum HeroOverlayOptions { None, Dark_25, Dark_50 }

public enum MarginSizes
{
    [Description("None")]
    None,
    [Description("Small")]
    Small,
    [Description("Medium")]
    Medium,
    [Description("Large")]
    Large
}
public class HeroWidgetViewModel : BaseWidgetViewModel
{
    protected override string WidgetName { get; } = HeroWidget.NAME;

    public ImageAssetViewModel Image { get; }
    public string Title { get; }
    public string Subtitle { get; }
    public string CTAButtonText { get; }
    public string CTAUrl { get; }
    public HeroTextAlignments TextAlignment { get; }
    public HeroOverlayOptions Overlay { get; }
    public MarginSizes Padding { get; }

    public HeroWidgetViewModel(ImageAssetViewModel image, HeroWidgetProperties props)
    {
        Image = image;
        Title = props.Title;
        Subtitle = props.Subtitle;
        CTAButtonText = props.CTAButtonText;
        CTAUrl = props.CTAUrl;
        TextAlignment = props.TextAlignmentParsed;
        Overlay = props.OverlayParsed;
        Padding = props.PaddingParsed;
    }
}
