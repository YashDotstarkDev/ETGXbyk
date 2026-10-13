namespace ETG.Web.Components.ViewComponents.BreadCrumb;

public class BreadCrumbViewModel
{
    public List<BreadCrumbItem> Items { get; set; } = new();

    public class BreadCrumbItem
    {
        public string Label { get; set; } = "";
        public string Url { get; set; } = "";
        public bool IsCurrentPage { get; set; }
    }
}