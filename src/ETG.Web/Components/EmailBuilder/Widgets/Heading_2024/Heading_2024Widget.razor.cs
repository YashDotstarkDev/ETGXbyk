using CMS.Base;
using CMS.ContentEngine;
using ETG.Core.Content;
using ETG.Web.Components.EmailBuilder.Widgets.Heading_2024;
using Kentico.Content.Web.Mvc;
using Kentico.EmailBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Microsoft.AspNetCore.Components;

[assembly: RegisterEmailWidget(
    identifier: Heading_2024Widget.IDENTIFIER,
    name: Heading_2024Widget.NAME,
    componentType: typeof(Heading_2024Widget),
    PropertiesType = typeof(Heading_2024WidgetProperties),
    IconClass = "icon-cogwheel-square",
    Description = "Heading (2024) widget.")]

namespace ETG.Web.Components.EmailBuilder.Widgets.Heading_2024;

public partial class Heading_2024Widget : ComponentBase
{
    public const string IDENTIFIER = "ETG.Web.EmailWidget.Heading_2024";
    public const string NAME = "Heading (2024)";

    [Inject]
    private IContentRetriever ContentRetriever { get; set; } = default!;

    [Parameter]
    public Heading_2024WidgetProperties Properties { get; set; } = new();

    protected Heading_2024WidgetModel Model { get; set; } = new();

    public override Task SetParametersAsync(ParameterView parameters) =>
        ContextUtils.PropagateCurrent(async () => await base.SetParametersAsync(parameters))();

    protected override async Task OnInitializedAsync()
    {
        Model = new Heading_2024WidgetModel
        {
            Heading = Properties.Heading,
        };
    }
}

public class Heading_2024WidgetProperties : IEmailWidgetProperties
{
    [TextInputComponent(Label = "Heading", Order = 1)]
    public string Heading { get; set; } = string.Empty;
}

public class Heading_2024WidgetModel
{
    public string Heading { get; set; } = string.Empty;
}
