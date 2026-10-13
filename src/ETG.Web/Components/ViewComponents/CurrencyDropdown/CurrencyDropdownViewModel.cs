namespace ETG.Web.Components.ViewComponents.CurrencyDropdown;


public class CurrencyDropdownViewModel
{
    public List<string> Currencies { get; set; } = new();
    public string CurrentCurrency { get; set; } = string.Empty;
    public string CurrentCurrencySymbol { get; set; } = string.Empty;
    public decimal ConversionRate { get; set; }
    public bool CurrencyApplyDiscounts { get; set; }
}