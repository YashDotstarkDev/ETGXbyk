using System.ComponentModel;
using CSharpFunctionalExtensions;
using ETG.Core.Components;
using ETG.Web.Components.PageBuilder.Widgets.NotificationBanner;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Websites.FormAnnotations;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using XperienceCommunity.KenticoComponentIcons;

[assembly: RegisterWidget(
    identifier: NotificationBannerWidget.IDENTIFIER,
    viewComponentType: typeof(NotificationBannerWidget),
    name: NotificationBannerWidget.NAME,
    propertiesType: typeof(NotificationBannerWidgetProperties),
    Description = "Displays a configurable notification banner with optional CTA, dismissal, and scheduled visibility.",
    IconClass = KenticoIcons.I,
    AllowCache = false)]

namespace ETG.Web.Components.PageBuilder.Widgets.NotificationBanner;

public class NotificationBannerWidget : ViewComponent
{
    public const string IDENTIFIER = "Website.NotificationBannerWidget";
    public const string NAME = "Notification Banner";

    public IViewComponentResult Invoke(ComponentViewModel<NotificationBannerWidgetProperties> cvm)
    {
        var props = cvm.Properties;

        return Validate(props)
            .Match(
                vm => View("~/Components/PageBuilder/Widgets/NotificationBanner/NotificationBanner.cshtml", vm),
                vm => View("~/Components/ComponentError.cshtml", vm));
    }

    private static Result<NotificationBannerWidgetViewModel, ComponentErrorViewModel> Validate(
        NotificationBannerWidgetProperties props)
    {
        if (string.IsNullOrWhiteSpace(props.Title) && string.IsNullOrWhiteSpace(props.Message))
        {
            return Result.Failure<NotificationBannerWidgetViewModel, ComponentErrorViewModel>(
                new ComponentErrorViewModel(NAME, ComponentType.Widget, "A title or message is required for the notification banner."));
        }

        var vm = new NotificationBannerWidgetViewModel(props);

        // In live mode, suppress rendering entirely if outside the scheduled window.
        // The view handles this by checking IsWithinSchedule; validation always passes
        // so editors can still see the banner in Page Builder edit mode.
        return vm;
    }
}

[FormCategory(Label = "Content", Order = 1)]
[FormCategory(Label = "Display", Order = 3)]
[FormCategory(Label = "Schedule", Order = 5)]
public class NotificationBannerWidgetProperties : BaseWidgetProperties
{
    // ── Content ────────────────────────────────────────────────────────────────

    [TextInputComponent(
        Label = "Title",
        ExplanationText = "Bold heading displayed at the start of the banner. Leave empty to show message only.",
        WatermarkText = "Important notice",
        Order = 2)]
    public string Title { get; set; } = "";

    [RichTextEditorComponent(
        Label = "Message",
        ExplanationText = "Main body of the notification. Supports inline formatting.",
        Order = 3)]
    public string Message { get; set; } = "";

    [TextInputComponent(
        Label = "CTA Text",
        ExplanationText = "Label for the call-to-action link. Leave empty to hide the button.",
        WatermarkText = "Learn more",
        Order = 4)]
    public string CtaText { get; set; } = "";

    [UrlSelectorComponent(
        Label = "CTA URL",
        Order = 5)]
    public string CtaUrl { get; set; } = "";

    // ── Display ────────────────────────────────────────────────────────────────

    [DropDownComponent(
        Label = "Banner type",
        ExplanationText = "Sets the colour and icon of the banner.",
        DataProviderType = typeof(EnumDropDownOptionsProvider<BannerType>),
        Order = 4)]
    public string Type { get; set; } = nameof(BannerType.Info);
    public BannerType TypeParsed => EnumDropDownOptionsProvider<BannerType>.Parse(Type, BannerType.Info);

    [CheckBoxComponent(
        Label = "Dismissible",
        ExplanationText = "Show a close button so visitors can hide the banner.",
        Order = 5)]
    public bool Dismissible { get; set; } = true;

    [TextInputComponent(
        Label = "Custom CSS class",
        ExplanationText = "Optional extra CSS class(es) applied to the banner wrapper.",
        WatermarkText = "my-custom-class",
        Order = 6)]
    public string CustomCssClass { get; set; } = "";

    // ── Schedule ───────────────────────────────────────────────────────────────

    [TextInputComponent(
        Label = "Start date",
        ExplanationText = "ISO 8601 date/time when the banner becomes visible (e.g. 2026-06-01T09:00:00). Leave empty for no start restriction.",
        WatermarkText = "2026-06-01T09:00:00",
        Order = 6)]
    public string StartDate { get; set; } = "";

    [TextInputComponent(
        Label = "End date",
        ExplanationText = "ISO 8601 date/time after which the banner is hidden (e.g. 2026-12-31T23:59:59). Leave empty for no end restriction.",
        WatermarkText = "2026-12-31T23:59:59",
        Order = 7)]
    public string EndDate { get; set; } = "";
}

// ── Enums ──────────────────────────────────────────────────────────────────────

public enum BannerType
{
    [Description("Info")]
    Info,
    [Description("Success")]
    Success,
    [Description("Warning")]
    Warning,
    [Description("Error")]
    Error
}

// ── ViewModel ─────────────────────────────────────────────────────────────────

public class NotificationBannerWidgetViewModel : BaseWidgetViewModel
{
    protected override string WidgetName { get; } = NotificationBannerWidget.NAME;

    public string Title { get; }
    public HtmlString Message { get; }
    public bool HasTitle { get; }
    public bool HasMessage { get; }
    public string CtaText { get; }
    public string CtaUrl { get; }
    public bool HasCta { get; }
    public BannerType Type { get; }
    public bool Dismissible { get; }
    public string CustomCssClass { get; }

    /// <summary>Unique ID used for the dismiss button's aria-controls and the alert's id attribute.</summary>
    public string AlertId { get; }

    /// <summary>Whether the current UTC time falls within the configured start/end window.</summary>
    public bool IsWithinSchedule { get; }

    /// <summary>Resolved start date, if any (used for display in edit mode).</summary>
    public DateTime? StartDate { get; }

    /// <summary>Resolved end date, if any (used for display in edit mode).</summary>
    public DateTime? EndDate { get; }

    public NotificationBannerWidgetViewModel(NotificationBannerWidgetProperties props)
    {
        Title = props.Title;
        Message = new HtmlString(props.Message);
        HasTitle = !string.IsNullOrWhiteSpace(props.Title);
        HasMessage = !string.IsNullOrWhiteSpace(props.Message);
        CtaText = props.CtaText;
        CtaUrl = props.CtaUrl;
        HasCta = !string.IsNullOrWhiteSpace(props.CtaText);
        Type = props.TypeParsed;
        Dismissible = props.Dismissible;
        CustomCssClass = props.CustomCssClass;
        AlertId = $"notification-banner-{props.ID}";

        // Parse optional schedule dates
        if (DateTime.TryParse(props.StartDate, out var start))
        {
            StartDate = start;
        }

        if (DateTime.TryParse(props.EndDate, out var end))
        {
            EndDate = end;
        }

        var now = DateTime.UtcNow;
        bool afterStart = StartDate == null || now >= StartDate.Value.ToUniversalTime();
        bool beforeEnd = EndDate == null || now <= EndDate.Value.ToUniversalTime();
        IsWithinSchedule = afterStart && beforeEnd;
    }

    /// <summary>Maps the BannerType to a Bootstrap alert CSS class.</summary>
    public string AlertClass => Type switch
    {
        BannerType.Success => "alert-success",
        BannerType.Warning => "alert-warning",
        BannerType.Error => "alert-danger",
        BannerType.Info or _ => "alert-info"
    };

    /// <summary>Appropriate Bootstrap icon class for each type.</summary>
    public string IconClass => Type switch
    {
        BannerType.Success => "bi bi-check-circle-fill",
        BannerType.Warning => "bi bi-exclamation-triangle-fill",
        BannerType.Error => "bi bi-x-circle-fill",
        BannerType.Info or _ => "bi bi-info-circle-fill"
    };

    /// <summary>Human-readable type label used in edit-mode schedule tooltip.</summary>
    public string TypeLabel => Type switch
    {
        BannerType.Success => "Success",
        BannerType.Warning => "Warning",
        BannerType.Error => "Error",
        BannerType.Info or _ => "Info"
    };
}
