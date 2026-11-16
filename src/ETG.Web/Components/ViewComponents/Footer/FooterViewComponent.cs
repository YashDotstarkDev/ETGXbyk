using ETG.Web.Components.ViewComponents.Shared;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ETG.Web.Components.ViewComponents.Footer;

public class FooterViewComponent(IMediator mediator, IWebsiteLayoutAccessor? layoutAccessor = null) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        try
        {
            var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;
            var result = await mediator.Send(new GetFooterQuery(), cancellationToken);

            var vm = result.GetValueOrDefault(new FooterViewModel());
            if (layoutAccessor != null)
            {
                var layout = await layoutAccessor.GetAsync(cancellationToken);
                vm = vm.WithLayout(layout ?? new WebsiteLayoutContentViewModel());
            }

            return View("~/Components/ViewComponents/Footer/Footer.cshtml", vm);
        }
        catch
        {
            return View("~/Components/ViewComponents/Footer/Footer.cshtml", new FooterViewModel());
        }
    }
}
