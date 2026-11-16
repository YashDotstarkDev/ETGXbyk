using CMS.Base;
using CMS.ContentEngine;
using ETG.Core.Content;
using ETG.Web.Components.EmailBuilder.Widgets.BenefitsBadges;
using Kentico.Content.Web.Mvc;
using Kentico.EmailBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Microsoft.AspNetCore.Components;

[assembly: RegisterEmailWidget(
    identifier: BenefitsBadgesWidget.IDENTIFIER,
    name: BenefitsBadgesWidget.NAME,
    componentType: typeof(BenefitsBadgesWidget),
    PropertiesType = typeof(BenefitsBadgesWidgetProperties),
    IconClass = "icon-cogwheel-square",
    Description = "Benefits Badges widget.")]

namespace ETG.Web.Components.EmailBuilder.Widgets.BenefitsBadges;

public partial class BenefitsBadgesWidget : ComponentBase
{
    public const string IDENTIFIER = "ETG.Web.EmailWidget.BenefitsBadges";
    public const string NAME = "Benefits Badges";

    [Inject]
    private IContentRetriever ContentRetriever { get; set; } = default!;

    [Parameter]
    public BenefitsBadgesWidgetProperties Properties { get; set; } = new();

    protected BenefitsBadgesWidgetModel Model { get; set; } = new();

    public override Task SetParametersAsync(ParameterView parameters) =>
        ContextUtils.PropagateCurrent(async () => await base.SetParametersAsync(parameters))();

    protected override async Task OnInitializedAsync()
    {
        Model = new BenefitsBadgesWidgetModel
        {
            Heading = Properties?.Heading ?? string.Empty,
            CTALabel = Properties?.CTALabel ?? string.Empty,
            CTAUrl = Properties?.CTAUrl ?? string.Empty,
        };
    }
}

public class BenefitsBadgesWidgetProperties : IEmailWidgetProperties
{
    [TextInputComponent(Label = "Heading", Order = 1)]
    public string Heading { get; set; } = string.Empty;

    [TextInputComponent(Label = "CTA Label", Order = 2)]
    public string CTALabel { get; set; } = string.Empty;

    [TextInputComponent(Label = "CTA Url", Order = 3)]
    public string CTAUrl { get; set; } = string.Empty;
}

public class BenefitsBadgesWidgetModel
{
    public string Heading { get; set; } = string.Empty;
    public string CTALabel { get; set; } = string.Empty;
    public string CTAUrl { get; set; } = string.Empty;
}
