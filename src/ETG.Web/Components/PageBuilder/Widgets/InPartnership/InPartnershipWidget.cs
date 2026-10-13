using CMS.ContentEngine;
using CSharpFunctionalExtensions;
using ETG.Core.Components;
using ETG.Core.Content;
using ETG.Web.Components.PageBuilder.Widgets.Image;
using ETG.Web.Components.PageBuilder.Widgets.InPartnership;
using ETG.Web.Rendering;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Microsoft.AspNetCore.Mvc;
using XperienceCommunity.KenticoComponentIcons;

[assembly: RegisterWidget(
    identifier: InPartnershipWidget.IDENTIFIER,
    viewComponentType: typeof(InPartnershipWidget),
    name: InPartnershipWidget.NAME,
    propertiesType: typeof(InPartnershipWidgetProperties),
    Description = "Adds an In Partnership widget to the page",
    IconClass = KenticoIcons.HEARTSHAKE)]

namespace ETG.Web.Components.PageBuilder.Widgets.InPartnership;

public class InPartnershipWidget(IContentRetriever contentRetriever) : ViewComponent
{
    public const string IDENTIFIER = "ETG.Web.Widget.InPartnership";
    public const string NAME = "In Partnership";

    private readonly IContentRetriever contentRetriever = contentRetriever;

    public async Task<IViewComponentResult> InvokeAsync(
        ComponentViewModel<InPartnershipWidgetProperties> cvm)
    {
        var props = cvm.Properties;

        var logo1 = await RetrieveLogo(props.Logo1Images);
        var logo2 = await RetrieveLogo(props.Logo2Images);
        var logo3 = await RetrieveLogo(props.Logo3Images);

        return Validate(props, logo1, logo2, logo3)
            .Match(
                vm => View(
                    "~/Components/PageBuilder/Widgets/InPartnership/InPartnership.cshtml",
                    vm),
                vm => View("~/Components/ComponentError.cshtml", vm));
    }

    private async Task<Maybe<MediaFile>> RetrieveLogo(
        IEnumerable<ContentItemReference>? logoReferences)
    {
        var identifiers = logoReferences?
            .Where(logo => logo.Identifier != Guid.Empty)
            .Select(logo => logo.Identifier)
            .ToList() ?? [];

        if (identifiers.Count == 0)
        {
            return Maybe<MediaFile>.None;
        }

        return await contentRetriever
            .RetrieveContentByGuids<MediaFile>(identifiers)
            .TryFirst();
    }

    private static Result<InPartnershipWidgetViewModel, ComponentErrorViewModel> Validate(
        InPartnershipWidgetProperties props,
        Maybe<MediaFile> logo1,
        Maybe<MediaFile> logo2,
        Maybe<MediaFile> logo3)
    {
      
        var logo1Image = CreateImageViewModel(logo1);

        if (logo1Image is null)
        {
            return Result.Failure<InPartnershipWidgetViewModel, ComponentErrorViewModel>(
                new ComponentErrorViewModel(
                    NAME,
                    ComponentType.Widget,
                    "Logo 1 is required and must contain a valid image asset."));
        }

        return new InPartnershipWidgetViewModel(
            logo1Image,
            CreateImageViewModel(logo2),
            CreateImageViewModel(logo3),
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
public class InPartnershipWidgetProperties : BaseWidgetProperties
{
    [TextInputComponent(Label = "Heading", Order = 1)]
    public string Heading { get; set; } = string.Empty;

    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Logo 1",
        MinimumItems = 1,
        MaximumItems = 1,
        AllowContentItemCreation = true,
        Order = 2)]
    public IEnumerable<ContentItemReference> Logo1Images { get; set; } = [];

    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Logo 2",
        MaximumItems = 1,
        AllowContentItemCreation = true,
        Order = 3)]
    public IEnumerable<ContentItemReference> Logo2Images { get; set; } = [];

    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Logo 3",
        MaximumItems = 1,
        AllowContentItemCreation = true,
        Order = 4)]
    public IEnumerable<ContentItemReference> Logo3Images { get; set; } = [];

    [CheckBoxComponent(Label = "Large Logos", Order = 5)]
    public bool LargeLogos { get; set; }
}

public class InPartnershipWidgetViewModel : BaseWidgetViewModel
{
    protected override string WidgetName { get; } = InPartnershipWidget.NAME;

    public string Heading { get; }
    public ImageAssetViewModel Logo1Image { get; }
    public ImageAssetViewModel? Logo2Image { get; }
    public ImageAssetViewModel? Logo3Image { get; }
    public bool LargeLogos { get; }

    public InPartnershipWidgetViewModel(
        ImageAssetViewModel logo1Image,
        ImageAssetViewModel? logo2Image,
        ImageAssetViewModel? logo3Image,
        InPartnershipWidgetProperties props)
    {
        Heading = props.Heading;
        Logo1Image = logo1Image;
        Logo2Image = logo2Image;
        Logo3Image = logo3Image;
        LargeLogos = props.LargeLogos;
    }
}