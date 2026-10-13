
namespace ETG.Web.Components.ViewComponents.MainNavigation;

public class NavigationViewModel
{
    public List<NavigationItemViewModel> RootItems { get; set; } = new();
    public ETG.Web.Components.ViewComponents.Shared.WebsiteLayoutContentViewModel? Layout { get; set; }

    public List<NavigationItemViewModel> GetAllItemsFlattened()
    {
        var result = new List<NavigationItemViewModel>();

        foreach (var item in RootItems)
        {
            result.Add(item);
            AddChildrenRecursively(item, result);
        }

        return result;
    }

    private static void AddChildrenRecursively(NavigationItemViewModel parent, List<NavigationItemViewModel> result)
    {
        foreach (var child in parent.Children)
        {
            result.Add(child);
            AddChildrenRecursively(child, result);
        }
    }
}