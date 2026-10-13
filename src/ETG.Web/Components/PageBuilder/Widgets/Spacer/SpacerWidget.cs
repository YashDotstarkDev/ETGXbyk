using CSharpFunctionalExtensions;
using ETG.Core.Components;
using ETG.Web.Components.PageBuilder.Widgets.Spacer;
using Kentico.Forms.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Microsoft.AspNetCore.Mvc;
using XperienceCommunity.KenticoComponentIcons;

[assembly: RegisterWidget(
    identifier: SpacerWidget.IDENTIFIER,
    name: "Spacer",
    viewComponentType: typeof(SpacerWidget),
    propertiesType: typeof(SpacerWidgetProperties),
    Description = "Adds vertical spacing between sections.",
    IconClass = KenticoIcons.ARROW_DOWN,
    AllowCache = true)]

namespace ETG.Web.Components.PageBuilder.Widgets.Spacer;

public class SpacerWidget : ViewComponent
{
    public const string IDENTIFIER = "ETG.Web.Widget.SpacerWidget";
    public const string NAME = "Spacer";

    public IViewComponentResult Invoke(ComponentViewModel<SpacerWidgetProperties> vm)
    {
        var props = vm.Properties;

        return Validate(props)
           .Match(
               model => View("~/Components/PageBuilder/Widgets/Spacer/Spacer.cshtml", model),
               error => View("~/Components/ComponentError.cshtml", error));
    }

    private static Result<SpacerWidgetViewModel, ComponentErrorViewModel> Validate(
           SpacerWidgetProperties props)
    {
        if (props == null || string.IsNullOrWhiteSpace(props.Padding.ToString()))
        {
            return Result.Failure<SpacerWidgetViewModel, ComponentErrorViewModel>(
                new ComponentErrorViewModel(NAME, ComponentType.Widget,
                    "The Spacer widget properties could not be loaded."));
        }

        // Validate allowed padding values
        if (props.Padding != 0 &&
            props.Padding != 20 &&
            props.Padding != 40 &&
            props.Padding != 60)
        {
            return Result.Failure<SpacerWidgetViewModel, ComponentErrorViewModel>(
                new ComponentErrorViewModel(
                    NAME,
                    ComponentType.Widget,
                    "Invalid padding value. Only 0, 20, 40, or 60 can be used."));
        }

        return new SpacerWidgetViewModel(props);
    }
}

public class SpacerWidgetProperties : BaseWidgetProperties
{
    [EditingComponent(
        IntInputComponent.IDENTIFIER,
        Order = 1,
        Label = "Padding",
        ExplanationText = "By default only 0, 20, 40, and 60 can be used")]
    public int Padding { get; set; } = 0;
}

public class SpacerWidgetViewModel(SpacerWidgetProperties props)
{
    public int Padding { get; set; } = props.Padding;
}