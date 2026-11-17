
using CMS.Base;
using CMS.ContentEngine;
using CMS.EmailMarketing;
using ETG.Core.Content;
using ETG.Web.Components.EmailBuilder.Widgets.WelcomeSection;
using Kentico.Components.Web.Mvc.FormComponents;
using Kentico.Content.Web.Mvc;
using Kentico.EmailBuilder.Web.Mvc;
using Kentico.Forms.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Microsoft.AspNetCore.Components;
using System.Linq;
using System.Text.Encodings.Web;

[assembly: RegisterEmailWidget(
    identifier: WelcomeSectionWidget.IDENTIFIER,
    name: WelcomeSectionWidget.NAME,
    componentType: typeof(WelcomeSectionWidget),
    PropertiesType = typeof(WelcomeSectionWidgetProperties),
    IconClass = "icon-cogwheel-square",
    Description = "Welcome Section widget.")]

namespace ETG.Web.Components.EmailBuilder.Widgets.WelcomeSection;

public partial class WelcomeSectionWidget : ComponentBase
{
    public const string IDENTIFIER = "ETG.Web.EmailWidget.WelcomeSection";
    public const string NAME = "Welcome Section";

    [Inject]
    private IContentRetriever ContentRetriever { get; set; } = default!;

    [Inject]
    private IEmailRecipientContextAccessor RecipientContextAccessor { get; set; } = default!;

    [Parameter]
    public WelcomeSectionWidgetProperties Properties { get; set; } = new();

    protected WelcomeSectionWidgetModel Model { get; set; } = new();

    public override Task SetParametersAsync(ParameterView parameters) =>
        ContextUtils.PropagateCurrent(async () => await base.SetParametersAsync(parameters))();

    private string PersonalizeContent(string? content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return string.Empty;
        }

        var recipient = RecipientContextAccessor.GetContext();

        var firstName = HtmlEncoder.Default.Encode(
            recipient.FirstName ?? string.Empty);

        return content.Replace(
            "{{FirstName}}",
            firstName,
            StringComparison.OrdinalIgnoreCase);
    }

    protected override async Task OnInitializedAsync()
    {
        var imageGuid = Properties.Image?.FirstOrDefault()?.Identifier ?? Guid.Empty;

        var guids = new[] { imageGuid }
            .Where(g => g != Guid.Empty)
            .Distinct()
            .ToList();

        var mediaDict = new Dictionary<Guid, MediaFile>();

        if (guids.Count > 0)
        {
            var files = await ContentRetriever.RetrieveContentByGuids<MediaFile>(guids);

            mediaDict = files.ToDictionary(
                f => f.SystemFields.ContentItemGUID);
        }

        string imageUrl = string.Empty;

        if (imageGuid != Guid.Empty &&
            mediaDict.TryGetValue(imageGuid, out var mediaFile))
        {
            imageUrl = mediaFile.LegacyMediaFileAsset?.Url ?? string.Empty;
        }

        Model = new WelcomeSectionWidgetModel
        {
            Heading = Properties.Heading ?? string.Empty,
            Image = imageUrl,

            // Personalize Rich Text content
            Intro = PersonalizeContent(Properties.Intro),
            Copy = PersonalizeContent(Properties.Copy),

            PrimaryCTALabel = Properties.PrimaryCTALabel ?? string.Empty,
            PrimaryCTAUrl = Properties.PrimaryCTAUrl ?? string.Empty,
            SecondaryCTALabel = Properties.SecondaryCTALabel ?? string.Empty,
            SecondaryCTAUrl = Properties.SecondaryCTAUrl ?? string.Empty,
        };
    }
}

public class WelcomeSectionWidgetProperties : IEmailWidgetProperties
{
    [TextInputComponent(Label = "Heading", Order = 1)]
    public string Heading { get; set; } = string.Empty;

    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Image",
        MaximumItems = 1,
        DefaultViewMode = ContentItemSelectorViewMode.Grid,
        AllowContentItemCreation = true,
        Order = 2)]
    public IEnumerable<ContentItemReference> Image { get; set; } = [];

    [RichTextEditorComponent(Label = "Intro", Order = 3)]
    public string Intro { get; set; } = string.Empty;

    [RichTextEditorComponent(Label = "Copy", Order = 4)]
    public string Copy { get; set; } = string.Empty;

    [TextInputComponent(Label = "Primary CTA Label", Order = 5)]
    public string PrimaryCTALabel { get; set; } = string.Empty;

    [TextInputComponent(Label = "Primary CTA Url", Order = 6)]
    public string PrimaryCTAUrl { get; set; } = string.Empty;

    [TextInputComponent(Label = "Secondary Cta Label", Order = 7)]
    public string SecondaryCTALabel { get; set; } = string.Empty;

    [TextInputComponent(Label = "Secondary CTA Url", Order = 8)]
    public string SecondaryCTAUrl { get; set; } = string.Empty;
}

public class WelcomeSectionWidgetModel
{
    public string Heading { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public string Intro { get; set; } = string.Empty;
    public string Copy { get; set; } = string.Empty;
    public string PrimaryCTALabel { get; set; } = string.Empty;
    public string PrimaryCTAUrl { get; set; } = string.Empty;
    public string SecondaryCTALabel { get; set; } = string.Empty;
    public string SecondaryCTAUrl { get; set; } = string.Empty;
}
