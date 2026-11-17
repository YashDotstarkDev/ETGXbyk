using CMS.ContentEngine;
using CMS.Websites;
using ETG.Core.Content;
using ETG.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace ETG.Web.Features.DestinationPostPage;

public record DestinationPostPageQuery(RoutedWebPage Page)
    : WebPageRoutedQuery<Destination>(Page);

public class DestinationPostPageQueryHandler(WebPageQueryTools tools)
    : WebPageQueryHandler<DestinationPostPageQuery, Destination>(tools)
{
    public override async Task<Destination> Handle(
        DestinationPostPageQuery request,
        CancellationToken cancellationToken = default)
    {
        // OPTIMIZED: Depth 2 is sufficient for Folders -> AccordionItems & MediaFiles
        var destinationQuery = new ContentItemQueryBuilder()
            .ForWebPage(request.Page, c => c.WithLinkedItems(2));

        var result = await Executor.GetMappedWebPageResult<Destination>(
            destinationQuery,
            DefaultQueryOptions,
            cancellationToken);

        return result.FirstOrDefault()!;
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(
        DestinationPostPageQuery query,
        Destination result,
        ICacheDependencyKeysBuilder builder)
    {
        builder = base.AddDependencyKeys(query, result, builder);
        if (result is null) return builder;

        return builder
            .AllContentItems(Destination.CONTENT_TYPE_NAME)
            .AllContentItems(AccordionItemFolder.CONTENT_TYPE_NAME)
            .AllContentItems(AccordionItem.CONTENT_TYPE_NAME)
            .AllContentItems(MediaFile.CONTENT_TYPE_NAME)
            .AllWebPages(query.Page.WebsiteChannelName);
    }
}
