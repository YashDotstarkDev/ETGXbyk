using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;

using CMS.ContactManagement;
using CMS.DataEngine;

using ETG.Web.Configuration;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace ETG.Web.Services.Unsubscribe;

public sealed record UnsubscribeRecipientTicket(
    int ContactId,
    string PagePath,
    string LanguageName,
    DateTimeOffset ExpiresUtc);

public sealed class UnsubscribeLinkService
{
    private readonly IDataProtector protector;
    private readonly IInfoProvider<ContactInfo> contactProvider;
    private readonly UnsubscribeOptions options;

    public UnsubscribeLinkService(
        IDataProtectionProvider protectionProvider,
        IInfoProvider<ContactInfo> contactProvider,
        IOptions<UnsubscribeOptions> options)
    {
        protector = protectionProvider.CreateProtector(
            "ETG.Unsubscribe.Recipient.v2");

        this.contactProvider = contactProvider;
        this.options = options.Value;
    }

    // Call only after trusted server code identifies the intended contact.
    public async Task<string> CreateLinkAsync(
        int verifiedContactId,
        string pagePath,
        string languageName)
    {
        if (!IsSafePagePath(pagePath))
        {
            throw new ArgumentException(
                "Supply a local page path without query parameters.",
                nameof(pagePath));
        }

        if (string.IsNullOrWhiteSpace(languageName))
        {
            throw new ArgumentException(
                "Supply the content-language code.",
                nameof(languageName));
        }

        var contacts = await contactProvider.Get()
            .WhereEquals(nameof(ContactInfo.ContactID), verifiedContactId)
            .TopN(1)
            .GetEnumerableTypedResultAsync();

        if (!contacts.Any())
        {
            throw new InvalidOperationException("Contact was not found.");
        }

        var ticket = new UnsubscribeRecipientTicket(
            verifiedContactId,
            pagePath,
            languageName,
            DateTimeOffset.UtcNow.AddHours(options.RecipientLinkHours));

        var token = protector.Protect(
            JsonSerializer.Serialize(ticket));

        return QueryHelpers.AddQueryString(
            pagePath,
            "recipient",
            token);
    }

    public bool TryRead(
        string? token,
        out UnsubscribeRecipientTicket? ticket)
    {
        ticket = null;

        if (string.IsNullOrWhiteSpace(token) || token.Length > 8192)
        {
            return false;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<UnsubscribeRecipientTicket>(
                protector.Unprotect(token));

            if (parsed is null
                || parsed.ContactId <= 0
                || parsed.ExpiresUtc <= DateTimeOffset.UtcNow
                || string.IsNullOrWhiteSpace(parsed.LanguageName)
                || !IsSafePagePath(parsed.PagePath))
            {
                return false;
            }

            ticket = parsed;
            return true;
        }
        catch (Exception exception) when (
            exception is CryptographicException
            || exception is JsonException
            || exception is ArgumentException)
        {
            return false;
        }
    }

    private static bool IsSafePagePath(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.StartsWith("/", StringComparison.Ordinal)
            && !value.StartsWith("//", StringComparison.Ordinal)
            && !value.Contains('\\')
            && !value.Contains('?')
            && !value.Contains('#')
            && !value.Any(char.IsControl);
    }
}