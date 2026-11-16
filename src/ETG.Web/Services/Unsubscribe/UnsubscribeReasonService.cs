using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.ContentEngine;

using ETG.Core.Models;

using ReasonContent = ETG.Core.Content.UnsubscribeReason;

namespace ETG.Web.Services.Unsubscribe;

public sealed class UnsubscribeReasonService : IUnsubscribeReasonService
{
    private readonly IContentQueryExecutor executor;

    public UnsubscribeReasonService(IContentQueryExecutor executor)
    {
        this.executor = executor;
    }

    public async Task<IReadOnlyList<UnsubscribeReasonOption>>GetReasonsAsync(string languageName,CancellationToken cancellationToken = default)
    {
        var query = new ContentItemQueryBuilder()
            .ForContentType(
                ReasonContent.CONTENT_TYPE_NAME,
                parameters => parameters.OrderBy(
                    nameof(ReasonContent.SortOrder),
                    nameof(ReasonContent.Reason)))
            .InLanguage(
                languageName,
                useLanguageFallbacks: true);

        var items = await executor.GetMappedResult<ReasonContent>(
            query,
            new ContentQueryExecutionOptions
            {
                ForPreview = false
            },
            cancellationToken: cancellationToken);

        return items
            .Select(item => new UnsubscribeReasonOption(
                item.SystemFields.ContentItemGUID,
                item.Reason))
            .ToArray();
    }

    public async Task<UnsubscribeReasonOption?> GetReasonAsync(string languageName,Guid reasonGuid,CancellationToken cancellationToken = default)
    {
        if (reasonGuid == Guid.Empty)
        {
            return null;
        }

        var reasons = await GetReasonsAsync(
            languageName,
            cancellationToken);

        return reasons.SingleOrDefault(
            reason => reason.ItemGuid == reasonGuid);
    }
}