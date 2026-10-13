using CMS.ContentEngine;
using CMS.Websites;
using ETG.Core.Operations;
using Kentico.Content.Web.Mvc;

using PageContent = ETG.Core.Content.Page;

namespace ETG.Web.Features.Brochures.Operations;
public record BrochuresPageQuery(RoutedWebPage Page)
    : IQuery<BrochuresPageQueryResponse>, ICacheByValueQuery
{
    public string CacheValueKey => Page.WebPageItemID.ToString();
}
public record BrochuresPageQueryResponse(
    PageContent Page,
    IReadOnlyList<ETG.BrochureFolder> Folders);
public class BrochuresPageQueryHandler(WebPageQueryTools tools)
    : WebPageQueryHandler<
        BrochuresPageQuery,
        BrochuresPageQueryResponse>(tools)
{
    public override async Task<BrochuresPageQueryResponse> Handle(
        BrochuresPageQuery request,
        CancellationToken cancellationToken = default)
    {

        var pageQuery = new ContentItemQueryBuilder()
            .ForWebPage(
                request.Page,
                parameters => parameters.WithLinkedItems(1));

        var pages = await Executor.GetMappedWebPageResult<PageContent>(
            pageQuery,
            DefaultQueryOptions,
            cancellationToken);

        var page = pages.First();
        
        var folderQuery = new ContentItemQueryBuilder()
            .ForContentType(
                ETG.BrochureFolder.CONTENT_TYPE_NAME,
                parameters => parameters.WithLinkedItems(3));

        var folders = await Executor.GetMappedResult<ETG.BrochureFolder>(
            folderQuery,
            new ContentQueryExecutionOptions
            {
                ForPreview = false
            },
            cancellationToken);

        return new BrochuresPageQueryResponse(
            page,
            folders.ToList());
    }
    protected override ICacheDependencyKeysBuilder AddDependencyKeys(
        BrochuresPageQuery query,
        BrochuresPageQueryResponse result,
        ICacheDependencyKeysBuilder builder) =>
        base.AddDependencyKeys(query, result, builder)
            .AllContentItems(ETG.BrochureFolder.CONTENT_TYPE_NAME)
            .Collection(
                result.Folders,
                (folder, folderBuilder) => folderBuilder
                    .ContentItem(folder)
                    .Collection(
                        folder.SelectedBrochures,
                        (brochure, brochureBuilder) => brochureBuilder
                            .ContentItem(brochure)
                            .Collection(
                                brochure.BrochureCoverImage,
                                (cover, coverBuilder) =>
                                    coverBuilder.ContentItem(cover))
                            .Collection(
                                brochure.BrochureFile,
                                (file, fileBuilder) =>
                                    fileBuilder.ContentItem(file))));
}