using CMS.Base;
using CMS.ContentEngine;
using ETG.Core.Content;
using ETG.Web.Components.EmailBuilder.Widgets.Inspiration;
using Kentico.Content.Web.Mvc;
using Kentico.EmailBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Microsoft.AspNetCore.Components;

[assembly: RegisterEmailWidget(
    identifier: InspirationWidget.IDENTIFIER,
    name: InspirationWidget.NAME,
    componentType: typeof(InspirationWidget),
    PropertiesType = typeof(InspirationWidgetProperties),
    IconClass = "icon-cogwheel-square",
    Description = "Displays an inspiration layout featuring a top main heading, two side-by-side cards, and a bottom banner card.")]

namespace ETG.Web.Components.EmailBuilder.Widgets.Inspiration;

public partial class InspirationWidget : ComponentBase
{
    public const string IDENTIFIER = "ETG.Web.EmailWidget.Inspiration";
    public const string NAME = "Inspiration";

    [Inject]
    private IContentRetriever ContentRetriever { get; set; } = default!;

    [Parameter]
    public InspirationWidgetProperties Properties { get; set; } = new();

    protected InspirationWidgetModel Model { get; set; } = new();

    public override Task SetParametersAsync(ParameterView parameters) =>
        ContextUtils.PropagateCurrent(async () => await base.SetParametersAsync(parameters))();

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        Properties ??= new InspirationWidgetProperties();

        var leftImageGuid = Properties.LeftImage?.FirstOrDefault()?.Identifier ?? Guid.Empty;
        var rightImageGuid = Properties.RightImage?.FirstOrDefault()?.Identifier ?? Guid.Empty;
        var bottomImageGuid = Properties.BottomImage?.FirstOrDefault()?.Identifier ?? Guid.Empty;

        var guids = new[] { leftImageGuid, rightImageGuid, bottomImageGuid }.Where(g => g != Guid.Empty).Distinct().ToList();

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

        string leftImageUrl = leftImageGuid != Guid.Empty && mediaDict.TryGetValue(leftImageGuid, out var mfLeft)
            ? mfLeft.LegacyMediaFileAsset?.Url ?? string.Empty
            : string.Empty;

        string rightImageUrl = rightImageGuid != Guid.Empty && mediaDict.TryGetValue(rightImageGuid, out var mfRight)
            ? mfRight.LegacyMediaFileAsset?.Url ?? string.Empty
            : string.Empty;

        string bottomImageUrl = bottomImageGuid != Guid.Empty && mediaDict.TryGetValue(bottomImageGuid, out var mfBottom)
            ? mfBottom.LegacyMediaFileAsset?.Url ?? string.Empty
            : string.Empty;

        Model = new InspirationWidgetModel
        {
            MainHeading = Properties?.MainHeading ?? string.Empty,

            LeftTitle = Properties?.LeftTitle ?? string.Empty,
            LeftImageUrl = leftImageUrl,
            LeftHeading = Properties?.LeftHeading ?? string.Empty,
            LeftCopy = Properties?.LeftCopy ?? string.Empty,
            LeftCtaCopy = Properties?.LeftCtaCopy ?? string.Empty,
            LeftCtaUrl = Properties?.LeftCtaUrl ?? string.Empty,

            RightTitle = Properties?.RightTitle ?? string.Empty,
            RightImageUrl = rightImageUrl,
            RightHeading = Properties?.RightHeading ?? string.Empty,
            RightCopy = Properties?.RightCopy ?? string.Empty,
            RightCtaCopy = Properties?.RightCtaCopy ?? string.Empty,
            RightCtaUrl = Properties?.RightCtaUrl ?? string.Empty,

            BottomTitle = Properties?.BottomTitle ?? string.Empty,
            BottomImageUrl = bottomImageUrl,
            BottomLead = Properties?.BottomLead ?? string.Empty,
            BottomCopy = Properties?.BottomCopy ?? string.Empty,
            BottomCtaCopy = Properties?.BottomCtaCopy ?? string.Empty,
            BottomCtaUrl = Properties?.BottomCtaUrl ?? string.Empty
        };
    }
}

public class InspirationWidgetProperties : IEmailWidgetProperties
{
    [TextInputComponent(Label = "Main heading", Order = 1)]
    public string MainHeading { get; set; } = string.Empty;

    // Left Card
    [TextInputComponent(Label = "Left Title", Order = 2)]
    public string LeftTitle { get; set; } = string.Empty;

    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Left Image",
        MaximumItems = 1,
        DefaultViewMode = ContentItemSelectorViewMode.Grid,
        AllowContentItemCreation = true,
        Order = 3)]
    public IEnumerable<ContentItemReference> LeftImage { get; set; } = [];

    [TextInputComponent(Label = "Left Heading", Order = 4)]
    public string LeftHeading { get; set; } = string.Empty;

    [TextAreaComponent(Label = "Left Copy", Order = 5)]
    public string LeftCopy { get; set; } = string.Empty;

    [TextInputComponent(Label = "Left CTA Copy", Order = 6)]
    public string LeftCtaCopy { get; set; } = string.Empty;

    [TextInputComponent(Label = "Left CTA URL", Order = 7)]
    public string LeftCtaUrl { get; set; } = string.Empty;

    // Right Card
    [TextInputComponent(Label = "Right Title", Order = 8)]
    public string RightTitle { get; set; } = string.Empty;

    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Right Image",
        MaximumItems = 1,
        DefaultViewMode = ContentItemSelectorViewMode.Grid,
        AllowContentItemCreation = true,
        Order = 9)]
    public IEnumerable<ContentItemReference> RightImage { get; set; } = [];

    [TextInputComponent(Label = "Right Heading", Order = 10)]
    public string RightHeading { get; set; } = string.Empty;

    [TextAreaComponent(Label = "Right Copy", Order = 11)]
    public string RightCopy { get; set; } = string.Empty;

    [TextInputComponent(Label = "Right CTA Copy", Order = 12)]
    public string RightCtaCopy { get; set; } = string.Empty;

    [TextInputComponent(Label = "Right CTA URL", Order = 13)]
    public string RightCtaUrl { get; set; } = string.Empty;

    // Bottom Card
    [TextInputComponent(Label = "Bottom Title", Order = 14)]
    public string BottomTitle { get; set; } = string.Empty;

    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Bottom Image",
        MaximumItems = 1,
        DefaultViewMode = ContentItemSelectorViewMode.Grid,
        AllowContentItemCreation = true,
        Order = 15)]
    public IEnumerable<ContentItemReference> BottomImage { get; set; } = [];

    [TextAreaComponent(Label = "Bottom Lead", Order = 16)]
    public string BottomLead { get; set; } = string.Empty;

    [TextAreaComponent(Label = "Bottom Copy", Order = 17)]
    public string BottomCopy { get; set; } = string.Empty;

    [TextInputComponent(Label = "Bottom CTA Copy", Order = 18)]
    public string BottomCtaCopy { get; set; } = string.Empty;

    [TextInputComponent(Label = "Bottom CTA URL", Order = 19)]
    public string BottomCtaUrl { get; set; } = string.Empty;
}

public class InspirationWidgetModel
{
    public string MainHeading { get; set; } = string.Empty;

    public string LeftTitle { get; set; } = string.Empty;
    public string LeftImageUrl { get; set; } = string.Empty;
    public string LeftHeading { get; set; } = string.Empty;
    public string LeftCopy { get; set; } = string.Empty;
    public string LeftCtaCopy { get; set; } = string.Empty;
    public string LeftCtaUrl { get; set; } = string.Empty;

    public string RightTitle { get; set; } = string.Empty;
    public string RightImageUrl { get; set; } = string.Empty;
    public string RightHeading { get; set; } = string.Empty;
    public string RightCopy { get; set; } = string.Empty;
    public string RightCtaCopy { get; set; } = string.Empty;
    public string RightCtaUrl { get; set; } = string.Empty;

    public string BottomTitle { get; set; } = string.Empty;
    public string BottomImageUrl { get; set; } = string.Empty;
    public string BottomLead { get; set; } = string.Empty;
    public string BottomCopy { get; set; } = string.Empty;
    public string BottomCtaCopy { get; set; } = string.Empty;
    public string BottomCtaUrl { get; set; } = string.Empty;

    public bool HasLeftCard =>
        !string.IsNullOrWhiteSpace(LeftTitle) ||
        !string.IsNullOrWhiteSpace(LeftImageUrl) ||
        !string.IsNullOrWhiteSpace(LeftHeading) ||
        !string.IsNullOrWhiteSpace(LeftCopy) ||
        !string.IsNullOrWhiteSpace(LeftCtaCopy);

    public bool HasRightCard =>
        !string.IsNullOrWhiteSpace(RightTitle) ||
        !string.IsNullOrWhiteSpace(RightImageUrl) ||
        !string.IsNullOrWhiteSpace(RightHeading) ||
        !string.IsNullOrWhiteSpace(RightCopy) ||
        !string.IsNullOrWhiteSpace(RightCtaCopy);

    public bool HasBottomCard =>
        !string.IsNullOrWhiteSpace(BottomTitle) ||
        !string.IsNullOrWhiteSpace(BottomImageUrl) ||
        !string.IsNullOrWhiteSpace(BottomLead) ||
        !string.IsNullOrWhiteSpace(BottomCopy) ||
        !string.IsNullOrWhiteSpace(BottomCtaCopy);
}