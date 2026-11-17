using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Websites;
using ETG.Core.Content;
using ETG.Core.Operations;

using ArticleContent = ETG.Core.Content.Article;

namespace ETG.Web.Features.Home.Operations;

public record GetHomePageArticlesQuery(
    string ChannelName
) : IQuery<List<ArticleViewModel>>, IChannelContentQuery
{
    string IChannelContentQuery.ChannelName => ChannelName;
}

public class GetHomePageArticlesQueryHandler(
    ContentItemQueryTools tools,
    IWebPageUrlRetriever urlRetriever,
    ITaxonomyRetriever taxonomyRetriever
) : ContentItemQueryHandler<GetHomePageArticlesQuery, List<ArticleViewModel>>(tools)
{
    public override async Task<List<ArticleViewModel>> Handle(
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

        var viewModels = await Task.WhenAll(
            articles.Select(article =>
                ArticleViewModel.CreateAsync(
                    article,
                    urlRetriever,
                    taxonomyRetriever)));

        return viewModels.ToList();
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(
        GetHomePageArticlesQuery query,
        List<ArticleViewModel> result,
        ICacheDependencyKeysBuilder builder)
    {
        return builder.AllContentItems(ArticleContent.CONTENT_TYPE_NAME);
    }
}
