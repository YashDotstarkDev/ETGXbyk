using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ETG.Web.Components.ViewComponents.Footer;

public class FooterViewComponent(IMediator mediator) : ViewComponent
{
    private readonly IMediator mediator = mediator;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        try
        {
            var result = await mediator.Send(new GetFooterQuery());

            var vm = result.GetValueOrDefault(new FooterViewModel());

            return View("~/Components/ViewComponents/Footer/Footer.cshtml", vm);
    }
        catch
        {
            return View("~/Components/ViewComponents/Footer/Footer.cshtml", new FooterViewModel());
        }
    }
}
