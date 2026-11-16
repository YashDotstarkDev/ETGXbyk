using CMS.Base;
using CMS.ContentEngine;
using ETG.Core.Content;
using ETG.Web.Components.EmailBuilder.Widgets.CompetitionCountdown;
using Kentico.Content.Web.Mvc;
using Kentico.EmailBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Microsoft.AspNetCore.Components;

[assembly: RegisterEmailWidget(
    identifier: CompetitionCountdownWidget.IDENTIFIER,
    name: CompetitionCountdownWidget.NAME,
    componentType: typeof(CompetitionCountdownWidget),
    PropertiesType = typeof(CompetitionCountdownWidgetProperties),
    IconClass = "icon-cogwheel-square",
    Description = "Competition Countdown widget.")]

namespace ETG.Web.Components.EmailBuilder.Widgets.CompetitionCountdown;

public partial class CompetitionCountdownWidget : ComponentBase
{
    public const string IDENTIFIER = "ETG.Web.EmailWidget.CompetitionCountdown";
    public const string NAME = "Competition Countdown";

    [Inject]
    private IContentRetriever ContentRetriever { get; set; } = default!;

    [Parameter]
    public CompetitionCountdownWidgetProperties Properties { get; set; } = new();

    protected CompetitionCountdownWidgetModel Model { get; set; } = new();

    public override Task SetParametersAsync(ParameterView parameters) =>
        ContextUtils.PropagateCurrent(async () => await base.SetParametersAsync(parameters))();

    protected override async Task OnInitializedAsync()
    {
        Model = new CompetitionCountdownWidgetModel
        {
            Heading = Properties?.Heading ?? string.Empty,
            TimerUrl = Properties?.TimerUrl ?? string.Empty,
            Width = Properties?.Width ?? string.Empty,
        };
    }
}

public class CompetitionCountdownWidgetProperties : IEmailWidgetProperties
{
    [TextInputComponent(Label = "Heading", Order = 1)]
    public string Heading { get; set; } = string.Empty;

    [TextAreaComponent(Label = "TimerUrl", Order = 2)]
    public string TimerUrl { get; set; } = string.Empty;

    [TextInputComponent(Label = "Image Width (Default: 380)", Order = 3)]
    public string Width { get; set; } = string.Empty;
}

public class CompetitionCountdownWidgetModel
{
    public string Heading { get; set; } = string.Empty;
    public string TimerUrl { get; set; } = string.Empty;
    public string Width { get; set; } = string.Empty;
}
