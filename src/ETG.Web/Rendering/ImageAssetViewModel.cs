using CMS.ContentEngine;
using CSharpFunctionalExtensions;
using ETG.Core.Content;

namespace ETG.Web.Rendering;

public class ImageAssetViewModel
{
    public Guid ID { get; init; }
    public string LegacyMediaFileTitle { get; }
    public string LegacyMediaFileDescription { get; }
    public ContentItemAsset LegacyMediaFileAsset { get; }

    public static ImageAssetViewModel Create(MediaFile content) => new(content);

    public ImageAssetViewModel(MediaFile content)
    {
        ID = content.SystemFields.ContentItemGUID;
        LegacyMediaFileTitle = content.LegacyMediaFileTitle;
        LegacyMediaFileDescription = content.LegacyMediaFileDescription;
        LegacyMediaFileAsset = content.LegacyMediaFileAsset;
    }
}