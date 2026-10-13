namespace ETG.Web.Components.ViewComponents.MainNavigation;

/// <summary>
/// Represents a navigation item with support for unlimited nesting levels.
/// This ViewModel is used to build a hierarchical menu structure.
/// </summary>
public class NavigationItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? IconClass { get; set; }
    public string? SvgIcon { get; set; }
    public string? LinkTo { get; set; }
    public string Target { get; set; } = "_self";
    public bool HideOnMobile { get; set; }
    public bool HideOnDesktop { get; set; }
    public List<NavigationItemViewModel> Children { get; set; } = new();
    public int Level { get; set; }


    public string? ViewAllCaption { get; set; }
    public string? ViewAllUrl { get; set; }

    /// <summary>
    /// Indicates whether this item has child items.
    /// </summary>
    public bool HasChildren => Children.Count > 0;

    /// <summary>
    /// Indicates whether this item should be visible on the current device type.
    /// </summary>
    public bool IsVisible(bool isMobile) => isMobile ? !HideOnMobile : !HideOnDesktop;
}
