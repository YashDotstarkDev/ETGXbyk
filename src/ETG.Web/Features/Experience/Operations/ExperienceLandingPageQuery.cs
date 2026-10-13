using CMS.ContentEngine;
using CMS.Websites;
using ETG.Core.Content;
using ETG.Core.Operations;
using Kentico.Content.Web.Mvc;

using ContainerContent = ETG.Core.Content.Container;
using ExperienceContent = ETG.Core.Content.Experience;

namespace ETG.Web.Features.Experience.Operations;

public record ExperienceLandingPageQuery(
    RoutedWebPage Page,
    string ChannelName
) : IQuery<ExperienceLandingPageQueryResponse>,
    ICacheByValueQuery,
    IChannelContentQuery
{
    public string CacheValueKey => Page.WebPageItemID.ToString();
}

public record ExperienceLandingPageQueryResponse(
    ContainerContent Page,
    IReadOnlyList<ExperienceContent> Items
);

public class ExperienceLandingPageQueryHandler(
    WebPageQueryTools tools
) : WebPageQueryHandler<
    ExperienceLandingPageQuery,
    ExperienceLandingPageQueryResponse>(tools)
{
    public override async Task<ExperienceLandingPageQueryResponse> Handle(
        ExperienceLandingPageQuery request,
        CancellationToken cancellationToken = default)
    {
        var landingPageQuery = new ContentItemQueryBuilder()
    .ForWebPage(request.Page);

        var landingPage = (
            await Executor.GetMappedWebPageResult<ContainerContent>(
                landingPageQuery,
                DefaultQueryOptions,
                cancellationToken
            )
        ).FirstOrDefault();

        if (landingPage is null)
        {
            return null;
        }

        var experiencesQuery = new ContentItemQueryBuilder()
            .ForContentType(
                ExperienceContent.CONTENT_TYPE_NAME,
                query => query
                    .ForWebsite(
                        websiteChannelName: request.ChannelName,
                        pathMatch: PathMatch.Children(
                            landingPage.SystemFields.WebPageItemTreePath,
                            nestingLevel: 1
                        )
                    )
                    .WithLinkedItems(4)
                    .OrderBy(nameof(WebPageFields.WebPageItemOrder))
            );

        var experiences = (
            await Executor.GetMappedWebPageResult<ExperienceContent>(
                experiencesQuery,
                DefaultQueryOptions,
                cancellationToken
            )
        ).ToList();

        return new ExperienceLandingPageQueryResponse(
            landingPage,
            experiences
        );
    }
}