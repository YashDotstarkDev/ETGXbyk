using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using CMS.Helpers;
using ETG.Core.Content;
using ETG.Core.Operations;

namespace ETG.Web.Features.TourPackage.Operations;

/// <summary>
/// Query to retrieve FAQs for a tour
/// CACHE FIX: Implements ICacheByValueQuery to prevent cache collisions
/// </summary>
public record GetTourFaqsQuery(
    string ChannelName,
    IEnumerable<Folder>? TourFaqFolders
) : IQuery<List<FAQ>>, IChannelContentQuery, ICacheByValueQuery
{
    string IChannelContentQuery.ChannelName => ChannelName;
    
    // CACHE KEY FIX: Include folder IDs to prevent cache collisions
    public string CacheValueKey => TourFaqFolders != null && TourFaqFolders.Any()
        ? string.Join("|", TourFaqFolders
            .Where(f => f?.SystemFields != null)
            .Select(f => f.SystemFields.WebPageItemID)
            .OrderBy(id => id))
        : "default-faqs";
}

public class GetTourFaqsQueryHandler(
    ContentItemQueryTools tools)
    : ContentItemQueryHandler<GetTourFaqsQuery, List<FAQ>>(tools)
{
    public override async Task<List<FAQ>> Handle(
        GetTourFaqsQuery request,
        CancellationToken cancellationToken)
    {

        if (request.TourFaqFolders != null &&
            request.TourFaqFolders.Any())
        {
            return request.TourFaqFolders
                .Where(folder => folder != null)
                .SelectMany(folder =>
                    folder.SelectedFAQs
                    ?? Enumerable.Empty<FAQ>())
                .Where(faq => faq != null)
                .ToList();
        }

        var defaultQuery = new ContentItemQueryBuilder()
            .ForContentType(
                Folder.CONTENT_TYPE_NAME,
                config => config
                    .WithLinkedItems(1)
                    .ForWebsite(
                        request.ChannelName,
                        pathMatch: PathMatch.Single(
                            "/Shared/FAQs")));

        var defaultFolders =
            await Executor.GetMappedWebPageResult<Folder>(
                defaultQuery,
                DefaultQueryOptions,
                cancellationToken);

        var defaultFolder =
            defaultFolders.FirstOrDefault();

        if (defaultFolder == null)
        {
            return [];
        }

        return (defaultFolder.SelectedFAQs
                ?? Enumerable.Empty<FAQ>())
            .Where(faq => faq != null)
            .ToList();
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(
        GetTourFaqsQuery query,
        List<FAQ> result,
        ICacheDependencyKeysBuilder builder)
    {
        // CACHE DEPENDENCY FIX: Include folder dependencies
        builder = builder.AllContentItems(FAQ.CONTENT_TYPE_NAME);
        
        // Add dependencies for each returned FAQ
        foreach (var faq in result)
        {
            builder = builder.ContentItem(faq);
        }
        
        // Add dependencies for the folders that contain the FAQs
        if (query.TourFaqFolders != null)
        {
            foreach (var folder in query.TourFaqFolders.Where(f => f?.SystemFields != null))
            {
                builder = builder.WebPage(folder.SystemFields.WebPageItemID);
            }
        }
        else
        {
            // Add dependency for the default FAQ folder at /Shared/FAQs
            builder = builder.AllWebPages(Folder.CONTENT_TYPE_NAME);
        }
        
        return builder;
    }
}