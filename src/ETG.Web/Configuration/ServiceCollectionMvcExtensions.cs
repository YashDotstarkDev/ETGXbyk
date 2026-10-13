using ETG.Web.Features.Errors;
using ETG.Web.Resources;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionMvcExtensions
{
    public static IServiceCollection AddAppMvc(this IServiceCollection services, IWebHostEnvironment env) =>
        services
            .Configure<RouteOptions>(options =>
            {
                options.LowercaseUrls = true;
                options.AppendTrailingSlash = false;
                options.LowercaseQueryStrings = false;
            })
            .AddLocalization()
            .Configure<RequestLocalizationOptions>(o =>
            {
                o.ApplyCurrentCultureToResponseHeaders = true;
                o.FallBackToParentUICultures = true;
                o.SupportedUICultures ??= [];
            })
            .Configure<StaticFileOptions>(o =>
            {
                o.OnPrepareResponse = context =>
                {
                    // Caches static files for 7 days
                    context.Context.Response.Headers.Append("Cache-Control", "public,max-age=604800");
                };
            })
            .AddControllersWithViews(config =>
            {
                if (!env.IsDevelopment())
                {
                    _ = config.Filters.Add<GlobalExceptionFilter>();
                }
            })
            .AddViewLocalization()
            .AddDataAnnotationsLocalization(options =>
            {
                options.DataAnnotationLocalizerProvider =
                    (type, factory) => factory.Create(typeof(SharedResources));
            })
            .Services
            .AddHttpContextAccessor()
            .AddHttpClient()
            .AddHealthChecks()
            .Services;
}
