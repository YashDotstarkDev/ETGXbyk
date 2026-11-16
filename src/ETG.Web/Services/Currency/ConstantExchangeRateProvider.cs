using CMS.Core;
using CMS.DataEngine;
using CMS.Scheduler;
using CMS.Websites.Routing;
using ETG.Core.Content;
using ETG.Core.Modules;
using ETG.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

[assembly: RegisterScheduledTask(identifier: "ETG.CurrencyRateUpdate", typeof(ETG.Web.Services.Currency.CurrencyRateScheduledTask))]

namespace ETG.Web.Services.Currency;

public class ConstantExchangeRateProvider(
    IHttpClientFactory httpClientFactory,
    IServiceScopeFactory scopeFactory,
    IInfoProvider<FexExchangeInfo> fexProvider,
    IMemoryCache memoryCache,
    IEventLogService eventLog,
    IHttpContextAccessor? httpContextAccessor = null) : IExchangeRateProvider
{
    private const string HttpClientName = "FixerClient";
    private const string CacheKeyPrefix = "ETG.Currency.Rates";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);
    private static CancellationTokenSource _cacheResetTokenSource = new();

    private string ResolveCurrentChannelName()
    {
        try
        {
            var httpContext = httpContextAccessor?.HttpContext;
            if (httpContext != null)
            {
                var channelContext = httpContext.RequestServices.GetService<IWebsiteChannelContext>();
                if (!string.IsNullOrWhiteSpace(channelContext?.WebsiteChannelName))
                {
                    return channelContext.WebsiteChannelName;
                }

                var host = httpContext.Request.Host.Host;
                if (!string.IsNullOrWhiteSpace(host))
                {
                    return host;
                }
            }
        }
        catch
        {
            // fallback if outside active request
        }

        return "default";
    }

    private static string GetCacheKey(string? channelName, string? baseCurrency) =>
        $"{CacheKeyPrefix}_{channelName ?? "default"}_{baseCurrency ?? CurrencyConstants.CODE_AUD}";

    private async Task<WebsiteGlobalContent?> GetGlobalContentAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var response = await mediator.Send(new WebsiteSettingsQuery(), cancellationToken);
            return response.GlobalContent;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            eventLog.LogException("CONVERSION", "GET_SETTINGS", ex);
            return null;
        }
    }

    // ---------- Reading (used on every page request) ----------

    public decimal GetRate(string currencyCode)
    {
        var channelName = ResolveCurrentChannelName();
        var cacheKey = GetCacheKey(channelName, CurrencyConstants.CODE_AUD);

        var rates = memoryCache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            entry.AddExpirationToken(new CancellationChangeToken(_cacheResetTokenSource.Token));
            return LoadRatesFromDatabase(CurrencyConstants.CODE_AUD);
        })!;

        return rates.TryGetValue(currencyCode, out var rate)
            ? rate
            : CurrencyConstants.GetFallbackRate(currencyCode);
    }

    public async Task<decimal> GetRateAsync(string currencyCode, CancellationToken cancellationToken = default)
    {
        var channelName = ResolveCurrentChannelName();
        var cacheKey = GetCacheKey(channelName, CurrencyConstants.CODE_AUD);

        if (!memoryCache.TryGetValue(cacheKey, out Dictionary<string, decimal>? rates) || rates == null)
        {
            rates = await LoadRatesFromDatabaseAsync(CurrencyConstants.CODE_AUD, cancellationToken);
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheDuration
            };
            cacheOptions.AddExpirationToken(new CancellationChangeToken(_cacheResetTokenSource.Token));
            memoryCache.Set(cacheKey, rates, cacheOptions);
        }

        return rates.TryGetValue(currencyCode, out var rate)
            ? rate
            : CurrencyConstants.GetFallbackRate(currencyCode);
    }

    private Dictionary<string, decimal> LoadRatesFromDatabase(string? baseCurrency = null)
    {
        try
        {
            var source = string.IsNullOrWhiteSpace(baseCurrency) ? CurrencyConstants.CODE_AUD : baseCurrency;

            return fexProvider.Get()
                .WhereEquals(nameof(FexExchangeInfo.SourceCurrency), source)
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
            return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private async Task<Dictionary<string, decimal>> LoadRatesFromDatabaseAsync(string? baseCurrency, CancellationToken cancellationToken)
    {
        try
        {
            var source = string.IsNullOrWhiteSpace(baseCurrency) ? CurrencyConstants.CODE_AUD : baseCurrency;

            var items = await fexProvider.Get()
                .WhereEquals(nameof(FexExchangeInfo.SourceCurrency), source)
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

            return items
                .Where(r => r.TargetCurrencyValue > 0)
                .ToDictionary(
                    r => r.TargetCurrency,
                    r => r.TargetCurrencyValue,
                    StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            eventLog.LogException("CONVERSION", "LOAD_ASYNC", ex);
            return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        }
    }

    // ---------- Writing (called only by the scheduled task) ----------

    public async Task<string> RefreshAsync(CancellationToken cancellationToken)
    {
        var gc = await GetGlobalContentAsync(cancellationToken);
        var apiKey = gc?.CurrencyFixerApiKey ?? string.Empty;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return "Fixer API key is not set; fallback rates remain in use.";
        }

        var baseCurrency = gc?.CurrencyBaseCurrency ?? string.Empty;
        var supportedRaw = gc?.CurrencySupported ?? string.Empty;
        var supported = supportedRaw
            .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        var client = httpClientFactory.CreateClient(HttpClientName);

        // SECURITY: API key sent via HTTPS (configured in ServiceCollectionCurrencyExtensions)
        var symbols = string.Join(',', supported.Where(c => c != "EUR"));
        var url = $"latest?access_key={apiKey}&symbols={symbols}";

        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync(url, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            // SECURITY: Log error without exposing API key
            eventLog.LogException("CONVERSION", "HTTPS_ERROR", ex, 
                $"Failed to connect to Fixer API. Verify HTTPS is supported by your Fixer plan.");
            throw new InvalidOperationException(
                "Failed to connect to Fixer API. Check if your plan supports HTTPS.", ex);
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var snippet = body.Length > 300 ? body[..300] : body;
            eventLog.LogError("CONVERSION", "API_ERROR", 
                $"Fixer API returned {(int)response.StatusCode}. Response: {snippet}");
            throw new InvalidOperationException($"Fixer returned {(int)response.StatusCode}: {snippet}");
        }

        var result = System.Text.Json.JsonSerializer.Deserialize<FixerRatesResponse>(body);

        if (result is null || !result.Success)
        {
            var errorType = result?.Error?.Type ?? "unknown";
            eventLog.LogError("CONVERSION", "API_ERROR", $"Fixer API error: {errorType}");
            throw new InvalidOperationException($"Fixer error: {errorType}");
        }

        if (!result.Rates.TryGetValue("AUD", out var eurToAud) || eurToAud == 0)
        {
            throw new InvalidOperationException("Fixer response did not include an AUD rate.");
        }

        var saved = new List<string>();

        // EUR -> X and EUR -> AUD from Fixer, so AUD -> X = (EUR -> X) / (EUR -> AUD)
        Save(baseCurrency, "EUR", 1m / eurToAud);
        saved.Add("EUR");

        foreach (var (code, eurToTarget) in result.Rates)
        {
            if (code == "AUD")
            {
                continue;
            }

            Save(baseCurrency, code, eurToTarget / eurToAud);
            saved.Add(code);
        }

        // Invalidate all scoped cache entries across all channels atomically
        var oldToken = Interlocked.Exchange(ref _cacheResetTokenSource, new CancellationTokenSource());
        oldToken.Cancel();
        oldToken.Dispose();

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