using CMS.ContentEngine;
using CMS.Websites;
using ETG.Core.Operations;
using Kentico.Content.Web.Mvc;

using ContainerContent = ETG.Core.Content.Container;

namespace ETG.Web.Features.CampaignPage;

public record CampaignPageQuery(RoutedWebPage Page)
    : WebPageRoutedQuery<ContainerContent>(Page);

public sealed class CampaignPageQueryHandler(WebPageQueryTools tools)
    : WebPageQueryHandler<CampaignPageQuery, ContainerContent>(tools)
{
    public override async Task<ContainerContent> Handle(
        CampaignPageQuery request,
        CancellationToken cancellationToken = default)
    {
        var query = new ContentItemQueryBuilder()
            .ForWebPage(
                request.Page,
                parameters => parameters.WithLinkedItems(
                    1,
                    options => options.IncludeWebPageData()));

        var pages = await Executor
            .GetMappedWebPageResult<ContainerContent>(
                query,
                DefaultQueryOptions,
                cancellationToken);

        return pages.FirstOrDefault();
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(
        CampaignPageQuery query,
        ContainerContent result,
        ICacheDependencyKeysBuilder builder)
    {
        base.AddDependencyKeys(query, result, builder);

        if (result is null) return builder;

        builder.Collection(result.ContainerHeroImage, (img, b) => b.ContentItem(img));
        builder.Collection(result.ContainerHeroContactUsCtaPath, (cta, b) => b.ContentItem(cta));
        builder.Collection(result.ContainerRedirect, (red, b) => b.ContentItem(red));

        return builder;
    }
}