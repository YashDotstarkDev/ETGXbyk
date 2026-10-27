using System;
using System.Linq;

namespace ETG.Web.Components.ViewComponents.BreadCrumb;

public static class GetBreadCrumbQuery
{
    public static BreadCrumbViewModel Get(string currentPath, string currentPageTitle = null, bool isArticleLanding = false, bool isArticlePost = false, string articleLandingTitle = null)
    {
        var viewModel = new BreadCrumbViewModel();

        if (string.IsNullOrWhiteSpace(currentPath) || currentPath == "/")
        {
            return viewModel;
        }

       
        // ---------------------------------------------------------

        if (isArticleLanding)
        {
            viewModel.Items.Add(new BreadCrumbViewModel.BreadCrumbItem
            {
               
                Label = !string.IsNullOrEmpty(currentPageTitle) ? currentPageTitle : "The Explorer's Log",
                Url = "",
                IsCurrentPage = true
            });
            return viewModel;
        }

        if (isArticlePost)
        {
            var landingLabel = !string.IsNullOrEmpty(articleLandingTitle) ? articleLandingTitle : "The Explorer's Log";
            viewModel.Items.Add(new BreadCrumbViewModel.BreadCrumbItem
            {
                Label = landingLabel,
                Url = "/articles",
                IsCurrentPage = false
            });

            
            viewModel.Items.Add(new BreadCrumbViewModel.BreadCrumbItem
            {
            
                Label = !string.IsNullOrEmpty(currentPageTitle) ? currentPageTitle : "Article",
                Url = "",
                IsCurrentPage = true
            });

            return viewModel;
        }

        // ---------------------------------------------------------


        var routeParts = currentPath
            .Trim('/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        var url = "";

        for (var i = 0; i < routeParts.Length; i++)
        {
            var routePart = routeParts[i];
            var isCurrentPage = i == routeParts.Length - 1;

            url += "/" + routePart;
            var label = GetLabel(routePart);

            viewModel.Items.Add(new BreadCrumbViewModel.BreadCrumbItem
            {
                Label = label,
                Url = isCurrentPage ? "" : url,
                IsCurrentPage = isCurrentPage
            });
        }

        return viewModel;
    }

    private static string GetLabel(string routePart)
    {
        return string.Join(
            " ",
            routePart
                .Replace("-", " ")
                .Replace("_", " ")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(word =>
                    char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant())
        );
    }
}
