
using ETG.Web.Components.ViewComponents.MainNavigation;
using ETG.Web.Components.ViewComponents.Shared;
using MediatR;
using Microsoft.AspNetCore.Mvc;

public class NavigationViewComponent(IMediator mediator, IWebsiteLayoutAccessor? layoutAccessor = null) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(int maxDepth = 10)
    {
        try
        {
            var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;
            var query = new GetNavigationHierarchyQuery("/Shared/Menus/Main-Menu", maxDepth);
            var result = await mediator.Send(query, cancellationToken);

            if (result.HasValue)
            {
                var model = result.Value;
                if (layoutAccessor != null)
                {
                    var layout = await layoutAccessor.GetAsync(cancellationToken);
                    model = model.WithLayout(layout ?? new WebsiteLayoutContentViewModel());
                }

                return View("~/Components/ViewComponents/MainNavigation/Navigation.cshtml", model);
            }

            return View("~/Components/ViewComponents/MainNavigation/Navigation.cshtml", new NavigationViewModel());
        }
        catch (Exception)
        {
            return View("~/Components/ViewComponents/MainNavigation/Navigation.cshtml", new NavigationViewModel());
        }
    }
}