using Microsoft.AspNetCore.Mvc;

namespace ETG.Web.Components.ViewComponents.BreadCrumb;

public class BreadCrumbViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        var currentPath = HttpContext.Request.Path.Value ?? "/";
        var currentPageTitle = ViewContext.ViewData["Title"]?.ToString();

     
        var modelType = ViewContext.ViewData.Model?.GetType().ToString() ?? "";

        bool isArticleLanding = modelType.Contains("ArticleLanding");
        bool isArticlePost = modelType.Contains("ArticlePost") || (modelType.Contains("Article") && !isArticleLanding);

        var viewModel = GetBreadCrumbQuery.Get(currentPath, currentPageTitle, isArticleLanding, isArticlePost);

        return View(
            "~/Components/ViewComponents/BreadCrumb/BreadCrumb.cshtml",
            viewModel);
    }
}
