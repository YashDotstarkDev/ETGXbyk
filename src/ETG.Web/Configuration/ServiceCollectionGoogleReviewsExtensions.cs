using ETG.Web.Services.GoogleReviews;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionGoogleReviewsExtensions
{
    public static IServiceCollection AddGoogleReviews(this IServiceCollection services)
    
    {
        services.AddHttpClient("Wextractor", client =>
        {
            client.BaseAddress = new Uri("https://wextractor.com/api/v1/");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        services.AddScoped<IGoogleReviewsService, GoogleReviewsService>();

        return services;
    }
}
