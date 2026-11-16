namespace ETG.Web.Services.Currency;

public interface ICurrencyService
{
    IReadOnlyList<string> GetSupportedCurrencies();
    Task<IReadOnlyList<string>> GetSupportedCurrenciesAsync(CancellationToken cancellationToken = default);
    bool IsValidCurrencyCode(string? currencyCode);
    Task<bool> IsValidCurrencyCodeAsync(string? currencyCode, CancellationToken cancellationToken = default);
    string GetCurrentCurrency();
    void SetCurrentCurrency(string currencyCode);
    decimal GetConversionRate(string currencyCode);
    Task<decimal> GetConversionRateAsync(string currencyCode, CancellationToken cancellationToken = default);
}
