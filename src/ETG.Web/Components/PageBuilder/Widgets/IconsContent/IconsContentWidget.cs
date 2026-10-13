using CSharpFunctionalExtensions;
using ETG.Web.Components.PageBuilder.Widgets.IconsContent;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Microsoft.AspNetCore.Mvc;
using XperienceCommunity.KenticoComponentIcons;

[assembly: RegisterWidget(
    identifier: IconsContentWidget.IDENTIFIER,
    name: "Icons Content",
    viewComponentType: typeof(IconsContentWidget),
    propertiesType: typeof(IconsContentWidgetProperties),
    Description = "Displays icon content items.",
    IconClass = KenticoIcons.L_GRID_2_2,
    AllowCache = true)]

namespace ETG.Web.Components.PageBuilder.Widgets.IconsContent;

public class IconsContentWidget : ViewComponent
{
    public const string IDENTIFIER = "ETG.Web.Widget.IconsContent";

    public IViewComponentResult Invoke(ComponentViewModel<IconsContentWidgetProperties> vm)
    {
        var props = vm.Properties;

        return Validate(props)
            .Match(
                model => View("~/Components/PageBuilder/Widgets/IconsContent/IconsContent.cshtml", model),
                error => View("~/Components/ComponentError.cshtml", error)
            );
    }
    private static Result<IconsContentWidgetViewModel, ComponentErrorViewModel> Validate(
        IconsContentWidgetProperties props)
    {
        if (string.IsNullOrWhiteSpace(props.Heading))
        {
            return Result.Failure<IconsContentWidgetViewModel, ComponentErrorViewModel>(
                new ComponentErrorViewModel(
                    "Icons Content",
                    ComponentType.Widget,
                    "Heading is required."
                )
            );
        }

        return new IconsContentWidgetViewModel(props);
    }
}

public class IconsContentWidgetProperties : BaseWidgetProperties
{
    [TextInputComponent(
        Label = "Heading",
        Order = 1)]
    public string Heading { get; set; } = "";

    [TextInputComponent(
        Label = "Icon 1",
        Order = 2)]
    public string Stat1Icon { get; set; } = "";

    [TextInputComponent(
        Label = "Title 1",
        Order = 3)]
    public string Stat1Title { get; set; } = "";

    [TextAreaComponent(
        Label = "Description 1",
        Order = 4)]
    public string Stat1Description { get; set; } = "";

    [TextInputComponent(
        Label = "Icon 2",
        Order = 5)]
    public string Stat2Icon { get; set; } = "";

    [TextInputComponent(
        Label = "Title 2",
        Order = 6)]
    public string Stat2Title { get; set; } = "";

    [TextAreaComponent(
        Label = "Description 2",
        Order = 7)]
    public string Stat2Description { get; set; } = "";

    [TextInputComponent(
        Label = "Icon 3",
        Order = 8)]
    public string Stat3Icon { get; set; } = "";

    [TextInputComponent(
        Label = "Title 3",
        Order = 9)]
    public string Stat3Title { get; set; } = "";

    [TextAreaComponent(
        Label = "Description 3",
        Order = 10)]
    public string Stat3Description { get; set; } = "";

    [TextInputComponent(
        Label = "Icon 4",
        Order = 11)]
    public string Stat4Icon { get; set; } = "";

    [TextInputComponent(
        Label = "Title 4",
        Order = 12)]
    public string Stat4Title { get; set; } = "";

    [TextAreaComponent(
        Label = "Description 4",
        Order = 13)]
    public string Stat4Description { get; set; } = "";
}

public class IconsContentWidgetViewModel(IconsContentWidgetProperties props)
{
    public string Heading { get; set; } = props.Heading;
    public string Stat1Icon { get; set; } = props.Stat1Icon;
    public string Stat1Title { get; set; } = props.Stat1Title;
    public string Stat1Description { get; set; } = props.Stat1Description;
    public string Stat2Icon { get; set; } = props.Stat2Icon;
    public string Stat2Title { get; set; } = props.Stat2Title;
    public string Stat2Description { get; set; } = props.Stat2Description;
    public string Stat3Icon { get; set; } = props.Stat3Icon;
    public string Stat3Title { get; set; } = props.Stat3Title;
    public string Stat3Description { get; set; } = props.Stat3Description;
    public string Stat4Icon { get; set; } = props.Stat4Icon;
    public string Stat4Title { get; set; } = props.Stat4Title;
    public string Stat4Description { get; set; } = props.Stat4Description;
}