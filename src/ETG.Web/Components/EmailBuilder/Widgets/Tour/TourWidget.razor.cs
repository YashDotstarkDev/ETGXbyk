using CMS.Base;
using CMS.ContentEngine;
using ETG.Core.Content;
using ETG.Web.Components.EmailBuilder.Widgets.Tour;
using Kentico.Content.Web.Mvc;
using Kentico.EmailBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Microsoft.AspNetCore.Components;

[assembly: RegisterEmailWidget(
    identifier: TourWidget.IDENTIFIER,
    name: TourWidget.NAME,
    componentType: typeof(TourWidget),
    PropertiesType = typeof(TourWidgetProperties),
    IconClass = "icon-cogwheel-square",
    Description = "Tour widget with image, details, and enquire CTA.")]

namespace ETG.Web.Components.EmailBuilder.Widgets.Tour;

public partial class TourWidget : ComponentBase
{
    public const string IDENTIFIER = "ETG.Web.EmailWidget.Tour";
    public const string NAME = "Tour";

    [Inject]
    private IContentRetriever ContentRetriever { get; set; } = default!;

    [Parameter]
    public TourWidgetProperties Properties { get; set; } = new();

    protected TourWidgetModel Model { get; set; } = new();

    public override Task SetParametersAsync(ParameterView parameters) =>
        ContextUtils.PropagateCurrent(async () => await base.SetParametersAsync(parameters))();

    protected override async Task OnInitializedAsync()
    {
        var imageGuid = Properties.image?.FirstOrDefault()?.Identifier ?? Guid.Empty;

        string imageUrl = string.Empty;
        if (imageGuid != Guid.Empty)
        {
            var files = await ContentRetriever.RetrieveContentByGuids<MediaFile>(new[] { imageGuid });
            var mediaFile = files.FirstOrDefault();
            imageUrl = mediaFile?.LegacyMediaFileAsset?.Url ?? string.Empty;
        }

        Model = new TourWidgetModel
        {
            image = imageUrl ?? string.Empty,
            tourname = Properties.tourname ?? string.Empty,
            nights = Properties.nights ?? string.Empty,
            inclusions = Properties.inclusions ?? string.Empty,
            price = Properties.price ?? string.Empty,
            departure = Properties.departure ?? string.Empty,
            copy = Properties.copy ?? string.Empty,
            hasPeaceOfMind = Properties.hasPeaceOfMind,
            experiences = Properties.experiences ?? string.Empty,
            tourstyles = Properties.tourstyles ?? string.Empty,
            enquireUrl = Properties.enquireUrl ?? string.Empty
        };
    }
}

public class TourWidgetProperties : IEmailWidgetProperties
{
    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Image",
        MaximumItems = 1,
        DefaultViewMode = ContentItemSelectorViewMode.Grid,
        AllowContentItemCreation = true,
        Order = 1)]
    public IEnumerable<ContentItemReference> image { get; set; } = [];

    [TextInputComponent(Label = "Tour Name", Order = 2)]
    public string tourname { get; set; } = string.Empty;

    [TextInputComponent(Label = "Number of nights", Order = 3)]
    public string nights { get; set; } = string.Empty;

    [TextInputComponent(Label = "Inclusions", Order = 4)]
    public string inclusions { get; set; } = string.Empty;

    [TextInputComponent(Label = "Price", Order = 5)]
    public string price { get; set; } = string.Empty;

    [TextInputComponent(Label = "Departure Date", Order = 6)]
    public string departure { get; set; } = string.Empty;

    [TextAreaComponent(Label = "Copy", Order = 7)]
    public string copy { get; set; } = string.Empty;

    [CheckBoxComponent(Label = "Has Peace Of Mind", Order = 8)]
    public bool hasPeaceOfMind { get; set; }

    [TextInputComponent(Label = "Experiences", Order = 9)]
    public string experiences { get; set; } = string.Empty;

    [TextInputComponent(Label = "Travel Styles", Order = 10)]
    public string tourstyles { get; set; } = string.Empty;

    [TextInputComponent(Label = "URL", Order = 11)]
    public string enquireUrl { get; set; } = string.Empty;
}

public class TourWidgetModel
{
    public string image { get; set; } = string.Empty;
    public string tourname { get; set; } = string.Empty;
    public string nights { get; set; } = string.Empty;
    public string inclusions { get; set; } = string.Empty;
    public string price { get; set; } = string.Empty;
    public string departure { get; set; } = string.Empty;
    public string copy { get; set; } = string.Empty;
    public bool hasPeaceOfMind { get; set; }
    public string experiences { get; set; } = string.Empty;
    public string tourstyles { get; set; } = string.Empty;
    public string enquireUrl { get; set; } = string.Empty;
}
