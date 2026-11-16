using ETG.Web.Features.Enquire;
using Kentico.Membership;
using Kentico.Web.Mvc;
using Kentico.Xperience.Zapier;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;

var builder = WebApplication.CreateBuilder(args);

var env = builder.Environment;
var config = builder.Configuration;

builder.Services
    .AddAppXperience(config, env)
    .AddAppMvc(env)
     .AddApp(config, env)
    .AddAuthentication();

// Multi-Factor Authentication (2FA / TOTP) & Security Options for Xperience administration
builder.Services.Configure<AdminIdentityOptions>(options =>
{
    options.AuthenticationOptions.MultiFactorAuthenticationOptions.Enabled = true;
    options.AuthenticationOptions.MultiFactorAuthenticationOptions.ApplicationName = "Entire Travel Group";
    options.EmailOptions.LinkExpiration = TimeSpan.FromHours(24);
});

builder.Services.AddKenticoZapier();

builder.Services
    .AddControllersWithViews();

builder.Services.Configure<BrotliCompressionProviderOptions>(o =>
    o.Level = CompressionLevel.Fastest);

builder.Services.Configure<GzipCompressionProviderOptions>(o =>
    o.Level = CompressionLevel.Fastest);

builder.Services.AddResponseCompression(o =>
{
    o.EnableForHttps = true;
    o.Providers.Add<BrotliCompressionProvider>();
    o.Providers.Add<GzipCompressionProvider>();
    o.MimeTypes = ResponseCompressionDefaults.MimeTypes
        .Concat(new[] { "image/svg+xml" })
        .Distinct();
});

builder.Services.AddContactsImport();

var app = builder.Build();

app
    .InitKentico()
    .UseForwardedHeaders()
    .IfDevelopment(env, b => b.UseDeveloperExceptionPage())
    .IfNotDevelopment(env, b => b.UseExceptionHandler("/error/500"))
    .UseResponseCompression()
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
    .UseContactsImport()
    .Use(async (context, next) =>
        {
            context.Response.Headers["X-Robots-Tag"] =
                "noindex, nofollow, noarchive";
            await next();
        });

// Apple Pay merchant domain association verification endpoint
app.MapGet("/.well-known/apple-developer-merchantid-domain-association", async (HttpContext context) =>
{
    var filePath = Path.Combine(app.Environment.WebRootPath, ".well-known", "apple-developer-merchantid-domain-association");
    if (File.Exists(filePath))
    {
        context.Response.ContentType = "text/plain";
        await context.Response.SendFileAsync(filePath);
    }
    else
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
    }
});
app.Run();
