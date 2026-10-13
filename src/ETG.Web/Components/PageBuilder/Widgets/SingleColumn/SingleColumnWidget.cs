using ETG.Core.Components;
using ETG.Web.Components.PageBuilder.Widgets.SingleColumn;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Microsoft.AspNetCore.Mvc;
using CSharpFunctionalExtensions;
using XperienceCommunity.KenticoComponentIcons;

[assembly: RegisterWidget(
    identifier: SingleColumnWidget.IDENTIFIER,
    name: "Single Column",
    viewComponentType: typeof(SingleColumnWidget),
    propertiesType: typeof(SingleColumnWidgetProperties),
    Description = "Single column content with optional CTA.",
    IconClass = KenticoIcons.COLLAPSE_SCHEME,
    AllowCache = true)]

namespace ETG.Web.Components.PageBuilder.Widgets.SingleColumn;

public class SingleColumnWidget : ViewComponent
{
    public const string IDENTIFIER = "ETG.Web.Widget.SingleColumn";

    public const string NAME = "Single Column";

    public IViewComponentResult Invoke(
        ComponentViewModel<SingleColumnWidgetProperties> cvm)
    {
        var props = cvm?.Properties;

        return Validate(props)
            .Match(
                vm => View("~/Components/PageBuilder/Widgets/SingleColumn/SingleColumn.cshtml", vm),
                vm => View("~/Components/ComponentError.cshtml", vm));
    }

    private static Result<SingleColumnWidgetViewModel, ComponentErrorViewModel> Validate(SingleColumnWidgetProperties props)
    {
        if (props == null || string.IsNullOrWhiteSpace(props.Content))
        {
            return Result.Failure<SingleColumnWidgetViewModel, ComponentErrorViewModel>(
                new ComponentErrorViewModel(NAME, ComponentType.Widget, "No content has been set for this widget."));
        }

        return new SingleColumnWidgetViewModel(props);
    }
}

public class SingleColumnWidgetProperties : BaseWidgetProperties
{
    [TextInputComponent(
        Label = "Large Content",
        Order = 1)]
    public string LargeContent { get; set; } = "";

    [TextAreaComponent(
        Label = "Content",
        Order = 2)]
    public string Content { get; set; } = "";

    [TextInputComponent(
        Label = "CTA Label",
        Order = 3)]
    public string CTALabel { get; set; } = "";

    [TextInputComponent(
        Label = "CTA Url",
        Order = 4)]
    public string CTAUrl { get; set; } = "";
}

public class SingleColumnWidgetViewModel(SingleColumnWidgetProperties props)
{
    public string LargeContent { get; set; } = props.LargeContent;
    public string Content { get; set; } = props.Content;
    public string CTALabel { get; set; } = props.CTALabel;
    public string CTAUrl { get; set; } = props.CTAUrl;
}