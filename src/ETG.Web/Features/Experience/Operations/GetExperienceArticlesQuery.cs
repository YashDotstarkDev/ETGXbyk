using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Websites;
using ETG.Core.Content;
using ETG.Core.Operations;

using ArticleContent = ETG.Core.Content.Article;

namespace ETG.Web.Features.Experience.Operations;

/// <summary>
/// Query to retrieve articles related to a specific experience
/// PERFORMANCE FIX: Uses TopN(3) to limit results at database level
/// CACHE FIX: Implements ICacheByValueQuery to prevent cache collisions
/// </summary>
public record GetExperienceArticlesQuery(
    string ChannelName,
    string ExperienceGuid
) : IQuery<List<ArticleContent>>, IChannelContentQuery, ICacheByValueQuery
{
    string IChannelContentQuery.ChannelName => ChannelName;
    
    // CACHE KEY FIX: Include experience GUID to prevent cache collisions
    public string CacheValueKey => ExperienceGuid ?? string.Empty;
}

public class GetExperienceArticlesQueryHandler(
    ContentItemQueryTools tools
) : ContentItemQueryHandler<GetExperienceArticlesQuery, List<ArticleContent>>(tools)
{
    public override async Task<List<ArticleContent>> Handle(
        GetExperienceArticlesQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ExperienceGuid))
        {
            return [];
        }

        // OPTIMIZED: TopN(3) and WHERE clause pushed to database
        var articlesQuery = new ContentItemQueryBuilder()
            .ForContentType(
                ArticleContent.CONTENT_TYPE_NAME,
                config => config
                    .ForWebsite(request.ChannelName)
                    .WithLinkedItems(2)
                    .TopN(3)
                    .OrderBy(new OrderByColumn(
                        nameof(ArticleContent.ArticleDate),
                        OrderDirection.Descending))
                    .Where(where =>
                        where.WhereLike(
                            nameof(ArticleContent.ArticleExperiences),
                            $"%{request.ExperienceGuid}%"))
            );

        var articles = await Executor.GetMappedWebPageResult<ArticleContent>(
            articlesQuery,
            DefaultQueryOptions,
            cancellationToken);

        return articles.ToList();
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(
        GetExperienceArticlesQuery query,
        List<ArticleContent> result,
        ICacheDependencyKeysBuilder builder)
    {
        // CACHE DEPENDENCY FIX: Include dependencies on experience content
        builder = builder.AllContentItems(ArticleContent.CONTENT_TYPE_NAME);
        
        // Add dependencies for each returned article
        foreach (var article in result)
        {
            builder = builder.ContentItem(article);
        }
        
        // Add dependency for the experience page that filters the results
        if (Guid.TryParse(query.ExperienceGuid, out var experienceGuid))
        {
            builder = builder.WebPage(experienceGuid);
        }
        
        return builder;
    }
}
