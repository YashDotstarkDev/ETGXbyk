// Services/Currency/ConstantExchangeRateProvider.cs
using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ETG.Web.Services.Currency;

public class ConstantExchangeRateProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<CurrencyOptions> options,
    ILogger<ConstantExchangeRateProvider> logger) : IExchangeRateProvider
{
    private const string HttpClientName = "FixerClient";

    private readonly IHttpClientFactory httpClientFactory = httpClientFactory;
    private readonly CurrencyOptions options = options.Value;
    private readonly ILogger<ConstantExchangeRateProvider> logger = logger;
    private readonly ConcurrentDictionary<string, decimal> liveRates = new(StringComparer.OrdinalIgnoreCase);

    public decimal GetRate(string currencyCode) =>
        liveRates.TryGetValue(currencyCode, out var rate)
            ? rate
            : CurrencyConstants.GetFallbackRate(currencyCode);

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.FixerApiKey))
        {
            logger.LogInformation("Fixer API key not set; using fallback currency rates.");
            return;
        }

        try
        {
            var client = httpClientFactory.CreateClient(HttpClientName);

            var symbols = string.Join(',', options.Supported.Where(c => c != "EUR"));
            var url = $"latest?access_key={options.FixerApiKey}&symbols={symbols}";

            logger.LogInformation("Requesting currency rates from Fixer: {Url}", url.Replace(options.FixerApiKey, "****"));

            var result = await client.GetFromJsonAsync<FixerRatesResponse>(url, cancellationToken);

            if (result is null || !result.Success)
            {
                logger.LogWarning("Fixer request failed: {Reason}. Using fallback rates.", result?.Error?.Type ?? "unknown error");
                return;
            }

            if (!result.Rates.TryGetValue("AUD", out var eurToAud) || eurToAud == 0)
            {
                logger.LogWarning("Fixer response missing AUD rate. Using fallback rates.");
                return;
            }

            liveRates["AUD"] = 1m;
            liveRates["EUR"] = 1m / eurToAud;

            foreach (var (code, eurToTarget) in result.Rates)
            {
                if (code != "AUD")
                {
                    liveRates[code] = eurToTarget / eurToAud;
                }
            }

            logger.LogInformation("Currency rates refreshed from Fixer at {Time}", DateTimeOffset.UtcNow);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to refresh currency rates from Fixer; continuing to use last known/fallback rates");
        }
    }

    private class FixerRatesResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("rates")]
        public Dictionary<string, decimal> Rates { get; set; } = new();

        [JsonPropertyName("error")]
        public FixerErrorDetail? Error { get; set; }
    }

    private class FixerErrorDetail
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
    }
}