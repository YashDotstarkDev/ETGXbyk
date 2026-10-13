using ETG.Core.Components;
using ETG.Web.Components.PageBuilder.Widgets.HtmlText;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Microsoft.AspNetCore.Mvc;
using CSharpFunctionalExtensions;
using XperienceCommunity.KenticoComponentIcons;

[assembly: RegisterWidget(
    identifier: HtmlTextWidget.IDENTIFIER,
    name: "Html Text",
    viewComponentType: typeof(HtmlTextWidget),
    propertiesType: typeof(HtmlTextWidgetProperties),
    Description = "Displays custom HTML content with optional side padding.",
    IconClass = KenticoIcons.XML_TAG,
    AllowCache = true)]

namespace ETG.Web.Components.PageBuilder.Widgets.HtmlText;

public class HtmlTextWidget : ViewComponent
{
    public const string IDENTIFIER = "ETG.Web.Widget.HtmlTextWidget";
    public const string NAME = "Html Text";

    public IViewComponentResult Invoke(ComponentViewModel<HtmlTextWidgetProperties> cvm)
    {
        var props = cvm?.Properties;

        return Validate(props)
            .Match(
                vm => View("~/Components/PageBuilder/Widgets/HtmlText/HtmlText.cshtml", vm),
                vm => View("~/Components/ComponentError.cshtml", vm));
    }

    private static Result<HtmlTextWidgetViewModel, ComponentErrorViewModel> Validate(HtmlTextWidgetProperties props)
    {
        if (props == null || string.IsNullOrWhiteSpace(props.Text))
        {
            return Result.Failure<HtmlTextWidgetViewModel, ComponentErrorViewModel>(
                new ComponentErrorViewModel(NAME, ComponentType.Widget, "No text has been set for this widget."));
        }

        return new HtmlTextWidgetViewModel(props);
    }
}

public class HtmlTextWidgetProperties : BaseWidgetProperties
{
    [TextAreaComponent(
        Label = "Text",
        Order = 1)]
    public string Text { get; set; } = "";

    [CheckBoxComponent(
        Label = "Add side padding",
        Order = 2)]
    public bool HasSidePadding { get; set; } = false;
}

public class HtmlTextWidgetViewModel(HtmlTextWidgetProperties props)
{
    public string Text { get; set; } = props.Text;
    public bool HasSidePadding { get; set; } = props.HasSidePadding;
}