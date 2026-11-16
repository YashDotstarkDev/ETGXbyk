using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.ContactManagement;
using CMS.DataEngine;

namespace ETG.Web.Services.Unsubscribe;

public sealed class ContactUnsubscribeService : IContactUnsubscribeService
{
    private const string ReasonField = "UnsubscribeReason";

    private readonly IInfoProvider<ContactInfo> contactProvider;
    private readonly IUnsubscribeReasonService reasonService;

    public ContactUnsubscribeService(
        IInfoProvider<ContactInfo> contactProvider,
        IUnsubscribeReasonService reasonService)
    {
        this.contactProvider = contactProvider;
        this.reasonService = reasonService;
    }

    public async Task<bool> SetUnsubscribeReasonAsync(
        int contactId,
        string languageName,
        Guid reasonGuid,
        CancellationToken cancellationToken = default)
    {
        if (contactId <= 0 || reasonGuid == Guid.Empty)
        {
            return false;
        }

        // Revalidate against currently published choices.
        var reason = await reasonService.GetReasonAsync(
            languageName,
            reasonGuid,
            cancellationToken);

        if (reason is null)
        {
            return false;
        }

        var contacts = await contactProvider.Get()
            .WhereEquals(nameof(ContactInfo.ContactID), contactId)
            .TopN(1)
            .GetEnumerableTypedResultAsync();

        var contact = contacts.FirstOrDefault();

        if (contact is null)
        {
            return false;
        }

        contact.SetValue(ReasonField, reason.Text);

        await contactProvider.SetAsync(
            contact,
            cancellationToken: cancellationToken);

        return true;
    }
}