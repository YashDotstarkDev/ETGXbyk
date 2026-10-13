using ETG.Web.Services.Currency;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ETG.Web.Configuration;

public static class ServiceCollectionCurrencyExtensions
{
    public static IServiceCollection AddCurrency(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<CurrencyOptions>(config.GetSection("Currency"));
        services.AddHttpContextAccessor();

        services.AddHttpClient("FixerClient", client =>
        {
            client.BaseAddress = new Uri("http://data.fixer.io/api/");
        });

        services.AddSingleton<ConstantExchangeRateProvider>();
        services.AddSingleton<IExchangeRateProvider>(sp => sp.GetRequiredService<ConstantExchangeRateProvider>());
        services.AddHostedService<CurrencyRateRefreshService>();

        services.AddScoped<ICurrencyService, CurrencyService>();
        services.AddScoped(sp =>
        {
            var currencyService = sp.GetRequiredService<ICurrencyService>();
            var code = currencyService.GetCurrentCurrency();
            return new CurrentCurrencyPricing(code, currencyService.GetConversionRate(code));
        });

        return services;
    }

    private class CurrencyRateRefreshService(
        IServiceScopeFactory scopeFactory,
        ILogger<CurrencyRateRefreshService> logger) : BackgroundService
    {
        private static readonly TimeSpan RefreshInterval = TimeSpan.FromHours(6);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = scopeFactory.CreateScope();
                var provider = scope.ServiceProvider.GetRequiredService<ConstantExchangeRateProvider>();
                await provider.RefreshAsync(stoppingToken);

                try
                {
                    await Task.Delay(RefreshInterval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // app is shutting down
                }
            }
        }
    }
}