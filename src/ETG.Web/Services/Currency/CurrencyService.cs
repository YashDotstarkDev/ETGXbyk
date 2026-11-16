using System;
using System.Collections.Generic;
using System.Linq;
using CMS.Websites.Routing;
using ETG.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace ETG.Web.Services.Currency;

public class CurrencyService : ICurrencyService
{
    private const string CURRENCY_COOKIE = "CurrentCurrency";
    private static readonly TimeSpan SupportedCurrenciesCacheDuration = TimeSpan.FromMinutes(30);

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IExchangeRateProvider _rates;
    private readonly IMemoryCache _memoryCache;
    private readonly IMediator? _mediator;

    public CurrencyService(
        IHttpContextAccessor httpContextAccessor,
        IExchangeRateProvider rates,
        IMemoryCache memoryCache,
        IMediator? mediator = null)
    {
        _httpContextAccessor = httpContextAccessor;
        _rates = rates;
        _memoryCache = memoryCache;
        _mediator = mediator;
    }

    public CurrencyService(IHttpContextAccessor httpContextAccessor, IExchangeRateProvider rates)
        : this(httpContextAccessor, rates, new MemoryCache(new MemoryCacheOptions()), null)
    {
    }

    public CurrencyService(
        IHttpContextAccessor httpContextAccessor,
        IExchangeRateProvider rates,
        IMediator? mediator)
        : this(httpContextAccessor, rates, new MemoryCache(new MemoryCacheOptions()), mediator)
    {
    }

    private string GetSupportedCurrenciesCacheKey()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var channelName = httpContext?.RequestServices.GetService<IWebsiteChannelContext>()?.WebsiteChannelName
                       ?? httpContext?.Request.Host.Host
                       ?? "default";

        return $"ETG.Currency.SupportedCurrencies_{channelName}";
    }

    public IReadOnlyList<string> GetSupportedCurrencies()
    {
        var cacheKey = GetSupportedCurrenciesCacheKey();
        if (_memoryCache.TryGetValue(cacheKey, out IReadOnlyList<string>? cached) && cached != null)
        {
            return cached;
        }

        // On synchronous cache miss, return safe fallback constants immediately without blocking the thread,
        // and trigger background population into memory cache for subsequent calls.
        _ = Task.Run(async () =>
        {
            try
            {
                await GetSupportedCurrenciesAsync(CancellationToken.None);
            }
            catch
            {
                // Non-critical background population failure
            }
        });

        return CurrencyConstants.SupportedCurrencies;
    }

    public async Task<IReadOnlyList<string>> GetSupportedCurrenciesAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = GetSupportedCurrenciesCacheKey();
        if (_memoryCache.TryGetValue(cacheKey, out IReadOnlyList<string>? cached) && cached != null)
        {
            return cached;
        }

        var loaded = await LoadSupportedCurrenciesAsync(cancellationToken);
        _memoryCache.Set(cacheKey, loaded, SupportedCurrenciesCacheDuration);
        return loaded;
    }

    private async Task<IReadOnlyList<string>> LoadSupportedCurrenciesAsync(CancellationToken cancellationToken)
    {
        if (_mediator != null)
        {
            try
            {
                var response = await _mediator.Send(new WebsiteSettingsQuery(), cancellationToken);
                var supported = response?.GlobalContent?.CurrencySupported;
                if (!string.IsNullOrWhiteSpace(supported))
                {
                    var list = supported
                        .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .ToList();
                    if (list.Count > 0)
                    {
                        return list;
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // fallback to constants if query fails
            }
        }

        return CurrencyConstants.SupportedCurrencies;
    }

    public bool IsValidCurrencyCode(string? currencyCode) =>
        !string.IsNullOrWhiteSpace(currencyCode) &&
        GetSupportedCurrencies().Contains(currencyCode, StringComparer.OrdinalIgnoreCase);

    public async Task<bool> IsValidCurrencyCodeAsync(string? currencyCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            return false;
        }

        var supported = await GetSupportedCurrenciesAsync(cancellationToken);
        return supported.Contains(currencyCode, StringComparer.OrdinalIgnoreCase);
    }

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
                IsEssential = true
            });
    }

    public decimal GetConversionRate(string currencyCode) =>
        string.Equals(currencyCode, CurrencyConstants.CODE_AUD, StringComparison.OrdinalIgnoreCase)
            ? 1m
            : _rates.GetRate(currencyCode.ToUpperInvariant());

    public async Task<decimal> GetConversionRateAsync(string currencyCode, CancellationToken cancellationToken = default) =>
        string.Equals(currencyCode, CurrencyConstants.CODE_AUD, StringComparison.OrdinalIgnoreCase)
            ? 1m
            : await _rates.GetRateAsync(currencyCode.ToUpperInvariant(), cancellationToken);
}