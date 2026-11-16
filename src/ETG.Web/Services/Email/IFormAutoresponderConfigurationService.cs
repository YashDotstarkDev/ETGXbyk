namespace ETG.Web.Services.Email;

public interface IFormAutoresponderConfigurationService
{
    Task<FormAutoresponderConfiguration?> GetConfigurationAsync(
        string formName);
}