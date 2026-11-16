using ETG.Web.Components.ViewComponents.MainNavigation;
using ETG.Web.Components.ViewComponents.Shared;
using MediatR;
using Microsoft.AspNetCore.Mvc;

public class TopNavigationViewComponent(IMediator mediator, IWebsiteLayoutAccessor? layoutAccessor = null) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(int maxDepth = 3)
    {
        try
        {
            var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;
            var query = new GetNavigationHierarchyQuery("/Shared/Menus/Top-Menu", maxDepth);
            var result = await mediator.Send(query, cancellationToken);

            var model = result.HasValue ? result.Value : new NavigationViewModel();
            if (layoutAccessor != null)
            {
                var layout = await layoutAccessor.GetAsync(cancellationToken);
                model = model.WithLayout(layout ?? new WebsiteLayoutContentViewModel());
            }

            return View("~/Components/ViewComponents/TopNavigation/TopNavigation.cshtml", model);
        }
        catch (Exception)
        {
            return View("~/Components/ViewComponents/TopNavigation/TopNavigation.cshtml", new NavigationViewModel());
        }
    }
}