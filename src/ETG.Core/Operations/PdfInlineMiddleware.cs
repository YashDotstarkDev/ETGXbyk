using CMS.ContentEngine;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace ETG.Web.Features.Brochures;

public sealed class PdfInlineMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var assetPath = $"/{ContentItemAssetConstants.CONTENT_ASSET_URL_PATH_PREFIX}/";


        if (path.Contains(assetPath, StringComparison.OrdinalIgnoreCase))
        {
            context.Response.OnStarting(() =>
            {
                var contentType = context.Response.ContentType;
                var disposition = context.Response.Headers.ContentDisposition.ToString();

                var isPdf = contentType?.StartsWith(
                    "application/pdf",
                    StringComparison.OrdinalIgnoreCase) == true;

                if (!isPdf &&
                    ContentDispositionHeaderValue.TryParse(
                        disposition, out var parsedDisposition))
                {
                    var fileName = parsedDisposition.FileNameStar
                        ?? parsedDisposition.FileName?.Trim('"');

                    isPdf = fileName?.EndsWith(
                        ".pdf",
                        StringComparison.OrdinalIgnoreCase) == true;
                }

                if (!isPdf)
                {
                    isPdf = path.EndsWith(
                        ".pdf",
                        StringComparison.OrdinalIgnoreCase);
                }

                if (isPdf)
                {
                    context.Response.Headers.ContentDisposition = "inline";
                }

                return Task.CompletedTask;
            });
        }

        await next(context);
    }
}
public sealed class PdfInlineStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(
        Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.UseMiddleware<PdfInlineMiddleware>();
            next(app);
        };
    }
}