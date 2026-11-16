using CMS.Base;
using CMS.ContentEngine;
using ETG.Core.Content;
using ETG.Web.Components.EmailBuilder.Widgets.FeaturedTour;
using Kentico.Components.Web.Mvc.FormComponents;
using Kentico.Content.Web.Mvc;
using Kentico.EmailBuilder.Web.Mvc;
using Kentico.Forms.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Microsoft.AspNetCore.Components;

[assembly: RegisterEmailWidget(
    identifier: FeaturedTourWidget.IDENTIFIER,
    name: FeaturedTourWidget.NAME,
    componentType: typeof(FeaturedTourWidget),
    PropertiesType = typeof(FeaturedTourWidgetProperties),
    IconClass = "icon-cogwheel-square",
    Description = "Featured tour with image, details, inclusions, and pricing.")]

namespace ETG.Web.Components.EmailBuilder.Widgets.FeaturedTour;

public partial class FeaturedTourWidget : ComponentBase
{
    public const string IDENTIFIER = "ETG.Web.EmailWidget.FeaturedTour";
    public const string NAME = "Featured Tour";

    [Inject]
    private IContentRetriever ContentRetriever { get; set; } = default!;

    [Parameter]
    public FeaturedTourWidgetProperties Properties { get; set; } = new();

    protected FeaturedTourWidgetModel Model { get; set; } = new();

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

        Model = new FeaturedTourWidgetModel
        {
            heading = Properties.heading,
            image = imageUrl,
            title = Properties.title,
            copy = Properties.copy,
            nights = Properties.nights,
            inclusions = Properties.inclusions,
            price = Properties.price,
            departure = Properties.departure,
            hasPeaceOfMind = Properties.hasPeaceOfMind,
            experiences = Properties.experiences,
            tourstyles = Properties.tourstyles,
            cruiseType = Properties.cruiseType,
            ctaUrl = Properties.ctaUrl,
            ctaCopy = Properties.ctaCopy
        };
    }
}

public class FeaturedTourWidgetProperties : IEmailWidgetProperties
{
    [TextInputComponent(Label = "Heading", Order = 1)]
    public string heading { get; set; } = string.Empty;

    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Image",
        MaximumItems = 1,
        DefaultViewMode = ContentItemSelectorViewMode.Grid,
        AllowContentItemCreation = true,
        Order = 2)]
    public IEnumerable<ContentItemReference> image { get; set; } = [];

    [TextInputComponent(Label = "Title", Order = 3)]
    public string title { get; set; } = string.Empty;

    [TextAreaComponent(Label = "Copy", Order = 4)]
    public string copy { get; set; } = string.Empty;

    [TextInputComponent(Label = "Number of nights", Order = 5)]
    public string nights { get; set; } = string.Empty;

    [TextInputComponent(Label = "Inclusions", ExplanationText = "Add inclusions as Meals,Flights,Accomodation,Transfer", Order = 6)]
    public string inclusions { get; set; } = "Meals,Flights,Accommodation,Transfer";

    [TextInputComponent(Label = "Price", Order = 7)]
    public string price { get; set; } = string.Empty;

    [DateTimeInputComponent(Label = "Departure dates", Order = 8)]
    public DateTime? departure { get; set; } 

    [CheckBoxComponent(Label = "Has Peace Of Mind", Order = 9)]
    public bool hasPeaceOfMind { get; set; }

    [TextInputComponent(Label = "Experiences", Order = 10)]
    public string experiences { get; set; } = string.Empty;

    [TextInputComponent(Label = "Travel Styles", Order = 11)]
    public string tourstyles { get; set; } = string.Empty;

    [TextInputComponent(Label = "Cruise type", Order = 12)]
    public string cruiseType { get; set; } = string.Empty;

    [TextInputComponent(Label = "CTA URL", Order = 13)]
    public string ctaUrl { get; set; } = string.Empty;

    [TextInputComponent(Label = "CTA Copy", Order = 14)]
    public string ctaCopy { get; set; } = string.Empty;
}

public class FeaturedTourWidgetModel
{
    public string heading { get; set; } = string.Empty;
    public string image { get; set; } = string.Empty;
    public string title { get; set; } = string.Empty;
    public string copy { get; set; } = string.Empty;
    public string nights { get; set; } = string.Empty;
    public string inclusions { get; set; } = string.Empty;
    public string price { get; set; } = string.Empty;
    public DateTime? departure { get; set; } 
    public bool hasPeaceOfMind { get; set; }
    public string experiences { get; set; } = string.Empty;
    public string tourstyles { get; set; } = string.Empty;
    public string cruiseType { get; set; } = string.Empty;
    public string ctaUrl { get; set; } = string.Empty;
    public string ctaCopy { get; set; } = string.Empty;
}
