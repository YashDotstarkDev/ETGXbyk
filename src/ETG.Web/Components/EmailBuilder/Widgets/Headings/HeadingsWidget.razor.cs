using CMS.Base;
using CMS.ContentEngine;
using ETG.Core.Content;
using ETG.Web.Components.EmailBuilder.Widgets.Headings;
using Kentico.Content.Web.Mvc;
using Kentico.EmailBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Microsoft.AspNetCore.Components;

[assembly: RegisterEmailWidget(
    identifier: HeadingsWidget.IDENTIFIER,
    name: HeadingsWidget.NAME,
    componentType: typeof(HeadingsWidget),
    PropertiesType = typeof(HeadingsWidgetProperties),
    IconClass = "icon-cogwheel-square",
    Description = "Headings widget.")]

namespace ETG.Web.Components.EmailBuilder.Widgets.Headings;

public partial class HeadingsWidget : ComponentBase
{
    public const string IDENTIFIER = "ETG.Web.EmailWidget.Headings";
    public const string NAME = "Headings";

    [Inject]
    private IContentRetriever ContentRetriever { get; set; } = default!;

    [Parameter]
    public HeadingsWidgetProperties Properties { get; set; } = new();

    protected HeadingsWidgetModel Model { get; set; } = new();

    public override Task SetParametersAsync(ParameterView parameters) =>
        ContextUtils.PropagateCurrent(async () => await base.SetParametersAsync(parameters))();

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        Model = new HeadingsWidgetModel();
    }
}

public class HeadingsWidgetProperties : IEmailWidgetProperties
{
    // No properties - static widget
}

public class HeadingsWidgetModel
{
    // No properties - static widget
}
