using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CMS.ContentEngine;
using CMS.Websites;
using ETG.Core.Content;
using ETG.Core.Operations;

namespace ETG.Web.Features.Destinations.Operations;

public record GetParentDestinationGuidQuery(string ChannelName, int ParentPageId)
    : IQuery<Guid?>, IChannelContentQuery, ICacheByValueQuery
{
    string IChannelContentQuery.ChannelName => ChannelName;
    public string CacheValueKey => ParentPageId.ToString();
}

public class GetParentDestinationGuidQueryHandler(ContentItemQueryTools tools)
    : ContentItemQueryHandler<GetParentDestinationGuidQuery, Guid?>(tools)
{
    public override async Task<Guid?> Handle(
        GetParentDestinationGuidQuery request,
        CancellationToken cancellationToken = default)
    {
        if (request.ParentPageId <= 0) return null;

        var query = new ContentItemQueryBuilder()
            .ForContentType(
                Destination.CONTENT_TYPE_NAME,
                config => config
                    .ForWebsite(request.ChannelName)
                    .WithLinkedItems(0)
                    .Where(where => where.WhereEquals(
                        nameof(WebPageFields.WebPageItemID),
                        request.ParentPageId)));

        var result = await Executor.GetMappedWebPageResult<Destination>(
            query,
            DefaultQueryOptions,
            cancellationToken);

        return result.FirstOrDefault()?.SystemFields.WebPageItemGUID;
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(
        GetParentDestinationGuidQuery query,
        Guid? result,
        ICacheDependencyKeysBuilder builder)
    {
        return builder
            .WebPage(query.ParentPageId)
            .AllContentItems(Destination.CONTENT_TYPE_NAME)
            .AllWebPages(query.ChannelName);
    }
}
