using CMS.Base;
using CMS.ContentEngine;
using ETG.Core.Content;
using ETG.Web.Components.EmailBuilder.Widgets.DestinationSingleSet;
using Kentico.Content.Web.Mvc;
using Kentico.EmailBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Microsoft.AspNetCore.Components;

[assembly: RegisterEmailWidget(
    identifier: DestinationSingleSetWidget.IDENTIFIER,
    name: DestinationSingleSetWidget.NAME,
    componentType: typeof(DestinationSingleSetWidget),
    PropertiesType = typeof(DestinationSingleSetWidgetProperties),
    IconClass = "icon-cogwheel-square",
    Description = "Displays a single destination card with an image, label, and link.")]

namespace ETG.Web.Components.EmailBuilder.Widgets.DestinationSingleSet;

public partial class DestinationSingleSetWidget : ComponentBase
{
    public const string IDENTIFIER = "ETG.Web.EmailWidget.DestinationSingleSet";
    public const string NAME = "Destination Single Set";

    [Inject]
    private IContentRetriever ContentRetriever { get; set; } = default!;

    [Parameter]
    public DestinationSingleSetWidgetProperties Properties { get; set; } = new();

    protected DestinationSingleSetWidgetModel Model { get; set; } = new();

    public override Task SetParametersAsync(ParameterView parameters) =>
        ContextUtils.PropagateCurrent(async () => await base.SetParametersAsync(parameters))();

    protected override async Task OnInitializedAsync()
    {
        string imageUrl = string.Empty;
        var imageGuid = Properties?.Image?.FirstOrDefault()?.Identifier ?? Guid.Empty;
        if (imageGuid != Guid.Empty)
        {
            var mediaFiles = await ContentRetriever.RetrieveContentByGuids<MediaFile>([imageGuid]);
            imageUrl = mediaFiles?.FirstOrDefault()?.LegacyMediaFileAsset?.Url ?? string.Empty;
        }

        Model = new DestinationSingleSetWidgetModel
        {
            ImageUrl = imageUrl,
            Url = Properties?.Url ?? string.Empty,
            Label = Properties?.Label ?? string.Empty
        };
    }
}

public class DestinationSingleSetWidgetProperties : IEmailWidgetProperties
{
    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Image",
        MaximumItems = 1,
        DefaultViewMode = ContentItemSelectorViewMode.Grid,
        AllowContentItemCreation = true,
        Order = 1)]
    public IEnumerable<ContentItemReference> Image { get; set; } = [];

    [TextInputComponent(Label = "URL", Order = 2)]
    public string Url { get; set; } = string.Empty;

    [TextInputComponent(Label = "Label", Order = 3)]
    public string Label { get; set; } = string.Empty;
}

public class DestinationSingleSetWidgetModel
{
    public string ImageUrl { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}