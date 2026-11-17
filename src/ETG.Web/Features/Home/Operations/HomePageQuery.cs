using CMS.ContentEngine;
using CMS.Websites;
using ETG.Core.Content;
using ETG.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace ETG.Web.Features.Home;

public record HomePageQuery(RoutedWebPage Page)
    : WebPageRoutedQuery<Homepage>(Page);

public sealed class HomePageQueryHandler(WebPageQueryTools tools)
    : WebPageQueryHandler<HomePageQuery, Homepage>(tools)
{
    public override async Task<Homepage> Handle(
        HomePageQuery request,
        CancellationToken cancellationToken = default)
    {
        var query = new ContentItemQueryBuilder()
            .ForWebPage(
                request.Page,
                config => config.WithLinkedItems(1));

        var pages = await Executor.GetMappedWebPageResult<Homepage>(
            query,
            DefaultQueryOptions,
            cancellationToken);

        return pages.FirstOrDefault();
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(
        HomePageQuery query,
        Homepage result,
        ICacheDependencyKeysBuilder builder)
    {
        base.AddDependencyKeys(query, result, builder);

        if (result is null) return builder;

        builder.Collection(result.HomepageHeroImage, (img, b) => b.ContentItem(img));
        builder.Collection(result.HomepageHeroVideoImage, (img, b) => b.ContentItem(img));

        if (result.HomepageExperiences is not null)
        {
            foreach (var exp in result.HomepageExperiences)
            {
                if (exp.WebPageGuid != Guid.Empty)
                {
                    builder.WebPage(exp.WebPageGuid);
                }
            }
        }

        return builder;
    }
}