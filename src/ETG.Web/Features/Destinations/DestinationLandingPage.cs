using CMS.ContentEngine;
using CMS.Helpers;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Content;
using ETG.Web.Features.Destinations.Operations;
using ETG.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using ContainerContent = ETG.Core.Content.Container;

[assembly: RegisterWebPageRoute(
    contentTypeName: ContainerContent.CONTENT_TYPE_NAME,
    controllerType: typeof(
        ETG.Web.Features.Destinations.DestinationLandingPageController),
    Path = "/destinations",
    WebsiteChannelNames = new[] { "ETG" }
)]

namespace ETG.Web.Features.Destinations;

public sealed class DestinationLandingPageController(
    IMediator mediator,
    IWebPageDataContextRetriever contextRetriever,
    IWebPageUrlRetriever urlRetriever,
    IWebsiteChannelContext websiteChannelContext,
    WebPageMetaService metaService,
    ITaxonomyRetriever taxonomyRetriever,
    IContainerExpiryService expiryService,
    IProgressiveCache? cache = null
) : Controller
{
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken = default)
    {
        if (!contextRetriever.TryRetrieve(out var data))
        {
            return NotFound();
        }

        var container = await mediator.Send(
            new DestinationLandingPageQuery(data.WebPage),
            cancellationToken);

        if (container is null)
        {
            return NotFound();
        }

        var redirectUrl = await expiryService.GetExpiryRedirectUrlAsync(
            container,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(redirectUrl))
        {
            return Redirect(redirectUrl);
        }

        async Task<DestinationLandingPageViewModel> LoadModelAsync()
        {
            // Existing destination query remains unchanged.
            var destinations = await mediator.Send(
                new MainDestinationsQuery(
                    data.WebPage,
                    websiteChannelContext.WebsiteChannelName,
                    container.SystemFields.WebPageItemTreePath),
                cancellationToken);

            // Categories now come from the tags on each destination.
            return await DestinationLandingPageViewModel.CreateAsync(
                container,
                destinations,
                urlRetriever,
                taxonomyRetriever,
                "en-AU");
        }

        DestinationLandingPageViewModel model;

        if (cache != null)
        {
            var cacheKey = $"DestinationLandingPageVM_{websiteChannelContext.WebsiteChannelName}_{data.WebPage.WebPageItemID}";
            model = await cache.LoadAsync(async cs =>
            {
                cs.GetCacheDependency = () => CacheHelper.GetCacheDependency(new[]
                {
                    $"webpageitem|byid|{data.WebPage.WebPageItemID}",
                    $"webpageitem|bychannel|{websiteChannelContext.WebsiteChannelName}|bycontenttype|{Destination.CONTENT_TYPE_NAME}",
                    $"webpageitem|bychannel|{websiteChannelContext.WebsiteChannelName}|bycontenttype|{ContainerContent.CONTENT_TYPE_NAME}",
                    $"webpageitem|bychannel|{websiteChannelContext.WebsiteChannelName}|all",
                    "webpageitem|all",
                    CacheHelper.GetCacheItemName("contentitem", "bycontenttype", Destination.CONTENT_TYPE_NAME),
                    CacheHelper.GetCacheItemName("contentitem", "bycontenttype", ContainerContent.CONTENT_TYPE_NAME)
                });
                return await LoadModelAsync();
            }, new CacheSettings(30, cacheKey));
        }
        else
        {
            model = await LoadModelAsync();
        }

        var heroImage = container.ContainerHeroImage?
            .OfType<ETG.Core.Content.MediaFile>()
            .FirstOrDefault();

        if (container is IMetaDataSchema metaFields)
        {
            metaService.SetMeta(
                metaFields,
                heroImage?.LegacyMediaFileAsset?.Url);
        }

        return View(
            "~/Features/Destinations/DestinationLandingPage_Default.cshtml",
            model);
    }
}
