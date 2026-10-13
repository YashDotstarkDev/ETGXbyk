using ETG.Core.Models.Tour;
namespace ETG.Web.Services.Tour;

public sealed class ShareLinksService(
    IHttpContextAccessor httpContextAccessor)
    : IShareLinksService
{
    public ShareLinksViewModel GetShareLinks()
    {
        var request = httpContextAccessor.HttpContext?.Request;

        if (request is null)
        {
            return new ShareLinksViewModel();
        }

        var currentUrl =
            $"{request.Scheme}://{request.Host}" +
            $"{request.PathBase}" +
            $"{request.Path}" +
            $"{request.QueryString}";

        var encodedUrl =
            Uri.EscapeDataString(currentUrl);

        return new ShareLinksViewModel
        {
            CurrentUrl = currentUrl,

            FacebookShareUrl =
                $"https://www.facebook.com/sharer/sharer.php?u={encodedUrl}",

            TwitterShareUrl =
                $"https://twitter.com/intent/tweet?url={encodedUrl}",

            LinkedInShareUrl =
                $"https://www.linkedin.com/shareArticle?mini=true&url={encodedUrl}",

            PinterestShareUrl =
                $"http://pinterest.com/pin/create/button/?url={encodedUrl}",

            CopyUrl = currentUrl
        };
    }
}
