using CMS.Base;
using CMS.ContentEngine;
using ETG.Core.Content;
using ETG.Web.Components.EmailBuilder.Widgets.IntroWhite;
using Kentico.Content.Web.Mvc;
using Kentico.EmailBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Microsoft.AspNetCore.Components;

[assembly: RegisterEmailWidget(
    identifier: IntroWhiteWidget.IDENTIFIER,
    name: IntroWhiteWidget.NAME,
    componentType: typeof(IntroWhiteWidget),
    PropertiesType = typeof(IntroWhiteWidgetProperties),
    IconClass = "icon-cogwheel-square",
    Description = "Intro White widget.")]

namespace ETG.Web.Components.EmailBuilder.Widgets.IntroWhite;

public partial class IntroWhiteWidget : ComponentBase
{
    public const string IDENTIFIER = "ETG.Web.EmailWidget.IntroWhite";
    public const string NAME = "Intro White";

    [Inject]
    private IContentRetriever ContentRetriever { get; set; } = default!;

    [Parameter]
    public IntroWhiteWidgetProperties Properties { get; set; } = new();

    protected IntroWhiteWidgetModel Model { get; set; } = new();

    public override Task SetParametersAsync(ParameterView parameters) =>
        ContextUtils.PropagateCurrent(async () => await base.SetParametersAsync(parameters))();

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        Properties ??= new IntroWhiteWidgetProperties();

        Model = new IntroWhiteWidgetModel
        {
            heading = Properties?.heading ?? string.Empty,
            copy = Properties?.copy ?? string.Empty,
        };
    }
}

public class IntroWhiteWidgetProperties : IEmailWidgetProperties
{
    [TextInputComponent(Label = "Heading", Order = 1)]
    public string heading { get; set; } = string.Empty;

    [TextAreaComponent(Label = "Copy", Order = 2)]
    public string copy { get; set; } = string.Empty;
}

public class IntroWhiteWidgetModel
{
    public string heading { get; set; } = string.Empty;
    public string copy { get; set; } = string.Empty;
}
