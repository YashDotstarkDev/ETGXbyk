using System.Text.Json;
using UtmModel = ETG.Core.Models.Utm;

namespace ETG.Web.Services.Utm;

public sealed class UtmService : IUtmService
{
    private const string CookieName = "etgUtm";
    private const int CookieExpirationDays = 100;

    private readonly IHttpContextAccessor _httpContextAccessor;

    public UtmService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public UtmModel? GetUtmCookie()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext == null)
        {
            return null;
        }

        if (!httpContext.Request.Cookies.TryGetValue(
                CookieName,
                out var cookieValue))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(cookieValue))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<UtmModel>(
                cookieValue,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public void SetUtmCookie(UtmModel utm)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext == null)
        {
            return;
        }

        var cookieValue = JsonSerializer.Serialize(utm);

        httpContext.Response.Cookies.Append(
            CookieName,
            cookieValue,
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(
                    CookieExpirationDays),

                HttpOnly = false,

                Secure = httpContext.Request.IsHttps,

                SameSite = SameSiteMode.Lax,

                IsEssential = true,

                Path = "/"
            });
    }

    public bool HasUtmCookie()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        return httpContext?.Request.Cookies.ContainsKey(
            CookieName) == true;
    }
}
