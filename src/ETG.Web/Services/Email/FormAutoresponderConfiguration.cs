namespace ETG.Web.Services.Email;

public sealed class FormAutoresponderConfiguration
{
    public bool IsEnabled { get; init; }
    public Guid? EmailConfigurationGuid { get; init; }
}