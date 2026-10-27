using UtmModel = ETG.Core.Models.Utm;

namespace ETG.Web.Services.Utm;

public interface IUtmService
{
    UtmModel? GetUtmCookie();

    void SetUtmCookie(UtmModel utm);

    bool HasUtmCookie();
}