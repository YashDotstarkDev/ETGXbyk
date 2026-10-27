using CMS.ContentEngine;
using CMS.Websites;
using ETG.Core.Content;
using ETG.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace ETG.Web.Features.GenericPage;

public record GenericPageQuery(RoutedWebPage Page)
    : WebPageRoutedQuery<Container>(Page);

public sealed class GenericPageQueryHandler(WebPageQueryTools tools)
    : WebPageQueryHandler<GenericPageQuery, Container>(tools)
{
    public override async Task<Container> Handle(
        GenericPageQuery request,
        CancellationToken cancellationToken = default)
    {
        // Load the page without the failing recursive linked-item query.
        var pageQuery = new ContentItemQueryBuilder()
            .ForWebPage(request.Page);

        var pages = await Executor.GetMappedWebPageResult<Container>(
            pageQuery,
            DefaultQueryOptions,
            cancellationToken);

        var page = pages.FirstOrDefault();

        if (page is null)
        {
            return null!;
        }

        // Retrieve only MediaFile items selected in ContainerHeroImage.
        var heroQuery = new ContentItemQueryBuilder()
            .ForContentType(
                ETG.Core.Content.MediaFile.CONTENT_TYPE_NAME,
                parameters => parameters.LinkedFrom(
                    Container.CONTENT_TYPE_NAME,
                    nameof(Container.ContainerHeroImage),
                    new IContentItemIdentifier[]
                    {
                        page.SystemFields
                    }))
            .InLanguage(request.Page.LanguageName);

        var heroImages = await Executor
            .GetMappedResult<ETG.Core.Content.MediaFile>(
                heroQuery,
                DefaultQueryOptions,
                cancellationToken);

        page.ContainerHeroImage = heroImages
            .Cast<IContentItemFieldsSource>()
            .ToArray();

        return page;
    }
}