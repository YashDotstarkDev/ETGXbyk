using CMS.Base;
using CMS.ContentEngine;
using ETG.Core.Content;
using ETG.Web.Components.EmailBuilder.Widgets.Spacer;
using Kentico.Content.Web.Mvc;
using Kentico.EmailBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Microsoft.AspNetCore.Components;

[assembly: RegisterEmailWidget(
    identifier: SpacerWidget.IDENTIFIER,
    name: SpacerWidget.NAME,
    componentType: typeof(SpacerWidget),
    PropertiesType = typeof(SpacerWidgetProperties),
    IconClass = "icon-cogwheel-square",
    Description = "Spacer widget.")]

namespace ETG.Web.Components.EmailBuilder.Widgets.Spacer;

public partial class SpacerWidget : ComponentBase
{
    public const string IDENTIFIER = "ETG.Web.EmailWidget.Spacer";
    public const string NAME = "Spacer";

    [Inject]
    private IContentRetriever ContentRetriever { get; set; } = default!;

    [Parameter]
    public SpacerWidgetProperties Properties { get; set; } = new();

    protected SpacerWidgetModel Model { get; set; } = new();

    public override Task SetParametersAsync(ParameterView parameters) =>
        ContextUtils.PropagateCurrent(async () => await base.SetParametersAsync(parameters))();

    protected override Task OnInitializedAsync()
    {
        Model = new SpacerWidgetModel();
        return Task.CompletedTask;
    }
}

public class SpacerWidgetProperties : IEmailWidgetProperties
{
    // No properties - static spacer widget
}

public class SpacerWidgetModel
{
    // No properties - static spacer widget
}
