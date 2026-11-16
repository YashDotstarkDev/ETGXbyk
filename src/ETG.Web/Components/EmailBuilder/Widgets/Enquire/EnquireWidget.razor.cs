using CMS.Base;
using CMS.ContentEngine;
using ETG.Core.Content;
using ETG.Web.Components.EmailBuilder.Widgets.Enquire;
using Kentico.Content.Web.Mvc;
using Kentico.EmailBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Microsoft.AspNetCore.Components;

[assembly: RegisterEmailWidget(
    identifier: EnquireWidget.IDENTIFIER,
    name: EnquireWidget.NAME,
    componentType: typeof(EnquireWidget),
    PropertiesType = typeof(EnquireWidgetProperties),
    IconClass = "icon-cogwheel-square",
    Description = "Enquire widget.")]

namespace ETG.Web.Components.EmailBuilder.Widgets.Enquire;

public partial class EnquireWidget : ComponentBase
{
    public const string IDENTIFIER = "ETG.Web.EmailWidget.Enquire";
    public const string NAME = "Enquire";

    [Inject]
    private IContentRetriever ContentRetriever { get; set; } = default!;

    [Parameter]
    public EnquireWidgetProperties Properties { get; set; } = new();

    protected EnquireWidgetModel Model { get; set; } = new();

    public override Task SetParametersAsync(ParameterView parameters) =>
        ContextUtils.PropagateCurrent(async () => await base.SetParametersAsync(parameters))();

    protected override async Task OnInitializedAsync()
    {
        var imageGuid = Properties.image?.FirstOrDefault()?.Identifier ?? Guid.Empty;

        var guids = new[] { imageGuid }.Where(g => g != Guid.Empty).Distinct().ToList();

        var mediaDict = new Dictionary<Guid, MediaFile>();
        if (guids.Count > 0)
        {
            var files = await ContentRetriever.RetrieveContentByGuids<MediaFile>(guids);
            mediaDict = files.ToDictionary(f => f.SystemFields.ContentItemGUID);
        }

        string imageUrl = string.Empty;
        if (imageGuid != Guid.Empty && mediaDict.TryGetValue(imageGuid, out var mf_image))
            imageUrl = mf_image?.LegacyMediaFileAsset?.Url ?? string.Empty;

        Model = new EnquireWidgetModel
        {
            heading = Properties.heading,
            image = imageUrl,
            copy = Properties.copy,
            ctaUrl = Properties.ctaUrl,
        };
    }
}

public class EnquireWidgetProperties : IEmailWidgetProperties
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

    [TextAreaComponent(Label = "Copy", Order = 3)]
    public string copy { get; set; } = string.Empty;

    [TextInputComponent(Label = "CTA URL", Order = 4)]
    public string ctaUrl { get; set; } = string.Empty;
}

public class EnquireWidgetModel
{
    public string heading { get; set; } = string.Empty;
    public string image { get; set; } = string.Empty;
    public string copy { get; set; } = string.Empty;
    public string ctaUrl { get; set; } = string.Empty;
}
