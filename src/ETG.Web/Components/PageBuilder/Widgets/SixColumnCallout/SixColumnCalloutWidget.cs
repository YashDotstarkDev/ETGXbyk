using CMS.ContentEngine;
using CSharpFunctionalExtensions;
using ETG.Core.Components;
using ETG.Core.Content;
using ETG.Web.Components.PageBuilder.Widgets.SixColumnCallout;
using ETG.Web.Rendering;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Microsoft.AspNetCore.Mvc;
using XperienceCommunity.KenticoComponentIcons;

[assembly: RegisterWidget(
    identifier: SixColumnCalloutWidget.IDENTIFIER,
    viewComponentType: typeof(SixColumnCalloutWidget),
    name: SixColumnCalloutWidget.NAME,
    propertiesType: typeof(SixColumnCalloutWidgetProperties),
    IconClass = KenticoIcons.L_GRID_2_2,
    AllowCache = true)]

namespace ETG.Web.Components.PageBuilder.Widgets.SixColumnCallout;

public class SixColumnCalloutWidget : ViewComponent
{
    public const string IDENTIFIER = "ETG.Web.Widget.SixColumnCallout";
    public const string NAME = "Six Column Callout";

    private readonly IContentRetriever contentRetriever;

    public SixColumnCalloutWidget(IContentRetriever contentRetriever)
    {
        this.contentRetriever = contentRetriever;
    }

    public async Task<IViewComponentResult> InvokeAsync(
        ComponentViewModel<SixColumnCalloutWidgetProperties> cvm)
    {
        var props = cvm.Properties;

        ImageAssetViewModel? brandHighlightLogo = null;

        // Retrieve the logo only for Brand Highlights.
        if (props.DisplayType == "Brand Highlights")
        {
            brandHighlightLogo = await RetrieveBrandLogoAsync(
                props.BrandHighlightLogo);
        }

        return Validate(props, brandHighlightLogo)
            .Match(
                vm => View(
                    "~/Components/PageBuilder/Widgets/SixColumnCallout/SixColumnCallout.cshtml",
                    vm),
                error => View(
                    "~/Components/ComponentError.cshtml",
                    error));
    }

    private async Task<ImageAssetViewModel?> RetrieveBrandLogoAsync(
        IEnumerable<ContentItemReference>? references)
    {
        var identifiers = references?
            .Where(reference => reference.Identifier != Guid.Empty)
            .Select(reference => reference.Identifier)
            .Take(1)
            .ToList() ?? [];

        if (identifiers.Count == 0)
        {
            return null;
        }

        // Uses the same retrieval pattern as your In Partnership widget.
        var logo = await contentRetriever
            .RetrieveContentByGuids<MediaFile>(identifiers)
            .TryFirst();

        return logo.TryGetValue(out var content) &&
               content.LegacyMediaFileAsset is not null
            ? ImageAssetViewModel.Create(content)
            : null;
    }

    private static Result<
        SixColumnCalloutWidgetViewModel,
        ComponentErrorViewModel> Validate(
            SixColumnCalloutWidgetProperties props,
            ImageAssetViewModel? brandHighlightLogo)
    {
        var labels = new[]
        {
            props.Label1,
            props.Label2,
            props.Label3,
            props.Label4,
            props.Label5,
            props.Label6
        };

        var svgCodes = new[]
        {
            props.SVGCode1,
            props.SVGCode2,
            props.SVGCode3,
            props.SVGCode4,
            props.SVGCode5,
            props.SVGCode6
        };

        var itemCount =
            props.DisplayType == "Brand Highlights" ? 4 : 6;

        var hasCompleteItem = false;

        for (var i = 0; i < itemCount; i++)
        {
            var hasLabel = !string.IsNullOrWhiteSpace(labels[i]);
            var hasSvg = !string.IsNullOrWhiteSpace(svgCodes[i]);

            if (hasLabel != hasSvg)
            {
                return Result.Failure<
                    SixColumnCalloutWidgetViewModel,
                    ComponentErrorViewModel>(
                    new ComponentErrorViewModel(
                        NAME,
                        ComponentType.Widget,
                        $"Label {i + 1} and SVG Code {i + 1} must be entered together."));
            }

            if (hasLabel && hasSvg)
            {
                hasCompleteItem = true;
            }
        }

        if (!hasCompleteItem)
        {
            return Result.Failure<
                SixColumnCalloutWidgetViewModel,
                ComponentErrorViewModel>(
                new ComponentErrorViewModel(
                    NAME,
                    ComponentType.Widget,
                    "At least one label and its matching SVG Code are required."));
        }

        return new SixColumnCalloutWidgetViewModel(
            props,
            brandHighlightLogo);
    }
}

public class SixColumnCalloutWidgetProperties : BaseWidgetProperties
{
    [DropDownComponent(
        Label = "Display Type",
        Order = 1,
        Options = "Feature Highlights;Feature Highlights\r\nBrand Highlights;Brand Highlights\r\nExperience Highlights;Experience Highlights")]
    public string DisplayType { get; set; } = "Feature Highlights";

    [TextInputComponent(
        Label = "Heading",
        Order = 2)]
    public string Heading { get; set; } = "";

    [TextInputComponent(
        Label = "Label 1",
        Order = 3)]
    public string Label1 { get; set; } = "";

    [TextAreaComponent(
        Label = "SVG Code 1",
        Order = 4)]
    public string SVGCode1 { get; set; } = "";

    [TextInputComponent(
        Label = "Label 2",
        Order = 5)]
    public string Label2 { get; set; } = "";

    [TextAreaComponent(
        Label = "SVG Code 2",
        Order = 6)]
    public string SVGCode2 { get; set; } = "";

    [TextInputComponent(
        Label = "Label 3",
        Order = 7)]
    public string Label3 { get; set; } = "";

    [TextAreaComponent(
        Label = "SVG Code 3",
        Order = 8)]
    public string SVGCode3 { get; set; } = "";

    [TextInputComponent(
        Label = "Label 4",
        Order = 9)]
    public string Label4 { get; set; } = "";

    [TextAreaComponent(
        Label = "SVG Code 4",
        Order = 10)]
    public string SVGCode4 { get; set; } = "";

    [TextInputComponent(
        Label = "Label 5",
        Order = 11)]
    public string Label5 { get; set; } = "";

    [TextAreaComponent(
        Label = "SVG Code 5",
        Order = 12)]
    public string SVGCode5 { get; set; } = "";

    [TextInputComponent(
        Label = "Label 6",
        Order = 13)]
    public string Label6 { get; set; } = "";

    [TextAreaComponent(
        Label = "SVG Code 6",
        Order = 14)]
    public string SVGCode6 { get; set; } = "";

    [TextInputComponent(
        Label = "CTA Label",
        Order = 15)]
    public string CTALabel { get; set; } = "Learn More";

    [TextInputComponent(
        Label = "CTA Url",
        Order = 16)]
    public string CTAUrl { get; set; } = "/peace-of-mind";

    [TextInputComponent(
        Label = "Section ID",
        Order = 17)]
    public string SectionId { get; set; } = "";

    // Optional: select or create one logo.
    // The selector appears only for Brand Highlights.
    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Brand Highlight Logo",
        MaximumItems = 1,
        AllowContentItemCreation = true,
        Order = 18)]
    [VisibleIfEqualTo(
        nameof(DisplayType),
        "Brand Highlights",
        StringComparison.Ordinal)]
    public IEnumerable<ContentItemReference> BrandHighlightLogo
    {
        get;
        set;
    } = [];
}

public class SixColumnCalloutWidgetViewModel
{
    public string DisplayType { get; }
    public string Heading { get; }

    public string Label1 { get; }
    public string SVGCode1 { get; }

    public string Label2 { get; }
    public string SVGCode2 { get; }

    public string Label3 { get; }
    public string SVGCode3 { get; }

    public string Label4 { get; }
    public string SVGCode4 { get; }

    public string Label5 { get; }
    public string SVGCode5 { get; }

    public string Label6 { get; }
    public string SVGCode6 { get; }

    public string CTALabel { get; }
    public string CTAUrl { get; }
    public string SectionId { get; }

    // Resolved single logo for the Razor view.
    public ImageAssetViewModel? BrandHighlightLogo { get; }

    public SixColumnCalloutWidgetViewModel(
        SixColumnCalloutWidgetProperties props,
        ImageAssetViewModel? brandHighlightLogo = null)
    {
        DisplayType = props.DisplayType;
        Heading = props.Heading;

        Label1 = props.Label1;
        SVGCode1 = props.SVGCode1;

        Label2 = props.Label2;
        SVGCode2 = props.SVGCode2;

        Label3 = props.Label3;
        SVGCode3 = props.SVGCode3;

        Label4 = props.Label4;
        SVGCode4 = props.SVGCode4;

        Label5 = props.Label5;
        SVGCode5 = props.SVGCode5;

        Label6 = props.Label6;
        SVGCode6 = props.SVGCode6;

        CTALabel = string.IsNullOrWhiteSpace(props.CTALabel) ? new SixColumnCalloutWidgetProperties().CTALabel: props.CTALabel;
        CTAUrl = string.IsNullOrWhiteSpace(props.CTAUrl) ? new SixColumnCalloutWidgetProperties().CTAUrl : props.CTAUrl;
        SectionId = props.SectionId;

        BrandHighlightLogo = props.DisplayType == "Brand Highlights"
            ? brandHighlightLogo
            : null;
    }
}