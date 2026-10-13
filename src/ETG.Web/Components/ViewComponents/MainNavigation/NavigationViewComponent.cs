
using ETG.Web.Components.ViewComponents.MainNavigation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

public class NavigationViewComponent : ViewComponent
{
    private readonly IMediator mediator;

    public NavigationViewComponent(IMediator mediator)
    {
        this.mediator = mediator;
    }

    public async Task<IViewComponentResult> InvokeAsync(int maxDepth = 10)
    {
        try
        {
            var query = new GetNavigationHierarchyQuery("/Shared/Menus/Main-Menu", maxDepth);
            var result = await mediator.Send(query);

            if (result.HasValue)
            {
                return View("~/Components/ViewComponents/MainNavigation/Navigation.cshtml", result.Value);
            }

            return View("~/Components/ViewComponents/MainNavigation/Navigation.cshtml", new NavigationViewModel());
        }
        catch (Exception ex)
        {
            return View("~/Components/ViewComponents/MainNavigation/Navigation.cshtml", new NavigationViewModel());
        }
    }
}