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
        services.AddMemoryCache();

        services.AddHttpClient("FixerClient", client =>
        {
            client.BaseAddress = new Uri("http://data.fixer.io/api/"); // http only on free/basic Fixer plans
        });

        services.AddSingleton<ConstantExchangeRateProvider>();
        services.AddSingleton<IExchangeRateProvider>(sp => sp.GetRequiredService<ConstantExchangeRateProvider>());

        services.AddScoped<ICurrencyService, CurrencyService>();
        services.AddScoped(sp =>
        {
            var currencyService = sp.GetRequiredService<ICurrencyService>();
            var code = currencyService.GetCurrentCurrency();
            return new CurrentCurrencyPricing(code, currencyService.GetConversionRate(code));
        });

        return services;
    }
}