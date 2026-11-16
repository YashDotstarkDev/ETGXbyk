using CMS.Base;
using CMS.ContentEngine;
using ETG.Core.Content;
using ETG.Web.Components.EmailBuilder.Widgets.LogoArea;
using Kentico.Content.Web.Mvc;
using Kentico.EmailBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Microsoft.AspNetCore.Components;

[assembly: RegisterEmailWidget(
    identifier: LogoAreaWidget.IDENTIFIER,
    name: LogoAreaWidget.NAME,
    componentType: typeof(LogoAreaWidget),
    PropertiesType = typeof(LogoAreaWidgetProperties),
    IconClass = "icon-cogwheel-square",
    Description = "Logo Area widget.")]

namespace ETG.Web.Components.EmailBuilder.Widgets.LogoArea;

public partial class LogoAreaWidget : ComponentBase
{
    public const string IDENTIFIER = "ETG.Web.EmailWidget.LogoArea";
    public const string NAME = "Logo Area";

    [Inject]
    private IContentRetriever ContentRetriever { get; set; } = default!;

    [Parameter]
    public LogoAreaWidgetProperties Properties { get; set; } = new();

    protected LogoAreaWidgetModel Model { get; set; } = new();

    public override Task SetParametersAsync(ParameterView parameters) =>
        ContextUtils.PropagateCurrent(async () => await base.SetParametersAsync(parameters))();

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        Properties ??= new LogoAreaWidgetProperties();

        Model = new LogoAreaWidgetModel
        {
            copy = Properties?.copy ?? string.Empty,
        };
    }
}

public class LogoAreaWidgetProperties : IEmailWidgetProperties
{
    [TextAreaComponent(Label = "Copy", Order = 1)]
    public string copy { get; set; } = string.Empty;
}

public class LogoAreaWidgetModel
{
    public string copy { get; set; } = string.Empty;
}
