namespace ETG.Web.Features.TourPackage.Operations
{
    using CMS.ContentEngine;
    using CMS.Websites;
    using CMS.Websites.Routing;
    using ETG.Core.Content;
    using ETG.Core.Operations;
    using Kentico.Content.Web.Mvc;

    public record TourPostPageQuery(RoutedWebPage Page) : WebPageRoutedQuery<Tour>(Page);

    public class TourPostPageQueryHandler(WebPageQueryTools tools) : WebPageQueryHandler<TourPostPageQuery, Tour>(tools)
    {
        public override async Task<Tour> Handle(TourPostPageQuery request, CancellationToken cancellationToken = default)
        {
            var q = new ContentItemQueryBuilder().ForWebPage(request.Page, c => c.WithLinkedItems(4));

            var r = await Executor.GetMappedWebPageResult<Tour>(q, DefaultQueryOptions, cancellationToken);

            return r.First();
        }

        protected override ICacheDependencyKeysBuilder AddDependencyKeys(TourPostPageQuery query, Tour result, ICacheDependencyKeysBuilder builder)
        {
            builder = base.AddDependencyKeys(query, result, builder);

            // Image folders
            builder.Collection(
                result.SelectedImageFolders,
                (item, itemBuilder) => itemBuilder.ContentItem(item));

            // Itinerary folders
            builder.Collection(
                result.SelectedItineraryFolders,
                (item, itemBuilder) => itemBuilder.ContentItem(item));

            // Tour highlight folders
            builder.Collection(
                result.SelectedTourHighlightFolders,
                (item, itemBuilder) => itemBuilder.ContentItem(item));

            // Inclusion folders
            builder.Collection(
                result.SelectedInclusionFolders,
                (item, itemBuilder) => itemBuilder.ContentItem(item));

            // Tour pricing folders
            builder.Collection(
                result.SelectedTourPricingFolders,
                (item, itemBuilder) => itemBuilder.ContentItem(item));

            // Document link folders
            builder.Collection(
                result.Selecteddocumentlinkfolders,
                (item, itemBuilder) => itemBuilder.ContentItem(item));

            // Optional extras folders
            builder.Collection(
                result.SelectedOptionalExtrasFolders,
                (item, itemBuilder) => itemBuilder.ContentItem(item));

            // Room upgrade folders
            builder.Collection(
                result.SelectedRoomUpgradeFolders,
                (item, itemBuilder) => itemBuilder.ContentItem(item));

            // Tour upgrades folders
            builder.Collection(
                result.SelectedTourUpgradesFolders,
                (item, itemBuilder) => itemBuilder.ContentItem(item));

            // Generic tour tile folders
            builder.Collection(
                result.SelectedGenericTourTileFolders,
                (item, itemBuilder) => itemBuilder.ContentItem(item));

            return builder;
        }
    }
}
