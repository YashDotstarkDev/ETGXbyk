using System;
using System.Text.RegularExpressions;
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
        var cleanId = ExtractYoutubeId(props.YoutubeID);
        if (string.IsNullOrWhiteSpace(cleanId))
        {
            return Result.Failure<YoutubeVideoWidgetViewModel, ComponentErrorViewModel>(
                new ComponentErrorViewModel(
                    NAME,
                    ComponentType.Widget,
                    "A valid YouTube Video ID or URL is required."));
        }

        return new YoutubeVideoWidgetViewModel(props, cleanId);
    }

    public static string ExtractYoutubeId(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var trimmed = input.Trim();

        // 1. Direct ID (standard YouTube video IDs are 11 characters)
        if (Regex.IsMatch(trimmed, @"^[\w-]{11}$"))
        {
            return trimmed;
        }

        // 2. Extract from various YouTube URL formats (watch, embed, youtu.be, shorts, live)
        var match = Regex.Match(
            trimmed,
            @"(?:youtu\.be\/|youtube\.com\/(?:embed\/|v\/|watch\?v=|watch\?.+&v=|shorts\/|live\/))([\w-]{11})",
            RegexOptions.IgnoreCase);

        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        // 3. Fallback: Parse query parameter if present
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var v = query["v"];
            if (!string.IsNullOrWhiteSpace(v) && Regex.IsMatch(v, @"^[\w-]{11}$"))
            {
                return v;
            }
        }

        return string.Empty;
    }
}

[FormCategory(Label = "Content", Order = 1)]
public class YoutubeVideoWidgetProperties : BaseWidgetProperties
{
    [TextInputComponent(Label = "Youtube ID or URL", ExplanationText = "Enter a YouTube video ID (e.g. rH-_aU-LXpw) or full YouTube URL", Order = 2)]
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
    public string VideoUrl { get; }
    public string EmbedUrl { get; }
    public string Caption { get; }
    public bool OpenInNewWindow { get; }

    public YoutubeVideoWidgetViewModel(
        YoutubeVideoWidgetProperties props,
        string cleanId)
    {
        YoutubeID = cleanId;
        VideoUrl = !string.IsNullOrWhiteSpace(cleanId)
            ? $"https://www.youtube.com/watch?v={cleanId}"
            : string.Empty;
        EmbedUrl = !string.IsNullOrWhiteSpace(cleanId)
            ? $"https://www.youtube.com/embed/{cleanId}"
            : string.Empty;
        Caption = props.Caption;
        OpenInNewWindow = props.OpenInNewWindow;
    }
}