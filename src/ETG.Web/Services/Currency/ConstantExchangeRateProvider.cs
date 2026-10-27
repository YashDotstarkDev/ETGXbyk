using CMS.Core;
using CMS.DataEngine;
using CMS.Scheduler;
using ETG.Core.Modules;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

[assembly: RegisterScheduledTask(identifier: "ETG.CurrencyRateUpdate", typeof(ETG.Web.Services.Currency.CurrencyRateScheduledTask))]

namespace ETG.Web.Services.Currency;

public class ConstantExchangeRateProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<CurrencyOptions> options,
    IInfoProvider<FexExchangeInfo> fexProvider,
    IMemoryCache memoryCache,
    IEventLogService eventLog) : IExchangeRateProvider
{
    private const string HttpClientName = "FixerClient";
    private const string CacheKey = "ETG.Currency.Rates";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    private readonly CurrencyOptions options = options.Value;

    // ---------- Reading (used on every page request) ----------

    public decimal GetRate(string currencyCode)
    {
        var rates = memoryCache.GetOrCreate(CacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return LoadRatesFromDatabase();
        })!;

        return rates.TryGetValue(currencyCode, out var rate)
            ? rate
            : CurrencyConstants.GetFallbackRate(currencyCode);
    }

    private Dictionary<string, decimal> LoadRatesFromDatabase()
    {
        try
        {
            return fexProvider.Get()
    .WhereEquals(nameof(FexExchangeInfo.SourceCurrency), options.BaseCurrency)
    .GetEnumerableTypedResult()
    .Where(r => r.TargetCurrencyValue > 0)
    .ToDictionary(
        r => r.TargetCurrency,
        r => r.TargetCurrencyValue,
        StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            eventLog.LogException("CONVERSION", "LOAD", ex);
            return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase); // empty -> constants are used
        }
    }

    // ---------- Writing (called only by the scheduled task) ----------

    public async Task<string> RefreshAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.FixerApiKey))
        {
            return "Fixer API key is not set; fallback rates remain in use.";
        }

        var client = httpClientFactory.CreateClient(HttpClientName);

        // Works on every Fixer plan (free plans only allow EUR as the base).
        var symbols = string.Join(',', options.Supported.Where(c => c != "EUR"));
        var url = $"latest?access_key={options.FixerApiKey}&symbols={symbols}";

        var response = await client.GetAsync(url, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var snippet = body.Length > 300 ? body[..300] : body;
            throw new InvalidOperationException($"Fixer returned {(int)response.StatusCode}: {snippet}");
        }

        var result = System.Text.Json.JsonSerializer.Deserialize<FixerRatesResponse>(body);

        if (result is null || !result.Success)
        {
            throw new InvalidOperationException($"Fixer error: {result?.Error?.Type ?? "unknown"}");
        }

        if (!result.Rates.TryGetValue("AUD", out var eurToAud) || eurToAud == 0)
        {
            throw new InvalidOperationException("Fixer response did not include an AUD rate.");
        }

        var saved = new List<string>();

        // EUR -> X and EUR -> AUD from Fixer, so AUD -> X = (EUR -> X) / (EUR -> AUD)
        Save(options.BaseCurrency, "EUR", 1m / eurToAud);
        saved.Add("EUR");

        foreach (var (code, eurToTarget) in result.Rates)
        {
            if (code == "AUD")
            {
                continue;
            }

            Save(options.BaseCurrency, code, eurToTarget / eurToAud);
            saved.Add(code);
        }

        memoryCache.Remove(CacheKey);

        return $"Saved rates for {string.Join(", ", saved)} at {DateTime.Now}.";
    }

    private void Save(string source, string target, decimal rate)
    {
        // Same safety-margin formula as the old CurrencyService.AdjustConversion
        var adjusted = (Math.Truncate(rate * 100) - 1) / 100;

        var item = fexProvider.Get()
            .WhereEquals(nameof(FexExchangeInfo.SourceCurrency), source)
            .WhereEquals(nameof(FexExchangeInfo.TargetCurrency), target)
            .GetEnumerableTypedResult()
            .FirstOrDefault()
            ?? new FexExchangeInfo { SourceCurrency = source, TargetCurrency = target };

        item.TargetCurrencyValue = rate;
        item.AdjustedTargetValue = adjusted;
        fexProvider.Set(item);

        eventLog.LogInformation("CONVERSION", "SAVE",
            $"SOURCE={source}, TARGET={target}, RATEFROMAPI={rate}, Adjusted={adjusted}");
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

public class CurrencyRateScheduledTask(
    ConstantExchangeRateProvider provider,
    IEventLogService eventLog) : IScheduledTask
{
    public async Task<ScheduledTaskExecutionResult> Execute(
        ScheduledTaskConfigurationInfo task, CancellationToken cancellationToken)
    {
        try
        {
            var message = await provider.RefreshAsync(cancellationToken);
            return new ScheduledTaskExecutionResult(message);
        }
        catch (Exception ex)
        {
            eventLog.LogException("CONVERSION", "GET", ex);
            return new ScheduledTaskExecutionResult($"Failed to run the task: {ex.Message}");
        }
    }
}