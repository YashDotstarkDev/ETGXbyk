using CMS.ContentEngine;
using CMS.Websites;
using Etg;
using ETG.Core.Content;
using ETG.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace ETG.Web.Features.TourPackage.Operations;

public record TourPostPageQuery(RoutedWebPage Page)
    : WebPageRoutedQuery<Tour>(Page);

public class TourPostPageQueryHandler(
    WebPageQueryTools tools)
    : WebPageQueryHandler<TourPostPageQuery, Tour>(tools)
{
    public override async Task<Tour> Handle(
        TourPostPageQuery request,
        CancellationToken cancellationToken = default)
    {

        var query = new ContentItemQueryBuilder()
            .ForWebPage(
                request.Page,
                config => config.WithLinkedItems(1));

        var result = await Executor.GetMappedWebPageResult<Tour>(
            query,
            DefaultQueryOptions,
            cancellationToken);

        var tour = result.First();

        await HydrateTourLinkedContent(
            tour,
            cancellationToken);

        return tour;
    }

    private async Task HydrateTourLinkedContent(
        Tour tour,
        CancellationToken cancellationToken)
    {
        var imageFoldersTask =
            LoadContentItems<ImageFolder>(
                ImageFolder.CONTENT_TYPE_NAME,
                GetContentItemGuids(tour.SelectedImageFolders),
                linkedItemsDepth: 2,
                cancellationToken);

        var itineraryFoldersTask =
            LoadContentItems<ItineraryFolder>(
                ItineraryFolder.CONTENT_TYPE_NAME,
                GetContentItemGuids(tour.SelectedItineraryFolders),
                linkedItemsDepth: 2,
                cancellationToken);

        var highlightFoldersTask =
            LoadContentItems<TourHighlightFolder>(
                TourHighlightFolder.CONTENT_TYPE_NAME,
                GetContentItemGuids(tour.SelectedTourHighlightFolders),
                linkedItemsDepth: 1,
                cancellationToken);

        var inclusionFoldersTask =
            LoadContentItems<InclusionFolder>(
                InclusionFolder.CONTENT_TYPE_NAME,
                GetContentItemGuids(tour.SelectedInclusionFolders),
                linkedItemsDepth: 1,
                cancellationToken);

        var pricingFoldersTask =
            LoadContentItems<TourPricingFolder>(
                TourPricingFolder.CONTENT_TYPE_NAME,
                GetContentItemGuids(tour.SelectedTourPricingFolders),
                linkedItemsDepth: 1,
                cancellationToken);

        var documentFoldersTask =
            LoadContentItems<Documentlinkfolder>(
                Documentlinkfolder.CONTENT_TYPE_NAME,
                GetContentItemGuids(tour.Selecteddocumentlinkfolders),
                linkedItemsDepth: 3,
                cancellationToken);

        var optionalExtrasFoldersTask =
            LoadContentItems<OptionalExtrasFolder>(
                OptionalExtrasFolder.CONTENT_TYPE_NAME,
                GetContentItemGuids(tour.SelectedOptionalExtrasFolders),
                linkedItemsDepth: 2,
                cancellationToken);

        var tourUpgradesFoldersTask =
    LoadContentItems<TourUpgradesFolder>(
        TourUpgradesFolder.CONTENT_TYPE_NAME,
        GetContentItemGuids(tour.SelectedTourUpgradesFolders),
        linkedItemsDepth: 1,
        cancellationToken);

        var roomUpgradeFoldersTask =
            LoadContentItems<RoomUpgradeFolder>(
                RoomUpgradeFolder.CONTENT_TYPE_NAME,
                GetContentItemGuids(tour.SelectedRoomUpgradeFolders),
                linkedItemsDepth: 2,
                cancellationToken);

        var discountFoldersTask =
            LoadContentItems<DiscountAndOfferFolder>(
                DiscountAndOfferFolder.CONTENT_TYPE_NAME,
                GetContentItemGuids(tour.TourDiscountAndOffer),
                linkedItemsDepth: 4,
                cancellationToken);

        var faqFoldersTask =
            LoadContentItems<Folder>(
                Folder.CONTENT_TYPE_NAME,
                GetContentItemGuids(tour.TourFAQFolder),
                linkedItemsDepth: 1,
                cancellationToken);

        await Task.WhenAll(
            imageFoldersTask,
            itineraryFoldersTask,
            highlightFoldersTask,
            inclusionFoldersTask,
            pricingFoldersTask,
            documentFoldersTask,
            optionalExtrasFoldersTask,
            roomUpgradeFoldersTask,
            faqFoldersTask,
            tourUpgradesFoldersTask,
            discountFoldersTask);

        tour.SelectedImageFolders =
            imageFoldersTask.Result;

        tour.SelectedItineraryFolders =
            itineraryFoldersTask.Result;

        tour.SelectedTourHighlightFolders =
            highlightFoldersTask.Result;

        tour.SelectedInclusionFolders =
            inclusionFoldersTask.Result;

        tour.SelectedTourPricingFolders =
            pricingFoldersTask.Result;

        tour.Selecteddocumentlinkfolders =
            documentFoldersTask.Result;

        tour.SelectedOptionalExtrasFolders =
            optionalExtrasFoldersTask.Result;

        tour.SelectedRoomUpgradeFolders =
            roomUpgradeFoldersTask.Result;

        tour.TourDiscountAndOffer =
            discountFoldersTask.Result;

        tour.SelectedTourUpgradesFolders =
    tourUpgradesFoldersTask.Result;

        tour.TourFAQFolder =
            faqFoldersTask.Result;
    }

    private async Task<List<T>> LoadContentItems<T>(
        string contentTypeName,
        IEnumerable<Guid> guids,
        int linkedItemsDepth,
        CancellationToken cancellationToken)
        where T : class, IContentItemFieldsSource
    {
        var ids = guids
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return [];
        }

        var query = new ContentItemQueryBuilder()
            .ForContentType(
                contentTypeName,
                config => config
                    .WithLinkedItems(linkedItemsDepth)
                    .Where(where =>
                        where.WhereIn(
                            nameof(ContentItemFields.ContentItemGUID),
                            ids)));

        var result =
            await Executor.GetMappedResult<T>(
                query,
                DefaultQueryOptions,
                cancellationToken);

        return result.ToList();
    }

    private static IEnumerable<Guid> GetContentItemGuids<T>(
        IEnumerable<T>? items)
        where T : class, IContentItemFieldsSource
    {
        return items?
            .Where(x => x?.SystemFields != null)
            .Select(x => x.SystemFields.ContentItemGUID)
            .Where(x => x != Guid.Empty)
            .Distinct()
            ?? [];
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(
        TourPostPageQuery query,
        Tour result,
        ICacheDependencyKeysBuilder builder)
    {
        builder = base.AddDependencyKeys(
            query,
            result,
            builder);

        builder.Collection(
            result.SelectedImageFolders,
            (item, itemBuilder) =>
                itemBuilder.ContentItem(item));

        builder.Collection(
            result.SelectedItineraryFolders,
            (item, itemBuilder) =>
                itemBuilder.ContentItem(item));

        builder.Collection(
            result.SelectedTourHighlightFolders,
            (item, itemBuilder) =>
                itemBuilder.ContentItem(item));

        builder.Collection(
            result.SelectedInclusionFolders,
            (item, itemBuilder) =>
                itemBuilder.ContentItem(item));

        builder.Collection(
            result.SelectedTourPricingFolders,
            (item, itemBuilder) =>
                itemBuilder.ContentItem(item));

        builder.Collection(
            result.Selecteddocumentlinkfolders,
            (item, itemBuilder) =>
                itemBuilder.ContentItem(item));

        builder.Collection(
            result.SelectedOptionalExtrasFolders,
            (item, itemBuilder) =>
                itemBuilder.ContentItem(item));

        builder.Collection(
            result.SelectedRoomUpgradeFolders,
            (item, itemBuilder) =>
                itemBuilder.ContentItem(item));

        builder.Collection(
    result.SelectedTourUpgradesFolders,
    (item, itemBuilder) =>
        itemBuilder.ContentItem(item));

        builder.Collection(
            result.TourDiscountAndOffer,
            (item, itemBuilder) =>
                itemBuilder.ContentItem(item));

        return builder;
    }
}