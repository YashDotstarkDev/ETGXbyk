using ETG.Admin.Features.ETGBooking.ImportBookingPrices;
using ETG.Admin.Services;
using ETG.Core;
using ETG.Core.Operations;
using ETG.Web;
using ETG.Web.Components.PageBuilder.Widgets.Heading;
using ETG.Web.Components.ViewComponents.Shared;
using ETG.Web.Configuration;
using ETG.Web.Features.Brochures;
using ETG.Web.Features.Enquire;
using ETG.Web.Features.Errors;
using ETG.Web.Features.SEO;
using ETG.Web.Infrastructure;
using ETG.Web.Rendering;
using ETG.Web.Services.Email;
using ETG.Web.Services.Storage;
using ETG.Web.Services;
using ETG.Web.Services.Tour;
using ETG.Web.Services.Unsubscribe;
using ETG.Web.Services.Utm;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Sidio.Sitemap.AspNetCore;
using Sidio.Sitemap.Core.Services;
using Slugify;
using CMS.EmailEngine;
using ETG.Web.Features.TourPackage;

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
        .AddAzureBlobStorage(config, env)
        .AddScoped<IFormEmailService, FormEmailService>()
        .AddScoped<IFormAutoresponderConfigurationService, FormAutoresponderConfigurationService>()
        .AddScoped<ContactTagService>()
        .AddScoped<ETG.Web.Services.Payment.ITravelPayService, ETG.Web.Services.Payment.TravelPayService>()
        .AddScoped<UnsubscribeRedirectService>()
        .AddScoped<TourVanityUrlBackfillRunner>()
        .AddSingleton<TourVanityUrlService>()
        .AddScoped<TourVanityUrlBackfillRunner>()
        .AddScoped<IWebsiteLayoutAccessor, WebsiteLayoutAccessor>()
        .AddMemoryCache()
        .AddTransient<UnpublishedTourRedirectMiddleware>()
        .AddTransient<IStartupFilter, UnpublishedTourRedirectStartupFilter>()
        .Configure<EmailQueueOptions>(options =>{options.ArchiveDuration = 30; options.BatchSize = 100;options.LoadInterval = 60000;});

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
                .AddOpenBehavior(typeof(CommandHandlerLogDecorator<,>))
                .AddOpenBehavior(typeof(QueryCachingPipelineBehavior<,>)))
            .AddTransient<WebPageCommandTools>()
            .AddTransient<IContentQueryExecutionOptionsCreator, DefaultContentQueryExecutionOptionsCreator>()
            .AddTransient<WebPageQueryTools>()
            .AddTransient<ContentItemQueryTools>()
            .AddTransient<DataItemCommandTools>()
            .AddTransient<DataItemQueryTools>()
            .AddTransient<IQueryCacheKeysCreator<PageNotFoundQuery, PageNotFoundViewModel>, PageNotFoundQueryHandler>()
            .AddTransient<IQueryCacheSettingsCustomizer<PageNotFoundQuery, PageNotFoundViewModel>, PageNotFoundQueryHandler>()
            .AddScoped<BookingPriceImportService>()
        .AddUnsubscribe(config);

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

    private static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var imgixSection = config.GetSection(ImgixOptions.SectionName);
        services.Configure<ImgixOptions>(imgixSection);

        var imgixOptions = imgixSection.Get<ImgixOptions>() ?? new ImgixOptions();
        ImgixExtensions.Initialize(imgixOptions);

        services.AddHttpClient(CaptchaValidator.HTTP_CLIENT_NAME, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        return services
            .AddScoped<CaptchaValidator>()
            .AddScoped<IContainerExpiryService, ContainerExpiryService>()
            .Configure<ReCaptchaSettings>(config.GetSection("ReCaptcha"));
    }

    private static IServiceCollection AddUnsubscribe(
this IServiceCollection services,
IConfiguration config)
    {
        //services
        //    .AddOptions<UnsubscribeOptions>()
        //    .Bind(config.GetSection(UnsubscribeOptions.SectionName))
        //    .ValidateDataAnnotations()
        //    .Validate(
        //        options =>
        //            options.CaptchaAllowedHostnames is { Length: > 0 }
        //            && options.CaptchaAllowedHostnames.All(
        //                hostname => !string.IsNullOrWhiteSpace(hostname)),
        //        "Configure allowed CAPTCHA hostnames.")
        //    .ValidateOnStart();

        services.AddScoped<
            IUnsubscribeReasonService,
            UnsubscribeReasonService>();

        services.AddScoped<
            IContactUnsubscribeService,
            ContactUnsubscribeService>();

        services.AddScoped<UnsubscribeLinkService>();

        return services;
    }

}