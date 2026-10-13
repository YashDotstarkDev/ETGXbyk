// Components/ViewComponents/CurrencyDropdown/CurrencyDropdownViewComponent.cs
using ETG.Web.Services.Currency;
using Microsoft.AspNetCore.Mvc;

namespace ETG.Web.Components.ViewComponents.CurrencyDropdown;

public class CurrencyDropdownViewComponent(
    ICurrencyService currencyService,
    CurrentCurrencyPricing pricing) : ViewComponent
{
    private readonly ICurrencyService currencyService = currencyService;
    private readonly CurrentCurrencyPricing pricing = pricing;

    public IViewComponentResult Invoke()
    {
        try
        {
            var model = new CurrencyDropdownViewModel
            {
                Currencies = currencyService.GetSupportedCurrencies()
                .OrderBy(c => c)
                .ToList(),
                CurrentCurrency = pricing.CurrentCurrency,
                CurrentCurrencySymbol = pricing.CurrentCurrencySymbol,
                ConversionRate = pricing.ConversionRate,
                CurrencyApplyDiscounts = pricing.CurrencyAppliesDiscounts
            };

            return View("~/Components/ViewComponents/CurrencyDropdown/CurrencyDropdown.cshtml", model);
        }
        catch (Exception)
        {
            return View("~/Components/ViewComponents/CurrencyDropdown/CurrencyDropdown.cshtml", new CurrencyDropdownViewModel());
        }
    }
}