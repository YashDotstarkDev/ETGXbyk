using ETG.Web.Components.ViewComponents.MainNavigation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

public class TopNavigationViewComponent : ViewComponent
{
    private readonly IMediator mediator;
    public TopNavigationViewComponent(IMediator mediator) => this.mediator = mediator;

    public async Task<IViewComponentResult> InvokeAsync(int maxDepth = 3)
    {
        try
        {
            var query = new GetNavigationHierarchyQuery("/Shared/Menus/Top-Menu", maxDepth);
            var result = await mediator.Send(query);
            var model = result.HasValue ? result.Value : new NavigationViewModel();
            return View("~/Components/ViewComponents/TopNavigation/TopNavigation.cshtml", model);
        }
        catch (Exception ex)
        {
            return View("~/Components/ViewComponents/TopNavigation/TopNavigation.cshtml", new NavigationViewModel());
        }
    }
}