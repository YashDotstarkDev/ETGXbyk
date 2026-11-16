using CMS.Base;
using CMS.ContentEngine;
using ETG.Core.Content;
using ETG.Web.Components.EmailBuilder.Widgets.Navigation;
using Kentico.Content.Web.Mvc;
using Kentico.EmailBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Microsoft.AspNetCore.Components;

[assembly: RegisterEmailWidget(
    identifier: NavigationWidget.IDENTIFIER,
    name: NavigationWidget.NAME,
    componentType: typeof(NavigationWidget),
    PropertiesType = typeof(NavigationWidgetProperties),
    IconClass = "icon-cogwheel-square",
    Description = "Navigation widget.")]

namespace ETG.Web.Components.EmailBuilder.Widgets.Navigation;

public partial class NavigationWidget : ComponentBase
{
    public const string IDENTIFIER = "ETG.Web.EmailWidget.Navigation";
    public const string NAME = "Navigation";

    [Inject]
    private IContentRetriever ContentRetriever { get; set; } = default!;

    [Parameter]
    public NavigationWidgetProperties Properties { get; set; } = new();

    protected NavigationWidgetModel Model { get; set; } = new();

    public override Task SetParametersAsync(ParameterView parameters) =>
        ContextUtils.PropagateCurrent(async () => await base.SetParametersAsync(parameters))();

    protected override async Task OnInitializedAsync()
    {
        Model = new NavigationWidgetModel
        {
            NavigationItem1 = Properties.NavigationItem1 ?? string.Empty,
            NavigationItem1Url = Properties.NavigationItem1Url ?? string.Empty,
            NavigationItem2 = Properties.NavigationItem2 ?? string.Empty,
            NavigationItem2Url = Properties.NavigationItem2Url ?? string.Empty,
            NavigationItem3 = Properties.NavigationItem3 ?? string.Empty,
            NavigationItem3Url = Properties.NavigationItem3Url ?? string.Empty,
            NavigationItem4 = Properties.NavigationItem4 ?? string.Empty,
            NavigationItem4Url = Properties.NavigationItem4Url ?? string.Empty,
        };
    }
}

public class NavigationWidgetProperties : IEmailWidgetProperties
{
    [TextInputComponent(Label = "Navigation Item 1", Order = 1)]
    public string NavigationItem1 { get; set; } = string.Empty;

    [TextInputComponent(Label = "Navigation Item 1 Url", Order = 2)]
    public string NavigationItem1Url { get; set; } = string.Empty;

    [TextInputComponent(Label = "Navigation Item 2", Order = 3)]
    public string NavigationItem2 { get; set; } = string.Empty;

    [TextInputComponent(Label = "Navigation Item 2 Url", Order = 4)]
    public string NavigationItem2Url { get; set; } = string.Empty;

    [TextInputComponent(Label = "Navigation Item 3", Order = 5)]
    public string NavigationItem3 { get; set; } = string.Empty;

    [TextInputComponent(Label = "Navigation Item 3 Url", Order = 6)]
    public string NavigationItem3Url { get; set; } = string.Empty;

    [TextInputComponent(Label = "Navigation Item 4", Order = 7)]
    public string NavigationItem4 { get; set; } = string.Empty;

    [TextInputComponent(Label = "Navigation Item 4 Url", Order = 8)]
    public string NavigationItem4Url { get; set; } = string.Empty;
}

public class NavigationWidgetModel
{
    public string NavigationItem1 { get; set; } = string.Empty;
    public string NavigationItem1Url { get; set; } = string.Empty;
    public string NavigationItem2 { get; set; } = string.Empty;
    public string NavigationItem2Url { get; set; } = string.Empty;
    public string NavigationItem3 { get; set; } = string.Empty;
    public string NavigationItem3Url { get; set; } = string.Empty;
    public string NavigationItem4 { get; set; } = string.Empty;
    public string NavigationItem4Url { get; set; } = string.Empty;
}
