namespace ETG.Web.Services.Currency;

public static class CurrencyConstants
{
    public const string CODE_AUD = "AUD";
    public const string CODE_NZD = "NZD";
    public const string CODE_GBP = "GBP";
    public const string CODE_EUR = "EUR";
    public const string CODE_USD = "USD";

    public const string SYMBOL_GBP = "£";
    public const string SYMBOL_NZD = "$";
    public const string SYMBOL_AUD = "$";
    public const string SYMBOL_EUR = "€";
    public const string SYMBOL_USD = "$";

    public static readonly IReadOnlyList<string> SupportedCurrencies =
        new[] { CODE_AUD, CODE_NZD, CODE_GBP, CODE_EUR, CODE_USD };

    // Units of target currency per 1 AUD. Fallback only, replaced by API rates later.
    // GBP 0.52 is taken from the old site (AUD 2,190 -> GBP 1,139).
    // The other three are PLACEHOLDERS: replace them with your old site's real rates.
    public static readonly IReadOnlyDictionary<string, decimal> FallbackRates =
        new Dictionary<string, decimal>
        {
            [CODE_AUD] = 1.00m,
            [CODE_GBP] = 0.52m,
            [CODE_NZD] = 1.08m,  // placeholder
            [CODE_EUR] = 0.60m,  // placeholder
            [CODE_USD] = 0.65m   // placeholder
        };

    public static decimal GetFallbackRate(string currencyCode) =>
        FallbackRates.TryGetValue(currencyCode, out var rate) ? rate : 1m;

    public static string GetSymbol(string? currencyCode) => currencyCode switch
    {
        CODE_EUR => SYMBOL_EUR,
        CODE_USD => SYMBOL_USD,
        CODE_NZD => SYMBOL_NZD,
        CODE_GBP => SYMBOL_GBP,
        _ => SYMBOL_AUD
    };
}