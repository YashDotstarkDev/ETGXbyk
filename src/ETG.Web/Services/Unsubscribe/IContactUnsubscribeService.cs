using System;
using System.Threading;
using System.Threading.Tasks;

namespace ETG.Web.Services.Unsubscribe;

public interface IContactUnsubscribeService
{
    Task<bool> SetUnsubscribeReasonAsync(
        int contactId,
        string languageName,
        Guid reasonGuid,
        CancellationToken cancellationToken = default);
}