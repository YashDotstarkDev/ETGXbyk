using CMS.Base;
using CMS.ContentEngine;
using ETG.Core.Content;
using ETG.Web.Components.EmailBuilder.Widgets.TextBlockImages;
using Kentico.Content.Web.Mvc;
using Kentico.EmailBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Microsoft.AspNetCore.Components;

[assembly: RegisterEmailWidget(
    identifier: TextBlockImagesWidget.IDENTIFIER,
    name: TextBlockImagesWidget.NAME,
    componentType: typeof(TextBlockImagesWidget),
    PropertiesType = typeof(TextBlockImagesWidgetProperties),
    IconClass = "icon-cogwheel-square",
    Description = "Displays a text heading and copy block followed by two side-by-side images and a full-width bottom image.")]

namespace ETG.Web.Components.EmailBuilder.Widgets.TextBlockImages;

public partial class TextBlockImagesWidget : ComponentBase
{
    public const string IDENTIFIER = "ETG.Web.EmailWidget.TextBlockImages";
    public const string NAME = "Text Block Images";

    [Inject]
    private IContentRetriever ContentRetriever { get; set; } = default!;

    [Parameter]
    public TextBlockImagesWidgetProperties Properties { get; set; } = new();

    protected TextBlockImagesWidgetModel Model { get; set; } = new();

    public override Task SetParametersAsync(ParameterView parameters) =>
        ContextUtils.PropagateCurrent(async () => await base.SetParametersAsync(parameters))();

    protected override async Task OnInitializedAsync()
    {
        var image1Guid = Properties.Image1?.FirstOrDefault()?.Identifier ?? Guid.Empty;
        var image2Guid = Properties.Image2?.FirstOrDefault()?.Identifier ?? Guid.Empty;
        var image3Guid = Properties.Image3?.FirstOrDefault()?.Identifier ?? Guid.Empty;

        var guids = new[] { image1Guid, image2Guid, image3Guid }.Where(g => g != Guid.Empty).Distinct().ToList();

        var mediaDict = new Dictionary<Guid, MediaFile>();
        if (guids.Count > 0)
        {
            var files = await ContentRetriever.RetrieveContentByGuids<MediaFile>(guids);
            mediaDict = files.GroupBy(f => f.SystemFields.ContentItemGUID).ToDictionary(g => g.Key, g => g.First());
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

        Model = new TextBlockImagesWidgetModel
        {
            Heading = Properties.Heading ?? string.Empty,
            Copy = Properties.Copy ?? string.Empty,
            Image1Url = image1Url ?? string.Empty,
            Image2Url = image2Url ?? string.Empty,
            Image3Url = image3Url ?? string.Empty
        };
    }
}

public class TextBlockImagesWidgetProperties : IEmailWidgetProperties
{
    [TextInputComponent(Label = "Heading", Order = 1)]
    public string Heading { get; set; } = string.Empty;

    [TextAreaComponent(Label = "Copy", Order = 2)]
    public string Copy { get; set; } = string.Empty;

    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Image 1",
        MaximumItems = 1,
        DefaultViewMode = ContentItemSelectorViewMode.Grid,
        AllowContentItemCreation = true,
        Order = 3)]
    public IEnumerable<ContentItemReference> Image1 { get; set; } = [];

    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Image 2",
        MaximumItems = 1,
        DefaultViewMode = ContentItemSelectorViewMode.Grid,
        AllowContentItemCreation = true,
        Order = 4)]
    public IEnumerable<ContentItemReference> Image2 { get; set; } = [];

    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Image 3",
        MaximumItems = 1,
        DefaultViewMode = ContentItemSelectorViewMode.Grid,
        AllowContentItemCreation = true,
        Order = 5)]
    public IEnumerable<ContentItemReference> Image3 { get; set; } = [];
}

public class TextBlockImagesWidgetModel
{
    public string Heading { get; set; } = string.Empty;
    public string Copy { get; set; } = string.Empty;
    public string Image1Url { get; set; } = string.Empty;
    public string Image2Url { get; set; } = string.Empty;
    public string Image3Url { get; set; } = string.Empty;
}