using CMS.Base;
using CMS.ContentEngine;
using ETG.Core.Content;
using ETG.Web.Components.EmailBuilder.Widgets.DestinationTwoSet;
using Kentico.Content.Web.Mvc;
using Kentico.EmailBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Microsoft.AspNetCore.Components;

[assembly: RegisterEmailWidget(
    identifier: DestinationTwoSetWidget.IDENTIFIER,
    name: DestinationTwoSetWidget.NAME,
    componentType: typeof(DestinationTwoSetWidget),
    PropertiesType = typeof(DestinationTwoSetWidgetProperties),
    IconClass = "icon-cogwheel-square",
    Description = "Displays two destination cards side-by-side with images, labels, and links.")]

namespace ETG.Web.Components.EmailBuilder.Widgets.DestinationTwoSet;

public partial class DestinationTwoSetWidget : ComponentBase
{
    public const string IDENTIFIER = "ETG.Web.EmailWidget.DestinationTwoSet";
    public const string NAME = "Destination Two Set";

    [Inject]
    private IContentRetriever ContentRetriever { get; set; } = default!;

    [Parameter]
    public DestinationTwoSetWidgetProperties Properties { get; set; } = new();

    protected DestinationTwoSetWidgetModel Model { get; set; } = new();

    public override Task SetParametersAsync(ParameterView parameters) =>
        ContextUtils.PropagateCurrent(async () => await base.SetParametersAsync(parameters))();

    protected override async Task OnInitializedAsync()
    {
        var image1Guid = Properties?.Image1?.FirstOrDefault()?.Identifier ?? Guid.Empty;
        var image2Guid = Properties?.Image2?.FirstOrDefault()?.Identifier ?? Guid.Empty;

        var guids = new[] { image1Guid, image2Guid }.Where(g => g != Guid.Empty).Distinct().ToList();

        var mediaDict = new Dictionary<Guid, MediaFile>();
        if (guids.Count > 0)
        {
            var files = await ContentRetriever.RetrieveContentByGuids<MediaFile>(guids);
            if (files is not null)
            {
                mediaDict = files
                    .Where(f => f?.SystemFields is not null)
                    .ToDictionary(f => f.SystemFields.ContentItemGUID);
            }
        }

        string image1Url = image1Guid != Guid.Empty && mediaDict.TryGetValue(image1Guid, out var mf1)
            ? mf1?.LegacyMediaFileAsset?.Url ?? string.Empty
            : string.Empty;

        string image2Url = image2Guid != Guid.Empty && mediaDict.TryGetValue(image2Guid, out var mf2)
            ? mf2?.LegacyMediaFileAsset?.Url ?? string.Empty
            : string.Empty;

        Model = new DestinationTwoSetWidgetModel
        {
            Image1Url = image1Url,
            Label1 = Properties?.Label1 ?? string.Empty,
            Url1 = Properties?.Url1 ?? string.Empty,
            Image2Url = image2Url,
            Label2 = Properties?.Label2 ?? string.Empty,
            Url2 = Properties?.Url2 ?? string.Empty
        };
    }
}

public class DestinationTwoSetWidgetProperties : IEmailWidgetProperties
{
    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Image 1",
        MaximumItems = 1,
        DefaultViewMode = ContentItemSelectorViewMode.Grid,
        AllowContentItemCreation = true,
        Order = 1)]
    public IEnumerable<ContentItemReference> Image1 { get; set; } = [];

    [TextInputComponent(Label = "Label 1", Order = 2)]
    public string Label1 { get; set; } = string.Empty;

    [TextInputComponent(Label = "URL 1", Order = 3)]
    public string Url1 { get; set; } = string.Empty;

    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Image 2",
        MaximumItems = 1,
        DefaultViewMode = ContentItemSelectorViewMode.Grid,
        AllowContentItemCreation = true,
        Order = 4)]
    public IEnumerable<ContentItemReference> Image2 { get; set; } = [];

    [TextInputComponent(Label = "Label 2", Order = 5)]
    public string Label2 { get; set; } = string.Empty;

    [TextInputComponent(Label = "URL 2", Order = 6)]
    public string Url2 { get; set; } = string.Empty;
}

public class DestinationTwoSetWidgetModel
{
    public string Image1Url { get; set; } = string.Empty;
    public string Label1 { get; set; } = string.Empty;
    public string Url1 { get; set; } = string.Empty;
    public string Image2Url { get; set; } = string.Empty;
    public string Label2 { get; set; } = string.Empty;
    public string Url2 { get; set; } = string.Empty;
}