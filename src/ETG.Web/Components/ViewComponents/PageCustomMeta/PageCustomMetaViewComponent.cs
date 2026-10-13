using CMS.Websites.Routing;
using ETG.Core.Content;
using ETG.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ETG.Web.Components.ViewComponents.PageCustomMeta;

public class PageCustomMetaViewComponent(
    WebPageMetaService metaService,
    IMediator mediator,
    IWebsiteChannelContext channelContext) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var meta = await metaService.GetMeta();
        var settings = await mediator.Send(new WebsiteSettingsQuery());

        var request = HttpContext.Request;

        var canonicalUrl =
            $"{request.Scheme}://{request.Host}" +
            $"{request.PathBase}{request.Path}";

        var vm = new PageCustomMetaViewModel(meta)
        {
            GoogleVerificationCode = settings.GlobalContent.GoogleVerificationCode ?? "",
            site_name = channelContext.WebsiteChannelName ?? "",
            CanonicalUrl = canonicalUrl
        };

        return View(
            "~/Components/ViewComponents/PageCustomMeta/PageCustomMeta.cshtml", vm);
    }
}

public class PageCustomMetaViewModel
{
    public PageCustomMetaViewModel(WebPageMeta meta)
    {
        Title = meta.Title;
        Description = meta.Description;
        Keywords = meta.Keywords;
        ShareImage = meta.ShareImage;
    }

    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public string Keywords { get; init; } = "";
    public string ShareImage { get; init; } = "";
    public string GoogleVerificationCode { get; init; } = "";
    public string site_name { get; init; } = "";
    public string CanonicalUrl { get; init; } = "";
}