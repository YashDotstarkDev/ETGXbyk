using CMS.ContentEngine;
using CMS.Websites;
using CSharpFunctionalExtensions;
using ETG;
using ETG.Core.Content;
using ETG.Core.Operations;
using ETG.Web.Rendering;
using Kentico.Content.Web.Mvc.Routing;
using CMS.Websites.Routing;

namespace ETG.Web.Components.ViewComponents.Shared;

public record GetWebsiteLayoutContentQuery : IQuery<Maybe<WebsiteLayoutContentViewModel>>, ICacheByValueQuery
{
    public string CacheValueKey => "WebsiteLayoutContent";
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
            // ── 1. WebsiteLayoutContent (page type — text/alt-text/copy fields) ────
            var layoutBuilder = new ContentItemQueryBuilder()
                .ForContentType(
                    WebsiteLayoutContent.CONTENT_TYPE_NAME,
                    p => p.WithLinkedItems(2)
                          .ForWebsite(tools.WebsiteChannelContext.WebsiteChannelName, PathMatch.Single(LayoutContentPath)));

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
            var channelName = tools.WebsiteChannelContext.WebsiteChannelName;

            var searchUrlResult = await urlRetriever.Retrieve("/search", channelName, currentLanguage);
            var contactUrlResult = await urlRetriever.Retrieve("/enquire", channelName, currentLanguage);


            return new WebsiteLayoutContentViewModel
            {
                BrandLogoSvg = SafeString(global?.BrandLogo),
                BrandLogoAltText = SafeString(layout?.BrandLogoAltText),
                ContactNumber = SafeString(layout?.ContactNumber),
                NavCtaText = SafeString(layout?.NavCtaText),
                OneTreeLogoUrl = SafeAssetUrl(layout?.OneTreeLogo),
                OneTreeLogoAltText = SafeString(layout?.OneTreeLogoAltText),
                SustainableHeading = SafeString(layout?.SustainableHeading),
                SustainableDescription = SafeString(layout?.SustainableDescription),
                FooterLogoUrl = SafeAssetUrl(global?.FooterLogo),
                FooterLogoAltText = SafeString(layout?.FooterLogoAltText),
                ContactUsText = SafeString(layout?.ContactUsText),
                AwardsImageSvg = SafeString(layout?.AwardsImage),
                AwardsImageAltText = SafeString(layout?.AwardsImageAltText),
                CopyrightText = SafeString(layout?.CopyrightText),

                SearchPageUrl = searchUrlResult?.RelativePath,
                ContactPageUrl = contactUrlResult?.RelativePath
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Maybe.None;
        }
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