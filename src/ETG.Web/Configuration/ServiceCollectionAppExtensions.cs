using ETG.Admin.Features.ETGBooking.ImportBookingPrices;
using ETG.Core;
using ETG.Core.Operations;
using ETG.Web.Components.PageBuilder.Widgets.Heading;
using ETG.Web.Configuration;
using ETG.Web.Features.Brochures;
using ETG.Web.Features.Enquire;
using ETG.Web.Features.SEO;
using ETG.Web.Infrastructure;
using ETG.Web.Rendering;
using ETG.Web.Services.Tour;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Sidio.Sitemap.AspNetCore;
using Sidio.Sitemap.Core.Services;
using Slugify;
using ETG.Web.Services.Storage;
using ETG.Web.Services.Utm;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionAppExtensions
{
    public static IServiceCollection AddApp(this IServiceCollection services, IConfiguration config, IWebHostEnvironment env) =>
    services
            .AddSEO()
            .AddRendering()
            .AddOperations(config)
            .AddInfrastructure(config)
            .AddTransient<IStartupFilter, PdfInlineStartupFilter>()
            .AddTransient<IStartupFilter, EnquireUrlRewriteStartupFilter>()
            .AddGoogleReviews()
            .AddCurrency(config)
            .AddScoped<IShareLinksService, ShareLinksService>()
            .AddScoped<ITourSummaryService, TourSummaryService>()
            .AddScoped<ITourPricingService, TourPricingService>()
            .AddScoped<IUtmService, UtmService>()
            .AddScoped<IBlobStorageService, AzureBlobStorageService>()
            .AddAzureBlobStorage(config, env);

    private static IServiceCollection AddOperations(this IServiceCollection services, IConfiguration config) =>
    services
            .AddSingleton<ICacheDependencyKeysBuilder, CacheDependencyKeysBuilder>()
            .Configure<DefaultQueryCacheSettings>(settings =>
            {
                var section = config.GetSection("Cache:Query");

                settings.CacheItemDuration = TimeSpan.FromMinutes(section.GetValue<int>("CacheItemDuration"));
                settings.IsEnabled = section.GetValue<bool>("IsEnabled");
                settings.IsSlidingExpiration = section.GetValue<bool>("IsSlidingExpiration");
            })
            .AddMediatR(cfg => cfg
                .RegisterServicesFromAssembly(typeof(ServiceCollectionAppExtensions).Assembly)
                .AddOpenBehavior(typeof(CommandHandlerLogDecorator<,>)))
            .AddTransient<WebPageCommandTools>()
            .AddTransient<IContentQueryExecutionOptionsCreator, DefaultContentQueryExecutionOptionsCreator>()
            .AddTransient<WebPageQueryTools>()
            .AddTransient<ContentItemQueryTools>()
            .AddTransient<DataItemCommandTools>()
            .AddTransient<DataItemQueryTools>()
            .AddScoped<BookingPriceImportService>();

    private static IServiceCollection AddRendering(this IServiceCollection services) =>
        services
            .AddSingleton<ISlugHelper>(_ => new SlugHelper(new SlugHelperConfiguration()))
            .AddScoped<IHeadingContext, HeadingContext>()
            .AddScoped<ViewService>()
            .AddScoped<IPageBuilderContext, PageBuilderContext>();

    private static IServiceCollection AddSEO(this IServiceCollection services) =>
        services
            .AddScoped<WebPageMetaService>()
            .AddTransient<SitemapRetriever>()
            .AddDefaultSitemapServices<HttpContextBaseUrlProvider>();

    private static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config) =>
        services
            .AddScoped<CaptchaValidator>()
            .Configure<ReCaptchaSettings>(config.GetSection("ReCaptcha"));

}