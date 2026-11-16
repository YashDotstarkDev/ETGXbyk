using CMS.Base;
using CMS.ContentEngine;
using ETG.Core.Content;
using ETG.Web.Components.EmailBuilder.Widgets.TwoColumnSection;
using Kentico.Content.Web.Mvc;
using Kentico.EmailBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Microsoft.AspNetCore.Components;

[assembly: RegisterEmailWidget(
    identifier: TwoColumnSectionWidget.IDENTIFIER,
    name: TwoColumnSectionWidget.NAME,
    componentType: typeof(TwoColumnSectionWidget),
    PropertiesType = typeof(TwoColumnSectionWidgetProperties),
    IconClass = "icon-cogwheel-square",
    Description = "Displays a two-column section layout with images, subtitles, titles, and individual call-to-action buttons.")]

namespace ETG.Web.Components.EmailBuilder.Widgets.TwoColumnSection;

public partial class TwoColumnSectionWidget : ComponentBase
{
    public const string IDENTIFIER = "ETG.Web.EmailWidget.TwoColumnSection";
    public const string NAME = "Two Column Section";

    [Inject]
    private IContentRetriever ContentRetriever { get; set; } = default!;

    [Parameter]
    public TwoColumnSectionWidgetProperties Properties { get; set; } = new();

    protected TwoColumnSectionWidgetModel Model { get; set; } = new();

    public override Task SetParametersAsync(ParameterView parameters) =>
        ContextUtils.PropagateCurrent(async () => await base.SetParametersAsync(parameters))();

    protected override async Task OnInitializedAsync()
    {
        var image1Guid = Properties.Image?.FirstOrDefault()?.Identifier ?? Guid.Empty;
        var image2Guid = Properties.Image2?.FirstOrDefault()?.Identifier ?? Guid.Empty;

        var guids = new[] { image1Guid, image2Guid }.Where(g => g != Guid.Empty).Distinct().ToList();

        var mediaDict = new Dictionary<Guid, MediaFile>();
        if (guids.Count > 0)
        {
            var files = await ContentRetriever.RetrieveContentByGuids<MediaFile>(guids);
            mediaDict = files.GroupBy(f => f.SystemFields.ContentItemGUID).ToDictionary(g => g.Key, g => g.First());
        }

        string imageUrl1 = image1Guid != Guid.Empty && mediaDict.TryGetValue(image1Guid, out var mf1)
            ? mf1.LegacyMediaFileAsset?.Url ?? string.Empty
            : string.Empty;

        string imageUrl2 = image2Guid != Guid.Empty && mediaDict.TryGetValue(image2Guid, out var mf2)
            ? mf2.LegacyMediaFileAsset?.Url ?? string.Empty
            : string.Empty;

        Model = new TwoColumnSectionWidgetModel
        {
            ImageUrl1 = imageUrl1 ?? string.Empty,
            Title1 = Properties.Title ?? string.Empty,
            SubTitle1 = Properties.SubTitle ?? string.Empty,
            CtaLabel1 = Properties.CtaLabel ?? string.Empty,
            CtaUrl1 = Properties.CTAUrl ?? string.Empty,

            ImageUrl2 = imageUrl2 ?? string.Empty,
            Title2 = Properties.Title2 ?? string.Empty,
            SubTitle2 = Properties.SubTitle2 ?? string.Empty,
            CtaLabel2 = Properties.CTALabel2 ?? string.Empty,
            CtaUrl2 = Properties.CTAUrl2 ?? string.Empty
        };
    }
}

public class TwoColumnSectionWidgetProperties : IEmailWidgetProperties
{
    // Section 1
    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Section 1 - Image",
        MaximumItems = 1,
        DefaultViewMode = ContentItemSelectorViewMode.Grid,
        AllowContentItemCreation = true,
        Order = 1)]
    public IEnumerable<ContentItemReference> Image { get; set; } = [];

    [TextInputComponent(Label = "Section 1 - Title", Order = 2)]
    public string Title { get; set; } = string.Empty;

    [TextInputComponent(Label = "Section 1 - Sub Title", Order = 3)]
    public string SubTitle { get; set; } = string.Empty;

    [TextInputComponent(Label = "Section 1 - CTA Label", Order = 4)]
    public string CtaLabel { get; set; } = string.Empty;

    [TextInputComponent(Label = "Section 1 - CTA Url", Order = 5)]
    public string CTAUrl { get; set; } = string.Empty;

    // Section 2
    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Section 2 - Image",
        MaximumItems = 1,
        DefaultViewMode = ContentItemSelectorViewMode.Grid,
        AllowContentItemCreation = true,
        Order = 6)]
    public IEnumerable<ContentItemReference> Image2 { get; set; } = [];

    [TextInputComponent(Label = "Section 2 - Title", Order = 7)]
    public string Title2 { get; set; } = string.Empty;

    [TextInputComponent(Label = "Section 2 - Sub Title", Order = 8)]
    public string SubTitle2 { get; set; } = string.Empty;

    [TextInputComponent(Label = "Section 2 - CTA Label", Order = 9)]
    public string CTALabel2 { get; set; } = string.Empty;

    [TextInputComponent(Label = "Section 2 - CTA Url", Order = 10)]
    public string CTAUrl2 { get; set; } = string.Empty;
}

public class TwoColumnSectionWidgetModel
{
    public string ImageUrl1 { get; set; } = string.Empty;
    public string Title1 { get; set; } = string.Empty;
    public string SubTitle1 { get; set; } = string.Empty;
    public string CtaLabel1 { get; set; } = string.Empty;
    public string CtaUrl1 { get; set; } = string.Empty;

    public string ImageUrl2 { get; set; } = string.Empty;
    public string Title2 { get; set; } = string.Empty;
    public string SubTitle2 { get; set; } = string.Empty;
    public string CtaLabel2 { get; set; } = string.Empty;
    public string CtaUrl2 { get; set; } = string.Empty;
}