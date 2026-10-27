using ETG.Web.Features.Enquire;
using Kentico.Web.Mvc;

var builder = WebApplication.CreateBuilder(args);

var env = builder.Environment;
var config = builder.Configuration;

builder.Services
    .AddAppXperience(config, env)
    .AddAppMvc(env)
     .AddApp(config, env)
    .AddAuthentication();
builder.Services
    .AddControllersWithViews();

var app = builder.Build();

app
    .InitKentico()
    .UseForwardedHeaders()
    .IfDevelopment(env, b => b.UseDeveloperExceptionPage())
    .IfNotDevelopment(env, b => b.UseExceptionHandler("/error/500"))
    .UseStaticFiles()
    .UseCors()
    .UseCookiePolicy()
    .UseAuthentication()
    .UseKentico()
    //.UseMiddleware<EnquireUrlRewriteMiddleware>()
    .UseRateLimiter()
    .UseRequestLocalization()
    .UseStatusCodePagesWithReExecute("/error/{0}")
    .IfNotDevelopment(env, b => b.UseHsts())
    .UseAuthorization()
    .UseKenticoRoutes(app)
    .UseAppRoutes()
    .Use(async (context, next) =>
        {
            context.Response.Headers["X-Robots-Tag"] =
                "noindex, nofollow, noarchive";
            await next();
        });

app.Run();