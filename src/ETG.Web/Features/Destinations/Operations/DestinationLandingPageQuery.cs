using System.Linq;
using CMS.ContentEngine;
using CMS.Websites;
using ETG.Core.Content;
using ETG.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace ETG.Web.Features.Destinations.Operations;

public record DestinationLandingPageQuery(RoutedWebPage Page)
    : WebPageRoutedQuery<Container>(Page);

public class DestinationLandingPageQueryHandler(WebPageQueryTools tools)
    : WebPageQueryHandler<DestinationLandingPageQuery, Container>(tools)
{
    public override async Task<Container> Handle(
        DestinationLandingPageQuery request,
        CancellationToken cancellationToken = default)
    {
        // Load the container without general linked-item loading.
        var query = new ContentItemQueryBuilder()
            .ForWebPage(request.Page);

        var result = await Executor.GetMappedWebPageResult<Container>(
            query,
            DefaultQueryOptions,
            cancellationToken);

        var container = result.FirstOrDefault();

        if (container is null)
        {
            return null!;
        }

        // Query only MediaFile items referenced by the hero field.
        var imageQuery = new ContentItemQueryBuilder()
            .ForContentType(
                ETG.Core.Content.MediaFile.CONTENT_TYPE_NAME,
                parameters => parameters.LinkedFrom(
                    Container.CONTENT_TYPE_NAME,
                    nameof(Container.ContainerHeroImage),
                    new IContentItemIdentifier[]
                    {
                        container.SystemFields
                    }))
            .InLanguage(request.Page.LanguageName);

        var images = await Executor
            .GetMappedResult<ETG.Core.Content.MediaFile>(
                imageQuery,
                DefaultQueryOptions,
                cancellationToken);

        container.ContainerHeroImage = images
            .Cast<IContentItemFieldsSource>()
            .ToArray();

        return container;
    }
}