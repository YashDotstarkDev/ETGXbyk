using CMS.ContentEngine;
using CSharpFunctionalExtensions;
using ETG.Core.Components;
using ETG.Core.Content;
using ETG.Web.Components.PageBuilder.Widgets.CTAPanel;
using ETG.Web.Rendering;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Websites.FormAnnotations;
using Microsoft.AspNetCore.Mvc;
using XperienceCommunity.KenticoComponentIcons;

[assembly: RegisterWidget(
    identifier: CTAPanelWidget.IDENTIFIER,
    viewComponentType: typeof(CTAPanelWidget),
    name: CTAPanelWidget.NAME,
    propertiesType: typeof(CTAPanelProperties),
    Description = "Displays a call-to-action panel with a background image, heading, text content, and button link.",
    IconClass = KenticoIcons.CAROUSEL,
    AllowCache = true)]

namespace ETG.Web.Components.PageBuilder.Widgets.CTAPanel;

public class CTAPanelWidget(IContentRetriever contentRetriever)
    : ViewComponent
{
    public const string IDENTIFIER = "ETG.Web.Widget.CTAPanel";
    public const string NAME = "CTA Panel";

    private readonly IContentRetriever contentRetriever = contentRetriever;

    public async Task<IViewComponentResult> InvokeAsync(
        ComponentViewModel<CTAPanelProperties> cvm)
    {
        var props = cvm.Properties;

        var imageReferences = props.Images?
            .Where(image => image.Identifier != Guid.Empty)
            .ToList() ?? [];

        Maybe<MediaFile> image = Maybe<MediaFile>.None;

        if (imageReferences.Count > 0)
        {
            image = await contentRetriever
                .RetrieveContentByGuids<MediaFile>(
                    imageReferences.Select(image => image.Identifier))
                .TryFirst();
        }

        return Validate(props, image)
            .Match(
                vm => View(
                    "~/Components/PageBuilder/Widgets/CTAPanel/CTAPanel.cshtml",
                    vm),
                error => View(
                    "~/Components/ComponentError.cshtml",
                    error));
    }

    private static Result<
        CTAPanelWidgetViewModel,
        ComponentErrorViewModel> Validate(
            CTAPanelProperties props,
            Maybe<MediaFile> image)
    {
        ImageAssetViewModel? imageViewModel = null;

        if (image.TryGetValue(out var img) &&
            img.LegacyMediaFileAsset is not null)
        {
            imageViewModel = ImageAssetViewModel.Create(img);
        }

        return new CTAPanelWidgetViewModel(imageViewModel, props);
    }
}

[FormCategory(Label = "Content", Order = 1)]
public class CTAPanelProperties : BaseWidgetProperties
{
    // First field in the Content category.
    [DropDownComponent(
        Label = "Display Type",
        Options = "DisplayType1;Display Type 1\r\nDestinationCta;Destination CTA",
        Order = 0)]
    public string DisplayType { get; set; } = "DisplayType1";

    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Background Image",
        MaximumItems = 1,
        DefaultViewMode =
            Kentico.Xperience.Admin.Base.Forms.ContentItemSelectorViewMode.Grid,
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
        Label = "Path",
        Order = 5)]
    public string Path { get; set; } = "";

    [TextInputComponent(
        Label = "CTA Label 2",
        Order = 6)]
    public string CTALabel2 { get; set; } = "";

    [TextInputComponent(
        Label = "Path 2",
        Order = 7)]
    public string Path2 { get; set; } = "";

    // Additional fields: visible only for Destination CTA.

    [TextInputComponent(
        Label = "Phone 1 Display Text",
        Order = 8)]
    [VisibleIfEqualTo(nameof(DisplayType), "DestinationCta")]
    public string Phone1Text { get; set; } = "";

    [TextInputComponent(
        Label = "Phone 1 Dial Number",
        Order = 9)]
    [VisibleIfEqualTo(nameof(DisplayType), "DestinationCta")]
    public string Phone1Number { get; set; } = "";

    [TextInputComponent(
        Label = "Phone 2 Display Text",
        Order = 10)]
    [VisibleIfEqualTo(nameof(DisplayType), "DestinationCta")]
    public string Phone2Text { get; set; } = "";

    [TextInputComponent(
        Label = "Phone 2 Dial Number",
        Order = 11)]
    [VisibleIfEqualTo(nameof(DisplayType), "DestinationCta")]
    public string Phone2Number { get; set; } = "";
}

public class CTAPanelWidgetViewModel : BaseWidgetViewModel
{
    protected override string WidgetName => CTAPanelWidget.NAME;

    public string DisplayType { get; }

    public ImageAssetViewModel? Image { get; }

    public string Heading { get; }
    public string TextContent { get; }

    public string CTALabel { get; }
    public string Path { get; }

    public string CTALabel2 { get; set; }
    public string Path2 { get; set; }

    public string HeadingBeforeLastWord { get; }
    public string HeadingLastWord { get; }

    public string Phone1Text { get; }
    public string Phone1Href { get; }

    public string Phone2Text { get; }
    public string Phone2Href { get; }

    public CTAPanelWidgetViewModel(
        ImageAssetViewModel? image,
        CTAPanelProperties props)
    {
        DisplayType = string.IsNullOrWhiteSpace(props.DisplayType)
            ? "DisplayType1"
            : props.DisplayType;

        Image = image;

        // Preserve the original heading for Display Type 1.
        Heading = props.Heading;

        // Separate the last word for Destination CTA.
        var headingWords = (props.Heading ?? string.Empty)
            .Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries);

        HeadingBeforeLastWord = headingWords.Length > 1
            ? string.Join(
                " ",
                headingWords.Take(headingWords.Length - 1))
            : string.Empty;

        HeadingLastWord = headingWords.LastOrDefault() ?? string.Empty;

        TextContent = props.TextContent;

        CTALabel = props.CTALabel;
        Path = props.Path;

        CTALabel2 = props.CTALabel2;
        Path2 = props.Path2;

        Phone1Text = props.Phone1Text;
        Phone1Href = CreatePhoneHref(props.Phone1Number);

        Phone2Text = props.Phone2Text;
        Phone2Href = CreatePhoneHref(props.Phone2Number);
    }

    private static string CreatePhoneHref(string? number)
    {
        if (string.IsNullOrWhiteSpace(number))
        {
            return "";
        }

        var value = number.Trim();

        var digits = string.Concat(
            value.Where(character =>
                character >= '0' && character <= '9'));

        if (digits.Length == 0)
        {
            return "";
        }

        var prefix = value.StartsWith("+") ? "+" : "";

        return $"tel:{prefix}{digits}";
    }
}