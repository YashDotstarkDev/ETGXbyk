using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Websites;
using ETG.Core.Content;
using ETG.Core.Operations;
using MediatR;

using ArticleContent = ETG.Core.Content.Article;

namespace ETG.Web.Features.Destinations.Operations;

public record GetDestinationArticlesQuery(
    string ChannelName,
    HashSet<Guid> DestinationGuids
) : IQuery<List<ArticleContent>>, IChannelContentQuery, ICacheByValueQuery
{
    string IChannelContentQuery.ChannelName => ChannelName;
    
    public string CacheValueKey => string.Join("|", DestinationGuids.OrderBy(g => g));
}

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

        var articlesQuery = new ContentItemQueryBuilder()
            .ForContentType(
                ArticleContent.CONTENT_TYPE_NAME,
                config => config
                    .ForWebsite(request.ChannelName)
                    .WithLinkedItems(1)
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
        return builder
            .AllContentItems(ArticleContent.CONTENT_TYPE_NAME)
            .WebPageType(ArticleContent.CONTENT_TYPE_NAME, query.ChannelName)
            .AllWebPages(query.ChannelName)
            .AllWebPages()
            .WebPages(result)
            .Collection(
                result,
                (article, itemBuilder) => itemBuilder.ContentItem(article));
    }
}
