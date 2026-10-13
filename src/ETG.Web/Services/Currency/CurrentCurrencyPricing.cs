namespace ETG.Web.Services.Currency;

public class CurrentCurrencyPricing
{
    public CurrentCurrencyPricing(string? currency, decimal conversionRate)
    {
        CurrentCurrency = string.IsNullOrEmpty(currency) ? CurrencyConstants.CODE_AUD : currency;
        ConversionRate = conversionRate;
    }

    public string CurrentCurrency { get; }
    public decimal ConversionRate { get; }
    public string CurrentCurrencySymbol => CurrencyConstants.GetSymbol(CurrentCurrency);
    public bool CurrentCurrencyIsAUD => CurrentCurrency == CurrencyConstants.CODE_AUD;
    public bool CurrencyAppliesDiscounts =>
        CurrentCurrency == CurrencyConstants.CODE_AUD || CurrentCurrency == CurrencyConstants.CODE_NZD;

    public int ConvertAUDToCurrentCurrency(decimal price) =>
        CurrentCurrencyIsAUD ? (int)price : (int)Math.Ceiling(price * ConversionRate);

    public int ConvertAUDToCurrentCurrency(double price) => ConvertAUDToCurrentCurrency((decimal)price);

    public int ConvertAUDToCurrentCurrency(int price) => ConvertAUDToCurrentCurrency((decimal)price);

    public int ConvertCurrentCurrencyToAUD(decimal price) =>
        CurrentCurrencyIsAUD || ConversionRate == 0
            ? (int)price
            : (int)Math.Ceiling(price / ConversionRate);

    public int ConvertCurrentCurrencyToAUD(double price) => ConvertCurrentCurrencyToAUD((decimal)price);

    /// <summary>e.g. "GBP 1,139"</summary>
    public string GetCurrentCurrencyDisplayPrice(decimal audPrice) =>
        $"{CurrentCurrency} {ConvertAUDToCurrentCurrency(audPrice):#,###}";

    public string GetCurrentCurrencyDisplayPrice(double audPrice) =>
        GetCurrentCurrencyDisplayPrice((decimal)audPrice);

    public string GetCurrentCurrencyDisplayPrice(int audPrice) =>
        GetCurrentCurrencyDisplayPrice((decimal)audPrice);
}
