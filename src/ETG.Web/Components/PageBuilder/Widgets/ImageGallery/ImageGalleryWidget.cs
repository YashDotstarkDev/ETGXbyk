using CMS.ContentEngine;
using CSharpFunctionalExtensions;
using ETG.Core.Components;
using ETG.Core.Content;
using ETG.Web.Components.PageBuilder.Widgets.ImageGallery;
using ETG.Web.Rendering;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Websites.FormAnnotations;
using Microsoft.AspNetCore.Mvc;
using XperienceCommunity.KenticoComponentIcons;

[assembly: RegisterWidget(
    identifier: ImageGalleryWidget.IDENTIFIER,
    viewComponentType: typeof(ImageGalleryWidget),
    name: ImageGalleryWidget.NAME,
    propertiesType: typeof(ImageGalleryProperties),
    Description = "Displays a mosaic image gallery with a lightbox.",
    IconClass = KenticoIcons.L_HEADER_LIST_IMG,
    AllowCache = true)]

namespace ETG.Web.Components.PageBuilder.Widgets.ImageGallery;

public class ImageGalleryWidget(IContentRetriever contentRetriever) : ViewComponent
{
    public const string IDENTIFIER = "ETG.Web.Widget.ImageGallery";
    public const string NAME = "Image Gallery";

    private readonly IContentRetriever contentRetriever = contentRetriever;

    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<ImageGalleryProperties> cvm)
    {
        var props = cvm.Properties;

        // The widget now stores the GUID(s) of ImageFolder items, not MediaFile items.
        var folderIds = props.ImagePaths?
            .Where(item => item.Identifier != Guid.Empty)
            .Select(item => item.Identifier)
            .ToList() ?? [];

        if (folderIds.Count == 0)
        {
            return View("~/Components/ComponentError.cshtml",
                new ComponentErrorViewModel(
                    NAME, ComponentType.Widget, "No valid images have been selected."));
        }

        // Levels: ImageFolder -> Image -> MediaFile (the actual asset)
        var folders = await contentRetriever
            .RetrieveContentByGuids<ImageFolder>(
                folderIds,
                new RetrieveContentParameters
                {
                    LinkedItemsMaxLevel = 2
                });

        var images = folders
            .SelectMany(folder => folder.SelectedImages ?? [])
            .SelectMany(image => image.ImageFilePath ?? [])
            .OfType<MediaFile>()
            .ToList();

        return Validate(props, images)
            .Match(
                vm => View("~/Components/PageBuilder/Widgets/ImageGallery/ImageGallery.cshtml", vm),
                vm => View("~/Components/ComponentError.cshtml", vm));
    }

    private static Result<ImageGalleryWidgetViewModel, ComponentErrorViewModel> Validate(
        ImageGalleryProperties props,
        IEnumerable<MediaFile> images)
    {
        if (!props.ImagePaths.Any())
        {
            return Result.Failure<ImageGalleryWidgetViewModel, ComponentErrorViewModel>(
                new ComponentErrorViewModel(NAME, ComponentType.Widget, "No images have been selected."));
        }

        var imageViewModels = images
            .Where(image => image.LegacyMediaFileAsset is not null)
            .Select(ImageAssetViewModel.Create)
            .ToList();

        if (!imageViewModels.Any())
        {
            return Result.Failure<ImageGalleryWidgetViewModel, ComponentErrorViewModel>(
                new ComponentErrorViewModel(NAME, ComponentType.Widget, "The selected content items or image files no longer exist."));
        }

        return new ImageGalleryWidgetViewModel(imageViewModels);
    }
}


[FormCategory(Label = "Content", Order = 1)]
public class ImageGalleryProperties : BaseWidgetProperties
{
    // Selector now points at the folder type (name kept as ImagePaths so saved data keeps its key).
    [ContentItemSelectorComponent(
        ImageFolder.CONTENT_TYPE_NAME,
        Label = "Images",
        MinimumItems = 1,
        MaximumItems = 50,
        DefaultViewMode =
        Kentico.Xperience.Admin.Base.Forms.ContentItemSelectorViewMode.Grid,
        AllowContentItemCreation = true,
        Order = 1)]
    public IEnumerable<ContentItemReference> ImagePaths { get; set; } = [];
}


public class ImageGalleryWidgetViewModel : BaseWidgetViewModel
{
    protected override string WidgetName => ImageGalleryWidget.NAME;
    public IReadOnlyList<ImageAssetViewModel> Images { get; }
    public ImageGalleryWidgetViewModel(IReadOnlyList<ImageAssetViewModel> images)
    {
        Images = images;
    }
}