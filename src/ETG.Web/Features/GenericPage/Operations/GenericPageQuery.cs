using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Content;
using ETG.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace ETG.Web.Features.GenericPage;

public record GenericPageQuery(RoutedWebPage Page)
    : WebPageRoutedQuery<Container>(Page);

public sealed class GenericPageQueryHandler(
    WebPageQueryTools tools,
    IWebsiteChannelContext websiteChannelContext)
    : WebPageQueryHandler<GenericPageQuery, Container>(tools)
{
    public override async Task<Container> Handle(
        GenericPageQuery request,
        CancellationToken cancellationToken = default)
    {
        if (request?.Page is null)
        {
            // The existing controller handles this with NotFound().
            return null!;
        }

        // Preserve the non-recursive Container query.
        var pageQuery = new ContentItemQueryBuilder()
            .ForWebPage(request.Page);

        var pages = await Executor.GetMappedWebPageResult<Container>(
            pageQuery,
            DefaultQueryOptions,
            cancellationToken);

        var page = pages?.FirstOrDefault();

        if (page?.SystemFields is null)
        {
            return null!;
        }

        var systemFields = page.SystemFields;
        var languageName = request.Page.LanguageName;
        var websiteChannelName =
            websiteChannelContext?.WebsiteChannelName;

        // An absent redirect selection is valid.
        page.ContainerRedirect ??=
            Array.Empty<IContentItemFieldsSource>();

        if (systemFields.ContentItemGUID != Guid.Empty &&
            !string.IsNullOrWhiteSpace(websiteChannelName))
        {
            var redirectQuery = new ContentItemQueryBuilder()
                .ForContentType(
                    Container.CONTENT_TYPE_NAME,
                    parameters =>
                    {
                        parameters.ForWebsite(websiteChannelName);

                        parameters.Where(where => where.WhereEquals(
                            "ContentItemGUID",
                            systemFields.ContentItemGUID));

                        parameters.WithLinkedItems(
                            1,
                            options => options.IncludeWebPageData());
                    })
                .InLanguage(languageName);

            var redirectContainers =
                await Executor.GetMappedWebPageResult<Container>(
                    redirectQuery,
                    DefaultQueryOptions,
                    cancellationToken);

            page.ContainerRedirect = redirectContainers?
                .FirstOrDefault()?
                .ContainerRedirect
                ?? Array.Empty<IContentItemFieldsSource>();
        }

        // Preserve existing dedicated hero-image retrieval.
        var heroQuery = new ContentItemQueryBuilder()
            .ForContentType(
                MediaFile.CONTENT_TYPE_NAME,
                parameters => parameters.LinkedFrom(
                    Container.CONTENT_TYPE_NAME,
                    nameof(Container.ContainerHeroImage),
                    new IContentItemIdentifier[]
                    {
                        systemFields
                    }))
            .InLanguage(languageName);

        var heroImages = await Executor.GetMappedResult<MediaFile>(
            heroQuery,
            DefaultQueryOptions,
            cancellationToken);

        page.ContainerHeroImage = heroImages?
            .OfType<IContentItemFieldsSource>()
            .ToArray()
            ?? Array.Empty<IContentItemFieldsSource>();

        // Preserve selected image-folder and nested-image retrieval.
        var foldersQuery = new ContentItemQueryBuilder()
            .ForContentType(
                ImageFolder.CONTENT_TYPE_NAME,
                parameters =>
                {
                    parameters.LinkedFrom(
                        Container.CONTENT_TYPE_NAME,
                        nameof(Container.SelectedImageFolders),
                        new IContentItemIdentifier[]
                        {
                            systemFields
                        });

                    parameters.WithLinkedItems(2);
                })
            .InLanguage(languageName);

        var folders = await Executor.GetMappedResult<ImageFolder>(
            foldersQuery,
            DefaultQueryOptions,
            cancellationToken);

        page.SelectedImageFolders = folders?
            .OfType<ImageFolder>()
            .ToArray()
            ?? Array.Empty<ImageFolder>();

        return page;
    }
}