using CMS.ContentEngine;
using CMS.Websites;
using ETG.Core.Content;
using ETG.Core.Operations;
using Kentico.Content.Web.Mvc;

using ExperienceContent = ETG.Core.Content.Experience;

namespace ETG.Web.Features.Experience.Operations;

public record ExperiencePageQuery(
    RoutedWebPage Page,
    string ChannelName
) : IQuery<ExperiencePageQueryResponse>,
    ICacheByValueQuery,
    IChannelContentQuery
{
    public string CacheValueKey =>
        Page.WebPageItemID.ToString();
}


public record ExperiencePageQueryResponse(
    ExperienceContent Experience
);


public class ExperiencePageQueryHandler(
    WebPageQueryTools tools
) : WebPageQueryHandler<
    ExperiencePageQuery,
    ExperiencePageQueryResponse>(tools)
{
    public override async Task<ExperiencePageQueryResponse> Handle(
        ExperiencePageQuery request,
        CancellationToken cancellationToken = default)
    {
        var query = new ContentItemQueryBuilder()
            .ForWebPage(
                request.Page,
                queryParams => queryParams
                    .WithLinkedItems(4)
            );

        var result =
            await Executor.GetMappedWebPageResult<ExperienceContent>(
                query,
                DefaultQueryOptions,
                cancellationToken
            );

        var experience = result.FirstOrDefault();

        if (experience == null)
        {
            return null;
        }

        return new ExperiencePageQueryResponse(experience);
    }
}