using CMS.Base;
using CMS.ContentEngine;
using ETG.Core.Content;
using ETG.Web.Components.EmailBuilder.Widgets.Announcements;
using Kentico.Content.Web.Mvc;
using Kentico.EmailBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Microsoft.AspNetCore.Components;

[assembly: RegisterEmailWidget(
    identifier: AnnouncementsWidget.IDENTIFIER,
    name: AnnouncementsWidget.NAME,
    componentType: typeof(AnnouncementsWidget),
    PropertiesType = typeof(AnnouncementsWidgetProperties),
    IconClass = "icon-bullhorn",
    Description = "Displays announcements with configurable titles, descriptions, images, and CTAs.")]

namespace ETG.Web.Components.EmailBuilder.Widgets.Announcements;

public partial class AnnouncementsWidget : ComponentBase
{
    public const string IDENTIFIER = "ETG.Web.EmailWidget.Announcements";
    public const string NAME = "Announcements";

    [Inject]
    private IContentRetriever ContentRetriever { get; set; } = default!;

    [Parameter]
    public AnnouncementsWidgetProperties Properties { get; set; } = new();

    protected AnnouncementsWidgetModel Model { get; set; } = new();

    public override Task SetParametersAsync(ParameterView parameters) =>
        ContextUtils.PropagateCurrent(async () => await base.SetParametersAsync(parameters))();

    protected override async Task OnInitializedAsync()
    {
        Properties ??= new AnnouncementsWidgetProperties();

        var image1Guid = Properties.Image1?.FirstOrDefault()?.Identifier ?? Guid.Empty;
        var image2Guid = Properties.Image2?.FirstOrDefault()?.Identifier ?? Guid.Empty;

        var guids = new[] { image1Guid, image2Guid }
            .Where(g => g != Guid.Empty)
            .Distinct()
            .ToList();

        var mediaDict = new Dictionary<Guid, MediaFile>();

        if (guids.Count > 0)
        {
            if (ContentRetriever is null)
            {
                throw new InvalidOperationException(
                    "ContentRetriever has not been initialized.");
            }

            var files = await ContentRetriever
                .RetrieveContentByGuids<MediaFile>(guids);

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

        string image1Url =
            image1Guid != Guid.Empty &&
            mediaDict.TryGetValue(image1Guid, out var mf1)
                ? mf1.LegacyMediaFileAsset?.Url ?? string.Empty
                : string.Empty;

        string image2Url =
            image2Guid != Guid.Empty &&
            mediaDict.TryGetValue(image2Guid, out var mf2)
                ? mf2.LegacyMediaFileAsset?.Url ?? string.Empty
                : string.Empty;

        Model = new AnnouncementsWidgetModel
        {
            Heading = Properties.Heading ?? string.Empty,
            Title1 = Properties.Title1 ?? string.Empty,
            Description1 = Properties.Description1 ?? string.Empty,
            Image1Url = image1Url,
            CTALabel1 = Properties.CTALabel1 ?? string.Empty,
            CTAUrl1 = Properties.CTAUrl1 ?? string.Empty,
            Title2 = Properties.Title2 ?? string.Empty,
            Description2 = Properties.Description2 ?? string.Empty,
            Image2Url = image2Url,
            CTALabel2 = Properties.CTALabel2 ?? string.Empty,
            CTAUrl2 = Properties.CTAUrl2 ?? string.Empty
        };
    }
}

public class AnnouncementsWidgetProperties : IEmailWidgetProperties
{
    [TextInputComponent(Label = "Heading", Order = 1)]
    public string Heading { get; set; } = string.Empty;

    [TextInputComponent(Label = "Announcement 1 - Title", Order = 2)]
    public string Title1 { get; set; } = string.Empty;

    [TextAreaComponent(Label = "Announcement 1 - Description", Order = 3)]
    public string Description1 { get; set; } = string.Empty;

    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Announcement 1 - Image",
        MaximumItems = 1,
        DefaultViewMode = ContentItemSelectorViewMode.Grid,
        AllowContentItemCreation = true,
        Order = 4)]
    public IEnumerable<ContentItemReference> Image1 { get; set; } = [];

    [TextInputComponent(Label = "Announcement 1 - CTA Label", Order = 5)]
    public string CTALabel1 { get; set; } = string.Empty;

    [TextInputComponent(Label = "Announcement 1 - CTA Url", Order = 6)]
    public string CTAUrl1 { get; set; } = string.Empty;

    [TextInputComponent(Label = "Announcement 2 - Title", Order = 7)]
    public string Title2 { get; set; } = string.Empty;

    [TextAreaComponent(Label = "Announcement 2 - Description", Order = 8)]
    public string Description2 { get; set; } = string.Empty;

    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Announcement 2 - Image",
        MaximumItems = 1,
        DefaultViewMode = ContentItemSelectorViewMode.Grid,
        AllowContentItemCreation = true,
        Order = 9)]
    public IEnumerable<ContentItemReference> Image2 { get; set; } = [];

    [TextInputComponent(Label = "Announcement 2 - CTA Label", Order = 10)]
    public string CTALabel2 { get; set; } = string.Empty;

    [TextInputComponent(Label = "Announcement 2 - CTA Url", Order = 11)]
    public string CTAUrl2 { get; set; } = string.Empty;
}

public class AnnouncementsWidgetModel
{
    public string Heading { get; set; } = string.Empty;

    public string Title1 { get; set; } = string.Empty;
    public string Description1 { get; set; } = string.Empty;
    public string Image1Url { get; set; } = string.Empty;
    public string CTALabel1 { get; set; } = string.Empty;
    public string CTAUrl1 { get; set; } = string.Empty;

    public string Title2 { get; set; } = string.Empty;
    public string Description2 { get; set; } = string.Empty;
    public string Image2Url { get; set; } = string.Empty;
    public string CTALabel2 { get; set; } = string.Empty;
    public string CTAUrl2 { get; set; } = string.Empty;
}