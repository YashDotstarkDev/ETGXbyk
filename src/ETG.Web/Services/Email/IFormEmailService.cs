namespace ETG.Web.Services.Email;

public interface IFormEmailService
{
    Task SendCustomerConfirmationAsync(
        string formName,
        string recipientEmail,
        IReadOnlyDictionary<string, string?> formData);
}