
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

    [HttpGet("change-currency")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult ChangeCurrency([FromQuery(Name = "currency")] string? currency, [FromQuery(Name = "code")] string? code, [FromQuery] string? returnUrl)
    {
        var targetCurrency = !string.IsNullOrWhiteSpace(currency) ? currency : code;
        return Set(targetCurrency ?? string.Empty, returnUrl);
    }

    [HttpGet("currency/set/{code}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Set(string code, string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(code) || !_currencyService.IsValidCurrencyCode(code))
        {
            return BadRequest();
        }

        _currencyService.SetCurrentCurrency(code);

        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
    }
}
