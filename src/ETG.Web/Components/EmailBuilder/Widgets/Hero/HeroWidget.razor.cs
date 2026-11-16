using CMS.Base;
using CMS.ContentEngine;
using ETG.Core.Content;
using ETG.Web.Components.EmailBuilder.Widgets.Hero;
using Kentico.Components.Web.Mvc.FormComponents;
using Kentico.Content.Web.Mvc;
using Kentico.EmailBuilder.Web.Mvc;
using Kentico.Forms.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Microsoft.AspNetCore.Components;

[assembly: RegisterEmailWidget(
    identifier: HeroWidget.IDENTIFIER,
    name: HeroWidget.NAME,
    componentType: typeof(HeroWidget),
    PropertiesType = typeof(HeroWidgetProperties),
    IconClass = "icon-cogwheel-square",
    Description = "Hero widget with image, heading, copy, and CTA button.")]

namespace ETG.Web.Components.EmailBuilder.Widgets.Hero;

public partial class HeroWidget : ComponentBase
{
    public const string IDENTIFIER = "ETG.Web.EmailWidget.Hero";
    public const string NAME = "Hero";

    [Inject]
    private IContentRetriever ContentRetriever { get; set; } = default!;

    [Parameter]
    public HeroWidgetProperties Properties { get; set; } = new();

    protected HeroWidgetModel Model { get; set; } = new();

    public override Task SetParametersAsync(ParameterView parameters) =>
        ContextUtils.PropagateCurrent(async () => await base.SetParametersAsync(parameters))();

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        Properties ??= new HeroWidgetProperties();

        var imageGuid = Properties?.image?.FirstOrDefault()?.Identifier ?? Guid.Empty;

        string imageUrl = string.Empty;
        if (imageGuid != Guid.Empty)
        {
            var files = await ContentRetriever.RetrieveContentByGuids<MediaFile>(new[] { imageGuid });
            imageUrl = files?.FirstOrDefault()?.LegacyMediaFileAsset?.Url ?? string.Empty;
        }

        Model = new HeroWidgetModel
        {
            image = imageUrl,
            heading = Properties?.heading ?? string.Empty,
            copy = Properties?.copy ?? string.Empty,
            ctaCopy = Properties?.ctaCopy ?? string.Empty,
            ctaUrl = Properties?.ctaUrl ?? string.Empty
        };
    }
}

public class HeroWidgetProperties : IEmailWidgetProperties
{
    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Image",
        MaximumItems = 1,
        DefaultViewMode = ContentItemSelectorViewMode.Grid,
        AllowContentItemCreation = true,
        Order = 1)]
    public IEnumerable<ContentItemReference> image { get; set; } = [];

    [TextInputComponent(Label = "Heading", Order = 2)]
    public string heading { get; set; } = string.Empty;

    [RichTextEditorComponent(Label = "Copy", Order = 3)]
    public string copy { get; set; } = string.Empty;

    [TextInputComponent(Label = "CTA Copy", Order = 4)]
    public string ctaCopy { get; set; } = string.Empty;

    [TextInputComponent(Label = "CTA URL", Order = 5)]
    public string ctaUrl { get; set; } = string.Empty;
}

public class HeroWidgetModel
{
    public string image { get; set; } = string.Empty;
    public string heading { get; set; } = string.Empty;
    public string copy { get; set; } = string.Empty;
    public string ctaCopy { get; set; } = string.Empty;
    public string ctaUrl { get; set; } = string.Empty;
}
