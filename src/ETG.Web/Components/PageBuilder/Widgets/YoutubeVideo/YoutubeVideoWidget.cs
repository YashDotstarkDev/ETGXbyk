using CSharpFunctionalExtensions;
using ETG.Core.Components;
using ETG.Web.Components.PageBuilder.Widgets.YoutubeVideo;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Microsoft.AspNetCore.Mvc;
using XperienceCommunity.KenticoComponentIcons;

[assembly: RegisterWidget(
    identifier: YoutubeVideoWidget.IDENTIFIER,
    viewComponentType: typeof(YoutubeVideoWidget),
    name: YoutubeVideoWidget.NAME,
    propertiesType: typeof(YoutubeVideoWidgetProperties),
    Description = "Adds a YouTube video to the page",
    IconClass = KenticoIcons.BRAND_YOUTUBE)]

namespace ETG.Web.Components.PageBuilder.Widgets.YoutubeVideo;

public class YoutubeVideoWidget : ViewComponent
{
    public const string IDENTIFIER = "ETG.Web.Widget.YoutubeVideo";
    public const string NAME = "Youtube Video";

    public Task<IViewComponentResult> InvokeAsync(
        ComponentViewModel<YoutubeVideoWidgetProperties> cvm)
    {
        var props = cvm.Properties;

        IViewComponentResult result = Validate(props)
            .Match(
                vm => View(
                    "~/Components/PageBuilder/Widgets/YoutubeVideo/YoutubeVideo.cshtml",
                    vm),
                vm => View("~/Components/ComponentError.cshtml", vm));

        return Task.FromResult(result);
    }

    private static Result<YoutubeVideoWidgetViewModel, ComponentErrorViewModel> Validate(
        YoutubeVideoWidgetProperties props)
    {
        if (string.IsNullOrWhiteSpace(props.YoutubeID))
        {
            return Result.Failure<YoutubeVideoWidgetViewModel, ComponentErrorViewModel>(
                new ComponentErrorViewModel(
                    NAME,
                    ComponentType.Widget,
                    "YouTube ID is required."));
        }

        return new YoutubeVideoWidgetViewModel(props);
    }
}

[FormCategory(Label = "Content", Order = 1)]
public class YoutubeVideoWidgetProperties : BaseWidgetProperties
{
    [TextInputComponent(Label = "Youtube ID", Order = 2)]
    public string YoutubeID { get; set; } = string.Empty;

    [TextInputComponent(Label = "Caption", Order = 3)]
    public string Caption { get; set; } = string.Empty;

    [CheckBoxComponent(Label = "Open In New Window", Order = 4)]
    public bool OpenInNewWindow { get; set; }
}

public class YoutubeVideoWidgetViewModel : BaseWidgetViewModel
{
    protected override string WidgetName { get; } = YoutubeVideoWidget.NAME;

    public string YoutubeID { get; }
    public string Caption { get; }
    public bool OpenInNewWindow { get; }

    public YoutubeVideoWidgetViewModel(
        YoutubeVideoWidgetProperties props)
    {
        YoutubeID = props.YoutubeID;
        Caption = props.Caption;
        OpenInNewWindow = props.OpenInNewWindow;
    }
}