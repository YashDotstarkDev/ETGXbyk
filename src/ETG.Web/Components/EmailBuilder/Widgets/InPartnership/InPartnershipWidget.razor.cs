using CMS.Base;
using CMS.ContentEngine;
using ETG.Core.Content;
using ETG.Web.Components.EmailBuilder.Widgets.InPartnership;
using Kentico.Content.Web.Mvc;
using Kentico.EmailBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Microsoft.AspNetCore.Components;

[assembly: RegisterEmailWidget(
    identifier: InPartnershipWidget.IDENTIFIER,
    name: InPartnershipWidget.NAME,
    componentType: typeof(InPartnershipWidget),
    PropertiesType = typeof(InPartnershipWidgetProperties),
    IconClass = "icon-cogwheel-square",
    Description = "Displays 1, 2, or 3 partner logos or images dynamically based on selection.")]

namespace ETG.Web.Components.EmailBuilder.Widgets.InPartnership;

public partial class InPartnershipWidget : ComponentBase
{
    public const string IDENTIFIER = "ETG.Web.EmailWidget.InPartnership";
    public const string NAME = "In Partnership";

    [Inject]
    private IContentRetriever ContentRetriever { get; set; } = default!;

    [Parameter]
    public InPartnershipWidgetProperties Properties { get; set; } = new();

    protected InPartnershipWidgetModel Model { get; set; } = new();

    public override Task SetParametersAsync(ParameterView parameters) =>
        ContextUtils.PropagateCurrent(async () => await base.SetParametersAsync(parameters))();

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        Properties ??= new InPartnershipWidgetProperties();

        var image1Guid = Properties.Image1?.FirstOrDefault()?.Identifier ?? Guid.Empty;
        var image2Guid = Properties.Image2?.FirstOrDefault()?.Identifier ?? Guid.Empty;
        var image3Guid = Properties.Image3?.FirstOrDefault()?.Identifier ?? Guid.Empty;

        var guids = new[] { image1Guid, image2Guid, image3Guid }.Where(g => g != Guid.Empty).Distinct().ToList();

        var mediaDict = new Dictionary<Guid, MediaFile>();
        if (guids.Count > 0)
        {
            var files = await ContentRetriever.RetrieveContentByGuids<MediaFile>(guids);
            if (files is not null)
            {
                foreach (var file in files)
                {
                    var guid = file?.SystemFields?.ContentItemGUID ?? Guid.Empty;
                    if (file is not null && guid != Guid.Empty)
                    {
                        mediaDict.TryAdd(guid, file);
                    }
                }
            }
        }

        string image1Url = image1Guid != Guid.Empty && mediaDict.TryGetValue(image1Guid, out var mf1)
            ? mf1.LegacyMediaFileAsset?.Url ?? string.Empty
            : string.Empty;

        string image2Url = image2Guid != Guid.Empty && mediaDict.TryGetValue(image2Guid, out var mf2)
            ? mf2.LegacyMediaFileAsset?.Url ?? string.Empty
            : string.Empty;

        string image3Url = image3Guid != Guid.Empty && mediaDict.TryGetValue(image3Guid, out var mf3)
            ? mf3.LegacyMediaFileAsset?.Url ?? string.Empty
            : string.Empty;

        Model = new InPartnershipWidgetModel
        {
            Image1Url = image1Url,
            ImageAltText1 = Properties?.ImageAltText1 ?? string.Empty,
            CtaUrl1 = Properties?.CtaUrl1 ?? string.Empty,

            Image2Url = image2Url,
            ImageAltText2 = Properties?.ImageAltText2 ?? string.Empty,
            CtaUrl2 = Properties?.CtaUrl2 ?? string.Empty,

            Image3Url = image3Url,
            ImageAltText3 = Properties?.ImageAltText3 ?? string.Empty,
            CtaUrl3 = Properties?.CtaUrl3 ?? string.Empty
        };
    }
}

public class InPartnershipWidgetProperties : IEmailWidgetProperties
{
    // Image 1
    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Image 1 - Image",
        MaximumItems = 1,
        DefaultViewMode = ContentItemSelectorViewMode.Grid,
        AllowContentItemCreation = true,
        Order = 1)]
    public IEnumerable<ContentItemReference> Image1 { get; set; } = [];

    [TextInputComponent(Label = "Image 1 - Alt Text", Order = 2)]
    public string ImageAltText1 { get; set; } = string.Empty;

    [TextInputComponent(Label = "Image 1 - CTA Url", Order = 3)]
    public string CtaUrl1 { get; set; } = string.Empty;

    // Image 2
    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Image 2 - Image",
        MaximumItems = 1,
        DefaultViewMode = ContentItemSelectorViewMode.Grid,
        AllowContentItemCreation = true,
        Order = 4)]
    public IEnumerable<ContentItemReference> Image2 { get; set; } = [];

    [TextInputComponent(Label = "Image 2 - Alt text", Order = 5)]
    public string ImageAltText2 { get; set; } = string.Empty;

    [TextInputComponent(Label = "Image 2 - CTA Url", Order = 6)]
    public string CtaUrl2 { get; set; } = string.Empty;

    // Image 3
    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Image 3 - Image",
        MaximumItems = 1,
        DefaultViewMode = ContentItemSelectorViewMode.Grid,
        AllowContentItemCreation = true,
        Order = 7)]
    public IEnumerable<ContentItemReference> Image3 { get; set; } = [];

    [TextInputComponent(Label = "Image 3 - Alt text", Order = 8)]
    public string ImageAltText3 { get; set; } = string.Empty;

    [TextInputComponent(Label = "Image 3 - CTA Url", Order = 9)]
    public string CtaUrl3 { get; set; } = string.Empty;
}

public class InPartnershipWidgetModel
{
    public string Image1Url { get; set; } = string.Empty;
    public string ImageAltText1 { get; set; } = string.Empty;
    public string CtaUrl1 { get; set; } = string.Empty;

    public string Image2Url { get; set; } = string.Empty;
    public string ImageAltText2 { get; set; } = string.Empty;
    public string CtaUrl2 { get; set; } = string.Empty;

    public string Image3Url { get; set; } = string.Empty;
    public string ImageAltText3 { get; set; } = string.Empty;
    public string CtaUrl3 { get; set; } = string.Empty;

    public bool HasImage1 => !string.IsNullOrWhiteSpace(Image1Url);
    public bool HasImage2 => !string.IsNullOrWhiteSpace(Image2Url);
    public bool HasImage3 => !string.IsNullOrWhiteSpace(Image3Url);
}