using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Websites;
using ETG.Core.Content;
using ETG.Core.Operations;
using MediatR;

using ArticleContent = ETG.Core.Content.Article;

namespace ETG.Web.Features.Destinations.Operations;

/// <summary>
/// Query to retrieve articles related to specific destination(s)
/// PERFORMANCE FIX: Uses database-level filtering with WhereLike and TopN(3)
/// CACHE FIX: Implements ICacheByValueQuery to prevent cache collisions
/// </summary>
public record GetDestinationArticlesQuery(
    string ChannelName,
    HashSet<Guid> DestinationGuids
) : IQuery<List<ArticleContent>>, IChannelContentQuery, ICacheByValueQuery
{
    string IChannelContentQuery.ChannelName => ChannelName;
    
    // CACHE KEY FIX: Include all destination GUIDs to prevent cache collisions
    public string CacheValueKey => string.Join("|", DestinationGuids.OrderBy(g => g));
}

/// <summary>
/// Handler that retrieves articles with database-level filtering
/// PERFORMANCE FIX: Pushes destination filtering and TopN to database query
/// </summary>
public class GetDestinationArticlesQueryHandler(
    ContentItemQueryTools tools
) : ContentItemQueryHandler<GetDestinationArticlesQuery, List<ArticleContent>>(tools)
{
    public override async Task<List<ArticleContent>> Handle(
        GetDestinationArticlesQuery request,
        CancellationToken cancellationToken)
    {
        if (request.DestinationGuids.Count == 0)
        {
            return [];
        }

        // OPTIMIZED: Use OR conditions to filter by any matching destination GUID at database level
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
                    {
                        // Build OR conditions for each destination GUID
                        foreach (var destGuid in request.DestinationGuids)
                        {
                            where.Or().WhereLike(
                                nameof(ArticleContent.ArticleDestinations),
                                $"%{destGuid}%");
                        }
                    })
            );

        var articles = await Executor.GetMappedWebPageResult<ArticleContent>(
            articlesQuery,
            DefaultQueryOptions,
            cancellationToken);

        return articles.ToList();
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(
        GetDestinationArticlesQuery query,
        List<ArticleContent> result,
        ICacheDependencyKeysBuilder builder)
    {
        // CACHE DEPENDENCY FIX: Include dependencies on destination content items
        builder = builder.AllContentItems(ArticleContent.CONTENT_TYPE_NAME);
        
        // Add dependencies for each returned article
        foreach (var article in result)
        {
            builder = builder.ContentItem(article);
        }
        
        // Add dependencies for destination pages that filter the results
        foreach (var destGuid in query.DestinationGuids)
        {
            builder = builder.WebPage(destGuid);
        }
        
        return builder;
    }
}
