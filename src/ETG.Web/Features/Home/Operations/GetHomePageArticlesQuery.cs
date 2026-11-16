using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Websites;
using ETG.Core.Content;
using ETG.Core.Operations;

using ArticleContent = ETG.Core.Content.Article;

namespace ETG.Web.Features.Home.Operations;

/// <summary>
/// Query to retrieve latest articles for home page
/// PERFORMANCE FIX: Uses TopN(3) to limit results at database level
/// </summary>
public record GetHomePageArticlesQuery(
    string ChannelName
) : IQuery<List<ArticleContent>>, IChannelContentQuery
{
    string IChannelContentQuery.ChannelName => ChannelName;
}

public class GetHomePageArticlesQueryHandler(
    ContentItemQueryTools tools
) : ContentItemQueryHandler<GetHomePageArticlesQuery, List<ArticleContent>>(tools)
{
    public override async Task<List<ArticleContent>> Handle(
        GetHomePageArticlesQuery request,
        CancellationToken cancellationToken)
    {
        // OPTIMIZED: TopN(3) pushes limit to database query
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
            );

        var articles = await Executor.GetMappedWebPageResult<ArticleContent>(
            articlesQuery,
            DefaultQueryOptions,
            cancellationToken);

        return articles.ToList();
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(
        GetHomePageArticlesQuery query,
        List<ArticleContent> result,
        ICacheDependencyKeysBuilder builder)
    {
        return builder
            .AllContentItems(ArticleContent.CONTENT_TYPE_NAME)
            .Collection(
                result,
                (article, itemBuilder) => itemBuilder.ContentItem(article));
    }
}
