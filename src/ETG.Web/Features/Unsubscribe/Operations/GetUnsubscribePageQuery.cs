using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.ContentEngine;
using CMS.Websites;

using MediatR;

using PageContent = ETG.Core.Content.Page;

namespace ETG.Web.Features.Unsubscribe.Operations;

public sealed record GetUnsubscribePageQuery(
    int WebPageItemId,
    string WebsiteChannelName,
    string LanguageName,
    bool ForPreview) : IRequest<PageContent?>;

public sealed class GetUnsubscribePageQueryHandler
    : IRequestHandler<GetUnsubscribePageQuery, PageContent?>
{
    private readonly IContentQueryExecutor executor;

    public GetUnsubscribePageQueryHandler(IContentQueryExecutor executor)
    {
        this.executor = executor;
    }

    public async Task<PageContent?> Handle(
        GetUnsubscribePageQuery request,
        CancellationToken cancellationToken)
    {
        var query = new ContentItemQueryBuilder()
            .ForContentType(
                PageContent.CONTENT_TYPE_NAME,
                parameters => parameters
                    .ForWebsite(request.WebsiteChannelName)
                    .Where(where => where.WhereEquals(
                        "WebPageItemID",
                        request.WebPageItemId))
                    .WithLinkedItems(1))
            .InLanguage(request.LanguageName);

        var pages = await executor.GetMappedWebPageResult<PageContent>(
            query,
            new ContentQueryExecutionOptions
            {
                ForPreview = request.ForPreview
            },
            cancellationToken: cancellationToken);

        return pages.FirstOrDefault();
    }
}