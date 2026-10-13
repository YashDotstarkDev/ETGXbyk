using CMS.ContentEngine;
using ETG.Core.Content;
using ETG.Web.Rendering;

namespace ETG.Web.Features.Shared;

/// <summary>
/// View model for displaying author information across Blogs, News, and other modules.
/// Follows the same pattern as ImageAssetViewModel for consistency.
/// </summary>
public class AuthorViewModel
{
    public Guid ID { get; init; }
    public string FirstName { get; init; }
    public string LastName { get; init; }
    public string Biography { get; init; }
    public ImageAssetViewModel Photo { get; init; }

    public string FullName => $"{FirstName} {LastName}".Trim();

    public static AuthorViewModel Create(AuthorContent content) => new(content);

    public AuthorViewModel(AuthorContent content)
    {
        ID = content.SystemFields.ContentItemGUID;
        FirstName = content.AuthorContentFirstName ?? "";
        LastName = content.AuthorContentLastName ?? "";
        Biography = content.AuthorContentBiographyHTML ?? "";
        Photo = content.AuthorContentPhoto?.FirstOrDefault() is MediaFile photo 
            ? ImageAssetViewModel.Create(photo) 
            : null;
    }
}
