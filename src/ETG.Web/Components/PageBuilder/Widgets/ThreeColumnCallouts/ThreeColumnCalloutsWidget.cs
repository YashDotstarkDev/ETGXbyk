using CMS.ContentEngine;
using CSharpFunctionalExtensions;
using ETG.Core.Components;
using ETG.Core.Content;
using ETG.Web.Components.PageBuilder.Widgets.CardList;
using ETG.Web.Components.PageBuilder.Widgets.ThreeColumnCallouts;
using ETG.Web.Rendering;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using XperienceCommunity.KenticoComponentIcons;

[assembly: RegisterWidget(
    identifier: ThreeColumnCalloutsWidget.IDENTIFIER,
    viewComponentType: typeof(ThreeColumnCalloutsWidget),
    name: ThreeColumnCalloutsWidget.NAME,
    propertiesType: typeof(ThreeColumnCalloutsWidgetProperties),
    Description = "Displays three callouts using either image with content or icon with content.",
    IconClass = KenticoIcons.L_LIST_IMG_ARTICLE,
    AllowCache = true)]

namespace ETG.Web.Components.PageBuilder.Widgets.ThreeColumnCallouts;

public class ThreeColumnCalloutsWidget(IContentRetriever contentRetriever) : ViewComponent
{
    public const string IDENTIFIER = "ETG.Web.Widget.ThreeColumnCallouts";
    public const string NAME = "Three Column Callouts";
    private readonly IContentRetriever contentRetriever = contentRetriever;

    public async Task<IViewComponentResult> InvokeAsync(
     ComponentViewModel<ThreeColumnCalloutsWidgetProperties> cvm)
    {
        var props = cvm.Properties;

        var callout1Image = Maybe<MediaFile>.None;
        if (props.Callout1Images?.Any(i => i.Identifier != Guid.Empty) == true)
        {
            callout1Image = await contentRetriever
                .RetrieveContentByGuids<MediaFile>(
                    props.Callout1Images
                        .Where(i => i.Identifier != Guid.Empty)
                        .Select(i => i.Identifier))
                .TryFirst();
        }

        var callout2Image = Maybe<MediaFile>.None;
        if (props.Callout2Images?.Any(i => i.Identifier != Guid.Empty) == true)
        {
            callout2Image = await contentRetriever
                .RetrieveContentByGuids<MediaFile>(
                    props.Callout2Images
                        .Where(i => i.Identifier != Guid.Empty)
                        .Select(i => i.Identifier))
                .TryFirst();
        }

        var callout3Image = Maybe<MediaFile>.None;
        if (props.Callout3Images?.Any(i => i.Identifier != Guid.Empty) == true)
        {
            callout3Image = await contentRetriever
                .RetrieveContentByGuids<MediaFile>(
                    props.Callout3Images
                        .Where(i => i.Identifier != Guid.Empty)
                        .Select(i => i.Identifier))
                .TryFirst();
        }

        return Validate(props, callout1Image, callout2Image, callout3Image)
            .Match(
                vm => View("~/Components/PageBuilder/Widgets/ThreeColumnCallouts/ThreeColumnCallouts.cshtml", vm),
                vm => View("~/Components/ComponentError.cshtml", vm)
            );
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

    private static Result<ThreeColumnCalloutsWidgetViewModel, ComponentErrorViewModel> Validate(ThreeColumnCalloutsWidgetProperties props,
        Maybe<MediaFile> callout1Image,
        Maybe<MediaFile> callout2Image,
        Maybe<MediaFile> callout3Image)
    {
        if (props.Callout1Images?.FirstOrDefault() is null)
        {
            return Result.Failure<ThreeColumnCalloutsWidgetViewModel, ComponentErrorViewModel>(
                new ComponentErrorViewModel(NAME, ComponentType.Widget, "No image has been selected."));
        }

        if (!callout1Image.TryGetValue(out var image1) || image1.LegacyMediaFileAsset is null)
        {
            return Result.Failure<ThreeColumnCalloutsWidgetViewModel, ComponentErrorViewModel>(
                new ComponentErrorViewModel(NAME, ComponentType.Widget, "The selected content item or image file no longer exists."));
        }

        ImageAssetViewModel? image2 = null;

        if (callout2Image.TryGetValue(out var image2Content) &&
            image2Content.LegacyMediaFileAsset is not null)
        {
            image2 = ImageAssetViewModel.Create(image2Content);
        }

        ImageAssetViewModel? image3 = null;

        if (callout3Image.TryGetValue(out var image3Content) &&
            image3Content.LegacyMediaFileAsset is not null)
        {
            image3 = ImageAssetViewModel.Create(image3Content);
        }

        return new ThreeColumnCalloutsWidgetViewModel(ImageAssetViewModel.Create(image1), image2, image3, props);
    }
}


[FormCategory(
    Label = "Content",
    Order = 1)]
public class ThreeColumnCalloutsWidgetProperties : BaseWidgetProperties
{
    [TextInputComponent(
        Label = "Section Id Name (Used in QuickLinks)",
        Order = 1)]
    public string SectionIdName { get; set; } = string.Empty;

    [TextInputComponent(
        Label = "Heading",
        Order = 2)]
    public string Heading { get; set; } = string.Empty;

    [DropDownComponent(
        Label = "Display Type",
        ExplanationText = "Select the layout to display for the callouts.",
        DataProviderType = typeof(EnumDropDownOptionsProvider<DisplayThemeType>),
        Order = 3)]
    public string DisplayType { get; set; } = nameof(DisplayThemeType.ImageWithContent);

    [CheckBoxComponent(
    Label = "Right align?",
    Order = 4)]
    [VisibleIfEqualTo(nameof(DisplayType), nameof(DisplayThemeType.ImageWithContent))]
    public bool StartFromRight { get; set; } = false;

    [TextInputComponent(
        Label = "Callout 1 Heading",
        Order = 5)]
    public string Callout1Heading { get; set; } = string.Empty;

    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Callout 1 Image / Icon",
        MinimumItems = 1,
        MaximumItems = 1,
        AllowContentItemCreation = true,
        Order = 6)]
    public IEnumerable<ContentItemReference> Callout1Images { get; set; } = [];

    [TextAreaComponent(
        Label = "Callout 1 Description",
        Order = 7)]
    public string Callout1Description { get; set; } = string.Empty;

    [TextInputComponent(
        Label = "Callout 1 Url",
        Order = 8)]
    public string Callout1Url { get; set; } = string.Empty;

    [TextInputComponent(
        Label = "Callout 1 CTA Button Text",
        ExplanationText = "Enter the text displayed on the callout button.",
        Order = 9)]
    public string Callout1CtaButtonText { get; set; } = "READ MORE";



    [TextInputComponent(
        Label = "Callout 2 Heading",
        Order = 10)]
    public string Callout2Heading { get; set; } = string.Empty;

    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Callout 2 Image / Icon",
        MinimumItems = 0,
        MaximumItems = 1,
        AllowContentItemCreation = true,
        Order = 11)]
    public IEnumerable<ContentItemReference> Callout2Images { get; set; } = [];

    [TextAreaComponent(
        Label = "Callout 2 Description",
        Order = 12)]
    public string Callout2Description { get; set; } = string.Empty;

    [TextInputComponent(
        Label = "Callout 2 Url",
        Order = 13)]
    public string Callout2Url { get; set; } = string.Empty;

    [TextInputComponent(
    Label = "Callout 2 CTA Button Text",
    ExplanationText = "Enter the text displayed on the callout button.",
    Order = 14)]
    public string Callout2CtaButtonText { get; set; } = "READ MORE";



    [TextInputComponent(
        Label = "Callout 3 Heading",
        Order = 15)]
    public string Callout3Heading { get; set; } = string.Empty;

    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Callout 3 Image / Icon",
        MinimumItems = 0,
        MaximumItems = 1,
        AllowContentItemCreation = true,
        Order = 16)]
    public IEnumerable<ContentItemReference> Callout3Images { get; set; } = [];

    [TextAreaComponent(
        Label = "Callout 3 Description",
        Order = 17)]
    public string Callout3Description { get; set; } = string.Empty;

    [TextInputComponent(
        Label = "Callout 3 Url",
        Order = 18)]
    public string Callout3Url { get; set; } = string.Empty;

    [TextInputComponent(
        Label = "Callout 3 CTA Button Text",
        ExplanationText = "Enter the text displayed on the callout button.",
        Order = 19)]
    public string Callout3CtaButtonText { get; set; } = "READ MORE";
}

public enum DisplayThemeType
{
    [Description("ImageWithContent")]
    ImageWithContent,
    [Description("IconWithContent")]
    IconWithContent
}
public class ThreeColumnCalloutsWidgetViewModel : BaseWidgetViewModel
{
    protected override string WidgetName { get; } = ThreeColumnCalloutsWidget.NAME;
    public string SectionIdName { get; }
    public string Heading { get; }
    public string DisplayType { get; }
    public bool StartFromRight { get; }

    public ImageAssetViewModel Callout1Image { get; }
    public string Callout1Heading { get; }
    public string Callout1Description { get; }
    public string Callout1Url { get; }
    public string Callout1CtaButtonText { get; }

    public ImageAssetViewModel Callout2Image { get; }
    public string Callout2Heading { get; }
    public string Callout2Description { get; }
    public string Callout2Url { get; }
    public string Callout2CtaButtonText { get; }

    public ImageAssetViewModel Callout3Image { get; }
    public string Callout3Heading { get; }
    public string Callout3Description { get; }
    public string Callout3Url { get; }
    public string Callout3CtaButtonText { get; }


    public ThreeColumnCalloutsWidgetViewModel(ImageAssetViewModel callout1Image, ImageAssetViewModel callout2Image, ImageAssetViewModel callout3Image, ThreeColumnCalloutsWidgetProperties props)
    {
        SectionIdName = props.SectionIdName;
        Heading = props.Heading;
        DisplayType = props.DisplayType;
        StartFromRight = props.StartFromRight;

        Callout1Image = callout1Image;
        Callout1Heading = props.Callout1Heading;
        Callout1Description = props.Callout1Description;
        Callout1Url = props.Callout1Url;
        Callout1CtaButtonText = props.Callout1CtaButtonText;

        Callout2Image = callout2Image;
        Callout2Heading = props.Callout2Heading;
        Callout2Description = props.Callout2Description;
        Callout2Url = props.Callout2Url;
        Callout2CtaButtonText = props.Callout2CtaButtonText;

        Callout3Image = callout3Image;
        Callout3Heading = props.Callout3Heading;
        Callout3Description = props.Callout3Description;
        Callout3Url = props.Callout3Url;
        Callout3CtaButtonText = props.Callout3CtaButtonText;
    }

    public bool IsImageWithContent => DisplayType == nameof(DisplayThemeType.ImageWithContent);
    public bool IsIconWithContent => DisplayType == nameof(DisplayThemeType.IconWithContent);
}