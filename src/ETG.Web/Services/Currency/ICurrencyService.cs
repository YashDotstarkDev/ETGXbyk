namespace ETG.Web.Services.Currency;

public interface ICurrencyService
{
    IReadOnlyList<string> GetSupportedCurrencies();
    bool IsValidCurrencyCode(string? currencyCode);
    string GetCurrentCurrency();
    void SetCurrentCurrency(string currencyCode);
    decimal GetConversionRate(string currencyCode);
}
