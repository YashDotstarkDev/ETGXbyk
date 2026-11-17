
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
) : WebPageRoutedQuery<ExperienceLandingPageQueryResponse>(Page),
    IChannelContentQuery
{
    public override string CacheValueKey => $"{Page.WebPageItemID}:{ChannelName}";
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
            .ForWebPage(
                request.Page,
                parameters => parameters.WithLinkedItems(
                    1,
                    options => options.IncludeWebPageData()));

        var landingPageResults =
            await Executor.GetMappedWebPageResult<ContainerContent>(
                landingPageQuery,
                DefaultQueryOptions,
                cancellationToken);

        var landingPage = landingPageResults.FirstOrDefault();

        if (landingPage is null)
        {
            return null;
        }

        // Retrieve only MediaFile items selected in ContainerHeroImage.
        var heroQuery = new ContentItemQueryBuilder()
            .ForContentType(
                MediaFile.CONTENT_TYPE_NAME,
                config => config.LinkedFrom(
                    ContainerContent.CONTENT_TYPE_NAME,
                    nameof(ContainerContent.ContainerHeroImage),
                    new IContentItemIdentifier[]
                    {
                        landingPage.SystemFields
                    }));

        var heroTask = Executor.GetMappedResult<MediaFile>(
            heroQuery,
            DefaultQueryOptions,
            cancellationToken);

        // Retrieve child Experience pages.
        var experiencesQuery = new ContentItemQueryBuilder()
            .ForContentType(
                ExperienceContent.CONTENT_TYPE_NAME,
                query => query
                    .ForWebsite(
                        websiteChannelName: request.ChannelName,
                        pathMatch: PathMatch.Children(
                            landingPage.SystemFields.WebPageItemTreePath,
                            nestingLevel: 1))
                    .WithLinkedItems(4)
                    .OrderBy("WebPageItemOrder"));

        var experiencesTask =
            Executor.GetMappedWebPageResult<ExperienceContent>(
                experiencesQuery,
                DefaultQueryOptions,
                cancellationToken);

        await Task.WhenAll(heroTask, experiencesTask);

        var heroImages = await heroTask;
        var experiences = await experiencesTask;

        landingPage.ContainerHeroImage = heroImages
            .Cast<IContentItemFieldsSource>()
            .ToArray();

        return new ExperienceLandingPageQueryResponse(
            landingPage,
            experiences.ToList());
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(
        ExperienceLandingPageQuery query,
        ExperienceLandingPageQueryResponse result,
        ICacheDependencyKeysBuilder builder) =>
        base.AddDependencyKeys(query, result, builder)
            .AllContentItems(ExperienceContent.CONTENT_TYPE_NAME);
}
