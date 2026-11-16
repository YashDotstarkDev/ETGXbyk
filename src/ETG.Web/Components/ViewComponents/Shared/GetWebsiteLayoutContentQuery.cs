using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using CSharpFunctionalExtensions;
using ETG;
using ETG.Core.Content;
using ETG.Core.Operations;
using ETG.Web.Rendering;
using Kentico.Content.Web.Mvc.Routing;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ETG.Web.Components.ViewComponents.Shared;

public record GetWebsiteLayoutContentQuery : IQuery<Maybe<WebsiteLayoutContentViewModel>>, ICacheByValueQuery
{
    public string CacheValueKey => "WebsiteLayoutContent";
}


public interface IWebsiteLayoutAccessor
{
    Task<WebsiteLayoutContentViewModel?> GetAsync(CancellationToken cancellationToken = default);
}

public sealed class WebsiteLayoutAccessor(IMediator mediator, ILogger<WebsiteLayoutAccessor> logger) : IWebsiteLayoutAccessor
{
    private Task<WebsiteLayoutContentViewModel?>? pending;

    // Scoped = one instance per request, so the first component loads it and the rest reuse it.
    public Task<WebsiteLayoutContentViewModel?> GetAsync(CancellationToken cancellationToken = default) =>
        pending ??= LoadAsync(cancellationToken);

    private async Task<WebsiteLayoutContentViewModel?> LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await mediator.Send(new GetWebsiteLayoutContentQuery(), cancellationToken);
            return result.HasValue ? result.Value : null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Layout content could not be loaded; views will use their fallbacks.");
            return null;
        }
    }
}

public class GetWebsiteLayoutContentQueryHandler(ContentItemQueryTools tools,
     IWebPageUrlRetriever urlRetriever,
    IPreferredLanguageRetriever currentLanguageRetriever
    )
    : ContentItemQueryHandler<GetWebsiteLayoutContentQuery, Maybe<WebsiteLayoutContentViewModel>>(tools)
{
    private const string LayoutContentPath = "/Shared/Menus/Layout-Content";

    public override async Task<Maybe<WebsiteLayoutContentViewModel>> Handle(
        GetWebsiteLayoutContentQuery request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var channelName = ResolveChannelName();

            // ── 1. WebsiteLayoutContent (page type — text/alt-text/copy fields) ────
            var layoutBuilder = new ContentItemQueryBuilder()
                .ForContentType(
                    WebsiteLayoutContent.CONTENT_TYPE_NAME,
                    p => p.WithLinkedItems(2)
                          .ForWebsite(channelName, PathMatch.Single(LayoutContentPath)));

            var layoutResult = await Executor.GetMappedResult<WebsiteLayoutContent>(layoutBuilder, DefaultQueryOptions, cancellationToken);
            var layout = layoutResult.FirstOrDefault();

            // ── 2. WebsiteGlobalContent (reusable content item — logo images) ─────
            var globalBuilder = new ContentItemQueryBuilder()
     .ForContentType(
         WebsiteGlobalContent.CONTENT_TYPE_NAME,
         p => p.WithLinkedItems(2).TopN(1));

            var globalResult = await Executor.GetMappedResult<WebsiteGlobalContent>(globalBuilder, DefaultQueryOptions, cancellationToken);
            var global = globalResult.FirstOrDefault();

            if (layout is null && global is null)
            {
                return Maybe.None;
            }

            var currentLanguage = currentLanguageRetriever.Get();

            string? searchPageUrl = null;
            try
            {
                var searchUrlResult = await urlRetriever.Retrieve("/search", channelName, currentLanguage);
                searchPageUrl = searchUrlResult?.RelativePath;
            }
            catch
            {
                searchPageUrl = "/search";
            }

            string? contactPageUrl = null;
            try
            {
                var contactUrlResult = await urlRetriever.Retrieve("/enquire", channelName, currentLanguage);
                contactPageUrl = contactUrlResult?.RelativePath;
            }
            catch
            {
                contactPageUrl = "/enquire";
            }

            return new WebsiteLayoutContentViewModel
            {
                BrandLogoSvg = SafeString(layout?.BrandLogo),
                BrandLogoAltText = SafeString(layout?.BrandLogoAltText),
                ContactNumber = SafeString(layout?.ContactNumber),
                NavCtaText = SafeString(layout?.NavCtaText),
                OneTreeLogoUrl = SafeAssetUrl(layout?.OneTreeLogo),
                OneTreeLogoAltText = SafeString(layout?.OneTreeLogoAltText),
                SustainableHeading = SafeString(layout?.SustainableHeading),
                SustainableDescription = SafeString(layout?.SustainableDescription),
                FooterLogoUrl = SafeAssetUrl(layout?.FooterLogo),
                FooterLogoAltText = SafeString(layout?.FooterLogoAltText),
                ContactUsText = SafeString(layout?.ContactUsText),
                AwardsImageSvg = SafeString(layout?.AwardsImage),
                AwardsImageAltText = SafeString(layout?.AwardsImageAltText),
                CopyrightText = SafeString(layout?.CopyrightText),

                SearchPageUrl = searchPageUrl,
                ContactPageUrl = contactPageUrl
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Maybe.None;
        }
    }

    private string ResolveChannelName()
    {
        var channelName = tools.WebsiteChannelContext.WebsiteChannelName;
        return string.IsNullOrWhiteSpace(channelName) || channelName == "Website"
            ? "ETG"
            : channelName;
    }

    private static string? SafeString(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static string? SafeAssetUrl(IEnumerable<MediaFile>? assets)
    {
        try
        {
            var asset = assets?.FirstOrDefault();
            if (asset is null)
            {
                return null;
            }

            var url = ImageAssetViewModel.Create(asset)?.LegacyMediaFileAsset?.Url;
            return string.IsNullOrWhiteSpace(url) ? null : url;
        }
        catch
        {
            return null;
        }
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(
        GetWebsiteLayoutContentQuery query,
        Maybe<WebsiteLayoutContentViewModel> result,
        ICacheDependencyKeysBuilder builder) =>
        builder
            .AllContentItems(WebsiteLayoutContent.CONTENT_TYPE_NAME)
            .AllContentItems(WebsiteGlobalContent.CONTENT_TYPE_NAME);
}