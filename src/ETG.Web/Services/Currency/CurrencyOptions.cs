// Services/Currency/CurrencyOptions.cs
namespace ETG.Web.Services.Currency;

public class CurrencyOptions
{
    public string BaseCurrency { get; set; } = "AUD";
    public List<string> Supported { get; set; } = new();
    public string FixerApiKey { get; set; } = "";
}