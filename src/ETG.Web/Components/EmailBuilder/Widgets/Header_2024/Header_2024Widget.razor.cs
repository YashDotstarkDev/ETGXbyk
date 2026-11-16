using CMS.Base;
using CMS.ContentEngine;
using ETG.Core.Content;
using ETG.Web.Components.EmailBuilder.Widgets.Header_2024;
using Kentico.Content.Web.Mvc;
using Kentico.EmailBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Microsoft.AspNetCore.Components;

[assembly: RegisterEmailWidget(
    identifier: Header_2024Widget.IDENTIFIER,
    name: Header_2024Widget.NAME,
    componentType: typeof(Header_2024Widget),
    PropertiesType = typeof(Header_2024WidgetProperties),
    IconClass = "icon-cogwheel-square",
    Description = "Header (2024) widget.")]

namespace ETG.Web.Components.EmailBuilder.Widgets.Header_2024;

public partial class Header_2024Widget : ComponentBase
{
    public const string IDENTIFIER = "ETG.Web.EmailWidget.Header_2024";
    public const string NAME = "Header (2024)";

    [Inject]
    private IContentRetriever ContentRetriever { get; set; } = default!;

    [Parameter]
    public Header_2024WidgetProperties Properties { get; set; } = new();

    protected Header_2024WidgetModel Model { get; set; } = new();

    public override Task SetParametersAsync(ParameterView parameters) =>
        ContextUtils.PropagateCurrent(async () => await base.SetParametersAsync(parameters))();

    protected override Task OnInitializedAsync()
    {
        Model = new Header_2024WidgetModel();
        return Task.CompletedTask;
    }
}

public class Header_2024WidgetProperties : IEmailWidgetProperties
{
    // No properties - static widget
}

public class Header_2024WidgetModel
{
    // No properties - static widget
}
