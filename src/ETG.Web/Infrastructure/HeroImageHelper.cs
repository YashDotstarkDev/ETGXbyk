using System.Collections.Generic;
using System.Linq;
using ETG;
using ETG.Core.Content;
using ETG.Core.Models;

namespace ETG.Web.Infrastructure;

public static class HeroImageHelper
{
    public static List<HeroGalleryImageItem> ExtractGalleryImages(IEnumerable<ImageFolder>? folders)
    {
        if (folders is null)
        {
            return [];
        }

        return folders
            .Where(folder => folder?.SelectedImages != null)
            .SelectMany(folder => folder.SelectedImages)
            .OfType<ETG.Image>()
            .Where(img => img != null)
            .Select(img =>
            {
                var mediaFile = img.ImageFilePath?
                    .OfType<MediaFile>()
                    .FirstOrDefault();

                var url = mediaFile?.LegacyMediaFileAsset?.Url ?? "";
                return new HeroGalleryImageItem(
                    url,
                    img.ImageAltText ?? "",
                    img.ImageCaption ?? ""
                );
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.ImageUrl))
            .ToList();
    }
}
