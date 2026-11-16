using CMS.Base;
using CMS.ContentEngine;
using ETG.Core.Content;
using ETG.Web.Components.EmailBuilder.Widgets.Event;
using Kentico.Content.Web.Mvc;
using Kentico.EmailBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Microsoft.AspNetCore.Components;
using System.Linq; // Ensure System.Linq is present

[assembly: RegisterEmailWidget(
    identifier: EventWidget.IDENTIFIER,
    name: EventWidget.NAME,
    componentType: typeof(EventWidget),
    PropertiesType = typeof(EventWidgetProperties),
    IconClass = "icon-cogwheel-square",
    Description = "Event widget.")]

namespace ETG.Web.Components.EmailBuilder.Widgets.Event;

public partial class EventWidget : ComponentBase
{
    public const string IDENTIFIER = "ETG.Web.EmailWidget.Event";
    public const string NAME = "Event";

    [Inject]
    private IContentRetriever ContentRetriever { get; set; } = default!;

    [Parameter]
    public EventWidgetProperties Properties { get; set; } = new();

    protected EventWidgetModel Model { get; set; } = new();

    public override Task SetParametersAsync(ParameterView parameters) =>
        ContextUtils.PropagateCurrent(async () => await base.SetParametersAsync(parameters))();

    protected override async Task OnInitializedAsync()
    {
        // FIX: Change 'Properties.FirstOrDefault()' to 'Properties.Image.FirstOrDefault()'
        var ImageGuid = Properties.Image.FirstOrDefault()?.Identifier ?? Guid.Empty;

        var guids = new[] { ImageGuid }.Where(g => g != Guid.Empty).Distinct().ToList();
        var mediaDict = new Dictionary<Guid, MediaFile>();

        if (guids.Count > 0)
        {
            var files = await ContentRetriever.RetrieveContentByGuids<MediaFile>(guids);
            mediaDict = files.ToDictionary(f => f.SystemFields.ContentItemGUID);
        }

        string ImageUrl = string.Empty;
        if (ImageGuid != Guid.Empty && mediaDict.TryGetValue(ImageGuid, out var mf_Image))
            ImageUrl = mf_Image.LegacyMediaFileAsset?.Url ?? string.Empty;

        Model = new EventWidgetModel
        {
            Title = Properties.Title,
            Image = ImageUrl,
            Description = Properties.Description,
            Copy = Properties.Copy,
            CTALabel = Properties.CTALabel,
            CTAUrl = Properties.CTAUrl,
            Location1 = Properties.Location1,
            Location1StartDate = Properties.Location1StartDate,
            Location1EndDate = Properties.Location1EndDate,
            Location2 = Properties.Location2,
            Location2StartDate = Properties.Location2StartDate,
            Location2EndDate = Properties.Location2EndDate,
            Location3 = Properties.Location3,
            Location3StartDate = Properties.Location3StartDate,
            Location3EndDate = Properties.Location3EndDate,
        };
    }
}

public class EventWidgetProperties : IEmailWidgetProperties
{
    [TextInputComponent(Label = "Title", Order = 1)]
    public string Title { get; set; } = string.Empty;

    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Image",
        MaximumItems = 1,
        DefaultViewMode = ContentItemSelectorViewMode.Grid,
        AllowContentItemCreation = true,
        Order = 2)]
    public IEnumerable<ContentItemReference> Image { get; set; } = [];

    [RichTextEditorComponent(Label = "Description", Order = 3)]
    public string Description { get; set; } = string.Empty;

    [TextAreaComponent(Label = "Copy", Order = 4)]
    public string Copy { get; set; } = string.Empty;

    [TextInputComponent(Label = "CTA Label", Order = 5)]
    public string CTALabel { get; set; } = string.Empty;

    [TextInputComponent(Label = "CTA Url", Order = 6)]
    public string CTAUrl { get; set; } = string.Empty;

    [TextInputComponent(Label = "Location", Order = 7)]
    public string Location1 { get; set; } = string.Empty;

    [DateTimeInputComponent(Label = "Start Date", Order = 8)]
    public DateTime? Location1StartDate { get; set; }

    [DateTimeInputComponent(Label = "End Date", Order = 9)]
    public DateTime? Location1EndDate { get; set; }

    [TextInputComponent(Label = "Location", Order = 10)]
    public string Location2 { get; set; } = string.Empty;

    [DateTimeInputComponent(Label = "Start Date", Order = 11)]
    public DateTime? Location2StartDate { get; set; }

    [DateTimeInputComponent(Label = "End Date", Order = 12)]
    public DateTime? Location2EndDate { get; set; }

    [TextInputComponent(Label = "Location", Order = 13)]
    public string Location3 { get; set; } = string.Empty;

    [DateTimeInputComponent(Label = "Start Date", Order = 14)]
    public DateTime? Location3StartDate { get; set; }

    [DateTimeInputComponent(Label = "End Date", Order = 15)]
    public DateTime? Location3EndDate { get; set; }
}

public class EventWidgetModel
{
    public string Title { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Copy { get; set; } = string.Empty;
    public string CTALabel { get; set; } = string.Empty;
    public string CTAUrl { get; set; } = string.Empty;
    public string Location1 { get; set; } = string.Empty;
    public DateTime? Location1StartDate { get; set; }
    public DateTime? Location1EndDate { get; set; }
    public string Location2 { get; set; } = string.Empty;
    public DateTime? Location2StartDate { get; set; }
    public DateTime? Location2EndDate { get; set; }
    public string Location3 { get; set; } = string.Empty;
    public DateTime? Location3StartDate { get; set; }
    public DateTime? Location3EndDate { get; set; }
}
