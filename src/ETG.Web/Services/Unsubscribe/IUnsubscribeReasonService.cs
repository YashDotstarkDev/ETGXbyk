using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using ETG.Core.Models;

namespace ETG.Web.Services.Unsubscribe;

public interface IUnsubscribeReasonService
{
    Task<IReadOnlyList<UnsubscribeReasonOption>> GetReasonsAsync(
        string languageName,
        CancellationToken cancellationToken = default);

    Task<UnsubscribeReasonOption?> GetReasonAsync(
        string languageName,
        Guid reasonGuid,
        CancellationToken cancellationToken = default);
}