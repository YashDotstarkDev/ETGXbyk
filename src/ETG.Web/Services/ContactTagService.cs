using CMS.ContactManagement;
using CMS.DataEngine;
using System.Text.Json;

namespace ETG.Web.Services;

/// <summary>
/// Creates or updates a Kentico ContactInfo record after a form submission
/// and appends the appropriate tag (Website Consumer or Website Trade).
///
/// The Tags field on ContactInfo is stored as a JSON array in the database:
///   ["Website Consumer"]
///   ["Entire Travel Club","Travellers Choice"]
///
/// Tag names match the ETG.ContactTags module objects:
///   Website Consumer  →  GUID A806EB09-88CD-4C5C-BC76-FD17779761E3
///   Website Trade     →  GUID 1AB9EA37-CDC1-4614-9F79-8DDA8391C876
/// </summary>
public sealed class ContactTagService(IInfoProvider<ContactInfo> contactProvider)
{
    public const string TagWebsiteConsumer = "Website Consumer";
    public const string TagWebsiteTrade    = "Website Trade";

    private const string TagsField    = "Tags";
    private const string IsAgentField = "ContactIsAgent";

    /// <summary>
    /// Called after Enquire / Contact Us form is saved.
    /// Always creates or updates the contact with name and phone.
    /// Tags as Website Consumer only when the user opted into the newsletter.
    /// Matches K13 AddContactForWebsiteConsumer behaviour exactly.
    /// </summary>
    public async Task TagEnquireContactAsync(
        string email,
        string firstName,
        string lastName,
        string phone,
        bool subscribeToNewsletter,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        var contact = await GetOrCreateContactAsync(email, cancellationToken);

        if (!string.IsNullOrWhiteSpace(firstName))
        {
            contact.ContactFirstName = firstName;
        }

        if (!string.IsNullOrWhiteSpace(lastName))
        {
            contact.ContactLastName = lastName;
        }

        if (string.IsNullOrWhiteSpace(contact.ContactMobilePhone) &&
            !string.IsNullOrWhiteSpace(phone))
        {
            contact.ContactMobilePhone = phone;
        }

        if (subscribeToNewsletter)
        {
            AppendTag(contact, TagWebsiteConsumer);
        }

        await contactProvider.SetAsync(contact, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Called after Competition / CompetitionAgents form is saved.
    /// Always tags — Website Trade if agent, Website Consumer otherwise.
    /// Matches K13 AddContact behaviour exactly.
    /// </summary>
    public async Task TagCompetitionContactAsync(
        string email,
        string firstName,
        string lastName,
        bool isAgent,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        var contact = await GetOrCreateContactAsync(email, cancellationToken);

        if (!string.IsNullOrWhiteSpace(firstName) &&
            string.IsNullOrWhiteSpace(contact.ContactFirstName))
        {
            contact.ContactFirstName = firstName;
        }

        if (!string.IsNullOrWhiteSpace(lastName) &&
            string.IsNullOrWhiteSpace(contact.ContactLastName))
        {
            contact.ContactLastName = lastName;
        }

        contact.SetValue(IsAgentField, isAgent);

        AppendTag(contact, isAgent ? TagWebsiteTrade : TagWebsiteConsumer);

        await contactProvider.SetAsync(contact, cancellationToken: cancellationToken);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<ContactInfo> GetOrCreateContactAsync(
        string email,
        CancellationToken cancellationToken)
    {
        var results = await contactProvider.Get()
            .WhereEquals(nameof(ContactInfo.ContactEmail), email.Trim())
            .TopN(1)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        var contact = results.FirstOrDefault();

        if (contact is not null)
        {
            return contact;
        }

        contact = new ContactInfo { ContactEmail = email.Trim() };
        await contactProvider.SetAsync(contact, cancellationToken: cancellationToken);
        return contact;
    }

    /// <summary>
    /// Reads the existing Tags JSON array, adds the new tag if not already present,
    /// then writes it back as a JSON array.
    /// Database format: ["Website Consumer","Website Trade"]
    /// </summary>
    private static void AppendTag(ContactInfo contact, string tag)
    {
        var raw = contact.GetStringValue(TagsField, string.Empty);

        List<string> tags = [];

        if (!string.IsNullOrWhiteSpace(raw))
        {
            try
            {
                tags = JsonSerializer.Deserialize<List<string>>(raw) ?? [];
            }
            catch (JsonException)
            {
                tags = [];
            }
        }

        if (!tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)))
        {
            tags.Add(tag);
        }

        contact.SetValue(TagsField, JsonSerializer.Serialize(tags));
    }
}
