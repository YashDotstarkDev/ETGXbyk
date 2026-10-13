using ETG.Core.Content;

namespace ETG.Core.Models;

public sealed class ContactUsPageViewModel
{
    public Page Page { get; set; } = default!;

    public ContactUsFormViewModel Form { get; set; } = new();
}

public sealed class ContactUsFormViewModel
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string PreferredContactType { get; set; } = "Email";

    public string Message { get; set; } = string.Empty;

    public bool SubscribeToNewsletter { get; set; }

    // K13 honeypot field
    public string Friend { get; set; } = string.Empty;

    public string ErrorMessage { get; set; } = string.Empty;
}