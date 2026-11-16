using CMS.Base;
using CMS.ContentEngine;
using ETG.Core.Content;
using ETG.Web.Components.EmailBuilder.Widgets.ExploreDestination;
using Kentico.Content.Web.Mvc;
using Kentico.EmailBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Microsoft.AspNetCore.Components;

[assembly: RegisterEmailWidget(
    identifier: ExploreDestinationWidget.IDENTIFIER,
    name: ExploreDestinationWidget.NAME,
    componentType: typeof(ExploreDestinationWidget),
    PropertiesType = typeof(ExploreDestinationWidgetProperties),
    IconClass = "icon-cogwheel-square",
    Description = "Explore Destination widget.")]

namespace ETG.Web.Components.EmailBuilder.Widgets.ExploreDestination;

public partial class ExploreDestinationWidget : ComponentBase
{
    public const string IDENTIFIER = "ETG.Web.EmailWidget.ExploreDestination";
    public const string NAME = "Explore Destination";

    [Inject]
    private IContentRetriever ContentRetriever { get; set; } = default!;

    [Parameter]
    public ExploreDestinationWidgetProperties Properties { get; set; } = new();

    protected ExploreDestinationWidgetModel Model { get; set; } = new();

    public override Task SetParametersAsync(ParameterView parameters) =>
        ContextUtils.PropagateCurrent(async () => await base.SetParametersAsync(parameters))();

    protected override Task OnInitializedAsync()
    {
        Model = new ExploreDestinationWidgetModel();
        return Task.CompletedTask;
    }
}

public class ExploreDestinationWidgetProperties : IEmailWidgetProperties
{
    // No properties - static widget
}

public class ExploreDestinationWidgetModel
{
    // No properties - static widget
}
