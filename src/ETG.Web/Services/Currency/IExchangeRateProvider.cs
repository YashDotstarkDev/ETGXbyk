namespace ETG.Web.Services.Currency;

public interface IExchangeRateProvider
{
    /// <summary>Units of the given currency per 1 AUD.</summary>
    decimal GetRate(string currencyCode);

    /// <summary>Units of the given currency per 1 AUD.</summary>
    Task<decimal> GetRateAsync(string currencyCode, CancellationToken cancellationToken = default);
}
