using CMS.ContentEngine;
using CMS.Websites;
using ETG.Core.Content;
using ETG.Core.Operations;

using ExperiencePage = ETG.Core.Content.Experience;

namespace ETG.Web.Features.Home.Operations;

public record GetSelectedExperiencesQuery(
    string ChannelName,
    IReadOnlyList<Guid> SelectedGuids
) : IQuery<IReadOnlyList<ExperiencePage>>, IChannelContentQuery, ICacheByValueQuery
{
    string IChannelContentQuery.ChannelName => ChannelName;

    public string CacheValueKey =>
        string.Join("|", SelectedGuids.Select(g => g.ToString()).OrderBy(s => s, StringComparer.Ordinal));
}

public sealed class GetSelectedExperiencesQueryHandler(
    ContentItemQueryTools tools
) : ContentItemQueryHandler<GetSelectedExperiencesQuery, IReadOnlyList<ExperiencePage>>(tools)
{
    public override async Task<IReadOnlyList<ExperiencePage>> Handle(
        GetSelectedExperiencesQuery request,
        CancellationToken cancellationToken)
    {
        // Guard already enforced by the controller, but be defensive.
        if (request.SelectedGuids.Count == 0)
            return [];

        var query = new ContentItemQueryBuilder()
            .ForContentType(
                ExperiencePage.CONTENT_TYPE_NAME,
                config => config
                    .ForWebsite(request.ChannelName)
                    .Where(w => w.WhereIn(
                        nameof(WebPageFields.WebPageItemGUID),
                        request.SelectedGuids.ToArray()))
                    .WithLinkedItems(0));

        var pages = await Executor.GetMappedWebPageResult<ExperiencePage>(
            query,
            DefaultQueryOptions,
            cancellationToken);

        return pages.ToList();
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(
        GetSelectedExperiencesQuery query,
        IReadOnlyList<ExperiencePage> result,
        ICacheDependencyKeysBuilder builder)
    {
        builder.WebPageType(ExperiencePage.CONTENT_TYPE_NAME, query.ChannelName);

        builder.Collection(result, (exp, b) => b.WebPage(exp));

        var returnedGuids = result
            .Select(e => e.SystemFields.WebPageItemGUID)
            .ToHashSet();

        foreach (var guid in query.SelectedGuids)
        {
            if (!returnedGuids.Contains(guid))
                builder.WebPage(guid);
        }

        return builder;
    }
}
