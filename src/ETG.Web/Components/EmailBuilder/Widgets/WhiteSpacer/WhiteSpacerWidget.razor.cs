using CMS.Base;
using CMS.ContentEngine;
using ETG.Core.Content;
using ETG.Web.Components.EmailBuilder.Widgets.WhiteSpacer;
using Kentico.Content.Web.Mvc;
using Kentico.EmailBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Microsoft.AspNetCore.Components;

[assembly: RegisterEmailWidget(
    identifier: WhiteSpacerWidget.IDENTIFIER,
    name: WhiteSpacerWidget.NAME,
    componentType: typeof(WhiteSpacerWidget),
    PropertiesType = typeof(WhiteSpacerWidgetProperties),
    IconClass = "icon-cogwheel-square",
    Description = "White Spacer widget.")]

namespace ETG.Web.Components.EmailBuilder.Widgets.WhiteSpacer;

public partial class WhiteSpacerWidget : ComponentBase
{
    public const string IDENTIFIER = "ETG.Web.EmailWidget.WhiteSpacer";
    public const string NAME = "White Spacer";

    [Inject]
    private IContentRetriever ContentRetriever { get; set; } = default!;

    [Parameter]
    public WhiteSpacerWidgetProperties Properties { get; set; } = new();

    protected WhiteSpacerWidgetModel Model { get; set; } = new();

    public override Task SetParametersAsync(ParameterView parameters) =>
        ContextUtils.PropagateCurrent(async () => await base.SetParametersAsync(parameters))();

    protected override Task OnInitializedAsync()
    {
        Model = new WhiteSpacerWidgetModel();
        return Task.CompletedTask;
    }
}

public class WhiteSpacerWidgetProperties : IEmailWidgetProperties
{
    // No properties - static spacer widget
}

public class WhiteSpacerWidgetModel
{
    // No properties - static spacer widget
}
