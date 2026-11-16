using CMS.ContentEngine;
using CMS.Websites;
using ETG.Core.Operations;

using ExperienceContent = ETG.Core.Content.Experience;

namespace ETG.Web.Features.TourPackage.Operations;
/// <summary>
/// Query to retrieve experience pages by their GUIDs
/// CACHE FIX: Implements ICacheByValueQuery to prevent cache collisions
/// </summary>
public record GetExperiencesQuery(
    string ChannelName,
    IEnumerable<Guid> ExperienceGuids
) : IQuery<List<ExperienceContent>>, IChannelContentQuery, ICacheByValueQuery
{
    string IChannelContentQuery.ChannelName => ChannelName;
    
    // CACHE KEY FIX: Include all experience GUIDs to prevent cache collisions
    public string CacheValueKey => string.Join("|", ExperienceGuids.OrderBy(g => g));
}

public class GetExperiencesQueryHandler(ContentItemQueryTools tools)
    : ContentItemQueryHandler<GetExperiencesQuery, List<ExperienceContent>>(tools)
{
    public override async Task<List<ExperienceContent>> Handle(
        GetExperiencesQuery request,
        CancellationToken cancellationToken)
    {
        var guids = request.ExperienceGuids
            .Where(g => g != Guid.Empty)
            .Distinct()
            .ToArray();

        if (guids.Length == 0)
        {
            return [];
        }

        var query = new ContentItemQueryBuilder()
            .ForContentType(
                ExperienceContent.CONTENT_TYPE_NAME,
                config => config
                    .ForWebsite(request.ChannelName)
                    .Where(w => w.WhereIn("WebPageItemGUID", guids)));

        var experiences = await Executor.GetMappedWebPageResult<ExperienceContent>(
            query,
            DefaultQueryOptions,
            cancellationToken);

        // Maintain order from input GUIDs
        var lookup = experiences.ToDictionary(
            e => e.SystemFields.WebPageItemGUID);

        return guids
            .Where(guid => lookup.ContainsKey(guid))
            .Select(guid => lookup[guid])
            .ToList();
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(
        GetExperiencesQuery query,
        List<ExperienceContent> result,
        ICacheDependencyKeysBuilder builder)
    {
        // CACHE DEPENDENCY FIX: Add dependencies for queried experiences
        foreach (var item in result)
        {
            builder = builder.ContentItem(item);
        }
        
        // Also add dependencies for all requested GUIDs (handles deleted/unpublished items)
        foreach (var guid in query.ExperienceGuids.Where(g => g != Guid.Empty))
        {
            builder = builder.WebPage(guid);
        }
        
        return builder;
    }
}
