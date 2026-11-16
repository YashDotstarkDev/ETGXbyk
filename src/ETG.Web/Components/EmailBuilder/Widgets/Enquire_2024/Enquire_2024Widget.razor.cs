using CMS.Base;
using CMS.ContentEngine;
using ETG.Core.Content;
using ETG.Web.Components.EmailBuilder.Widgets.Enquire_2024;
using Kentico.Content.Web.Mvc;
using Kentico.EmailBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Microsoft.AspNetCore.Components;

[assembly: RegisterEmailWidget(
    identifier: Enquire_2024Widget.IDENTIFIER,
    name: Enquire_2024Widget.NAME,
    componentType: typeof(Enquire_2024Widget),
    PropertiesType = typeof(Enquire_2024WidgetProperties),
    IconClass = "icon-cogwheel-square",
    Description = "Enquire (2024) widget.")]

namespace ETG.Web.Components.EmailBuilder.Widgets.Enquire_2024;

public partial class Enquire_2024Widget : ComponentBase
{
    public const string IDENTIFIER = "ETG.Web.EmailWidget.Enquire_2024";
    public const string NAME = "Enquire (2024)";

    [Inject]
    private IContentRetriever ContentRetriever { get; set; } = default!;

    [Parameter]
    public Enquire_2024WidgetProperties Properties { get; set; } = new();

    protected Enquire_2024WidgetModel Model { get; set; } = new();

    public override Task SetParametersAsync(ParameterView parameters) =>
        ContextUtils.PropagateCurrent(async () => await base.SetParametersAsync(parameters))();

    protected override async Task OnInitializedAsync()
    {
        Model = new Enquire_2024WidgetModel
        {
            Heading = Properties.Heading,
            CtaLabel = Properties.CtaLabel,
            CTAUrl = Properties.CTAUrl,
        };
    }
}

public class Enquire_2024WidgetProperties : IEmailWidgetProperties
{
    [TextInputComponent(Label = "Heading", Order = 1)]
    public string Heading { get; set; } = string.Empty;

    [TextInputComponent(Label = "CTA Label", Order = 2)]
    public string CtaLabel { get; set; } = string.Empty;

    [TextInputComponent(Label = "CTA Url", Order = 3)]
    public string CTAUrl { get; set; } = string.Empty;
}

public class Enquire_2024WidgetModel
{
    public string Heading { get; set; } = string.Empty;
    public string CtaLabel { get; set; } = string.Empty;
    public string CTAUrl { get; set; } = string.Empty;
}
