namespace ETG.Core.Models.Payment;

public class TravelPayOptions
{
    public const string SECTION_NAME = "TravelPay";

    public string BaseUrl { get; set; } = string.Empty;
    public string PluginBaseUrl { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiUsername { get; set; } = string.Empty;
    public string ApiPassword { get; set; } = string.Empty;
    public string MerchantCode { get; set; } = string.Empty;
    public string TravelpayCSSLink { get; set; } = string.Empty;
    public string TravelpayJSLink { get; set; } = string.Empty;
    public string TravelpayBS5JSLink { get; set; } = string.Empty;
    public string ResultUrl { get; set; } = "/payment/process";
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderDisplayName { get; set; } = string.Empty;
    public string MerchantEmail { get; set; } = string.Empty;
}
