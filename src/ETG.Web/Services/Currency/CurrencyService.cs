
using Microsoft.AspNetCore.Http;

namespace ETG.Web.Services.Currency;

public class CurrencyService : ICurrencyService
{
    private const string CURRENCY_COOKIE = "CurrentCurrency";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IExchangeRateProvider _rates;

    public CurrencyService(IHttpContextAccessor httpContextAccessor, IExchangeRateProvider rates)
    {
        _httpContextAccessor = httpContextAccessor;
        _rates = rates;
    }

    public IReadOnlyList<string> GetSupportedCurrencies() => CurrencyConstants.SupportedCurrencies;

    public bool IsValidCurrencyCode(string? currencyCode) =>
        !string.IsNullOrWhiteSpace(currencyCode) &&
        CurrencyConstants.SupportedCurrencies.Contains(currencyCode, StringComparer.OrdinalIgnoreCase);

    public string GetCurrentCurrency()
    {
        var context = _httpContextAccessor.HttpContext;
        var cookieValue = context?.Request.Cookies[CURRENCY_COOKIE];

        if (IsValidCurrencyCode(cookieValue))
        {
            return cookieValue!.ToUpperInvariant();
        }

        var host = context?.Request.Host.Host ?? string.Empty;
        return host.Contains("nz", StringComparison.OrdinalIgnoreCase)
            ? CurrencyConstants.CODE_NZD
            : CurrencyConstants.CODE_AUD;
    }

    public void SetCurrentCurrency(string currencyCode)
    {
        _httpContextAccessor.HttpContext?.Response.Cookies.Append(
            CURRENCY_COOKIE,
            currencyCode.ToUpperInvariant(),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                Path = "/",
                Secure = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = true // confirm against your cookie-consent setup
            });
    }

    public decimal GetConversionRate(string currencyCode) =>
        string.Equals(currencyCode, CurrencyConstants.CODE_AUD, StringComparison.OrdinalIgnoreCase)
            ? 1m
            : _rates.GetRate(currencyCode.ToUpperInvariant());
}