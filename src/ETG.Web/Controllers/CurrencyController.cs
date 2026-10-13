
using ETG.Web.Services.Currency;
using Microsoft.AspNetCore.Mvc;

namespace ETG.Web.Controllers;
public class CurrencyController : Controller
{
    private readonly ICurrencyService _currencyService;

    public CurrencyController(ICurrencyService currencyService)
    {
        _currencyService = currencyService;
    }

    [HttpGet("currency/set/{code}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Set(string code, string? returnUrl)
    {
        if (!_currencyService.IsValidCurrencyCode(code))
        {
            return BadRequest();
        }

        _currencyService.SetCurrentCurrency(code);

        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
    }
}
