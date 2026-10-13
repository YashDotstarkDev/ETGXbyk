using CMS.ContentEngine;
using CSharpFunctionalExtensions;
using ETG.Core.Components;
using ETG.Core.Content;
using ETG.Web.Components.PageBuilder.Widgets.Image;
using ETG.Web.Components.PageBuilder.Widgets.YoutubeVideo;
using ETG.Web.Rendering;
using Kentico.Content.Web.Mvc;
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

public class YoutubeVideoWidget(IContentRetriever contentRetriever) : ViewComponent
{
    public const string IDENTIFIER = "ETG.Web.Widget.YoutubeVideo";
    public const string NAME = "Youtube Video";

    private readonly IContentRetriever contentRetriever = contentRetriever;

    public async Task<IViewComponentResult> InvokeAsync(
        ComponentViewModel<YoutubeVideoWidgetProperties> cvm)
    {
        var props = cvm.Properties;

        var imageReferences = props.Image?
            .Where(image => image.Identifier != Guid.Empty)
            .ToList() ?? [];

        Maybe<MediaFile> image = Maybe<MediaFile>.None;

        if (imageReferences.Count > 0)
        {
            image = await contentRetriever
                .RetrieveContentByGuids<MediaFile>(
                    imageReferences.Select(image => image.Identifier))
                .TryFirst();
        }

        return Validate(props, image)
            .Match(
                vm => View(
                    "~/Components/PageBuilder/Widgets/YoutubeVideo/YoutubeVideo.cshtml",
                    vm),
                vm => View("~/Components/ComponentError.cshtml", vm));
    }

    private static Result<YoutubeVideoWidgetViewModel, ComponentErrorViewModel> Validate(
        YoutubeVideoWidgetProperties props,
        Maybe<MediaFile> image)
    {
        if (string.IsNullOrWhiteSpace(props.YoutubeID))
        {
            return Result.Failure<YoutubeVideoWidgetViewModel, ComponentErrorViewModel>(
                new ComponentErrorViewModel(
                    NAME,
                    ComponentType.Widget,
                    "YouTube ID is required."));
        }
       
        ImageAssetViewModel? imageViewModel = null;

        if (image.TryGetValue(out var selectedImage) &&
            selectedImage.LegacyMediaFileAsset is not null)
        {
            imageViewModel = ImageAssetViewModel.Create(selectedImage);
        }

        return new YoutubeVideoWidgetViewModel(imageViewModel, props);
    }
}

[FormCategory(Label = "Content", Order = 1)]
public class YoutubeVideoWidgetProperties : BaseWidgetProperties
{
    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Image",
        MaximumItems = 1,
        AllowContentItemCreation = true,
        Order = 1)]
    public IEnumerable<ContentItemReference> Image { get; set; } = [];

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

    public ImageAssetViewModel? Image { get; }
    public string YoutubeID { get; }
    public string Caption { get; }
    public bool OpenInNewWindow { get; }

    public YoutubeVideoWidgetViewModel(
        ImageAssetViewModel? image,
        YoutubeVideoWidgetProperties props)
    {
        Image = image;
        YoutubeID = props.YoutubeID;
        Caption = props.Caption;
        OpenInNewWindow = props.OpenInNewWindow;
    }
}