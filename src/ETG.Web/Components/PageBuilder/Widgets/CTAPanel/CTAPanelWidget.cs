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
using System.ComponentModel;
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

public class CTAPanelWidget(IContentRetriever contentRetriever) : ViewComponent
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
                vm => View("~/Components/ComponentError.cshtml", vm));
    }

    private static Result<CTAPanelWidgetViewModel, ComponentErrorViewModel> Validate(
        CTAPanelProperties props,
        Maybe<MediaFile> image)
    {
        ImageAssetViewModel? imageViewModel = null;

        if (image.TryGetValue(out var img) && img.LegacyMediaFileAsset is not null)
        {
            imageViewModel = ImageAssetViewModel.Create(img);
        }

        return new CTAPanelWidgetViewModel(imageViewModel, props);
    }
}

[FormCategory(Label = "Content", Order = 1)]
public class CTAPanelProperties : BaseWidgetProperties
{
    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Background Image",
        MaximumItems = 1,
        DefaultViewMode =
            Kentico.Xperience.Admin.Base.Forms.ContentItemSelectorViewMode.Grid,
        AllowContentItemCreation = true,
        Order = 1)]
    public IEnumerable<ContentItemReference> Images { get; set; } = [];

    [TextInputComponent(Label = "Heading", Order = 2)]
    public string Heading { get; set; } = "";

    [TextAreaComponent(Label = "Text Content", Order = 3)]
    public string TextContent { get; set; } = "";

    [TextInputComponent(Label = "CTA Label", Order = 4)]
    public string CTALabel { get; set; } = "";

    [TextInputComponent(Label = "Path", Order = 5)]
    public string Path { get; set; } = "";

    [TextInputComponent(Label = "CTA Label 2", Order = 6)]
    public string CTALabel2 { get; set; } = "";

    [TextInputComponent(Label = "Path 2", Order = 7)]
    public string Path2 { get; set; } = "";
}

public class CTAPanelWidgetViewModel : BaseWidgetViewModel
{
    protected override string WidgetName { get; } = CTAPanelWidget.NAME;

    public ImageAssetViewModel? Image { get; }
    public string Heading { get; }
    public string TextContent { get; }
    public string CTALabel { get; }
    public string Path { get; }
    public string CTALabel2 { get; set; }
    public string Path2 { get; set; }

    public CTAPanelWidgetViewModel(
        ImageAssetViewModel? image,
        CTAPanelProperties props)
    {
        Image = image;
        Heading = props.Heading;
        TextContent = props.TextContent;
        CTALabel = props.CTALabel;
        Path = props.Path;
        CTALabel2 = props.CTALabel2;
        Path2 = props.Path2;
    }
}