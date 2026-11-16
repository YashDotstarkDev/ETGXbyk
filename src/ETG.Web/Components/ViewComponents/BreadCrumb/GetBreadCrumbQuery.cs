using System;
using System.Linq;

namespace ETG.Web.Components.ViewComponents.BreadCrumb;

public static class GetBreadCrumbQuery
{
    public static BreadCrumbViewModel Get(
        string currentPath,
        string? currentPageTitle = null,
        bool isArticleLanding = false,
        bool isArticlePost = false,
        string? articleLandingTitle = null,
        string? tourTitle = null,
        BreadCrumbViewModel.BreadCrumbItem? tourParent = null)
    {
        var viewModel = new BreadCrumbViewModel();

        if (string.IsNullOrWhiteSpace(currentPath)
            || currentPath == "/")
        {
            return viewModel;
        }

        // Added tour-only branch.
        // Home is already rendered by the existing Razor view.
        if (tourTitle is not null)
        {
            if (tourParent is not null)
            {
                viewModel.Items.Add(
                    new BreadCrumbViewModel.BreadCrumbItem
                    {
                        Label = tourParent.Label,
                        Url = tourParent.Url,
                        IsCurrentPage = false
                    });
            }

            viewModel.Items.Add(
                new BreadCrumbViewModel.BreadCrumbItem
                {
                    Label = tourTitle,
                    Url = "",
                    IsCurrentPage = true
                });

            return viewModel;
        }

        // Existing article landing behavior.
        if (isArticleLanding)
        {
            viewModel.Items.Add(
                new BreadCrumbViewModel.BreadCrumbItem
                {
                    Label = !string.IsNullOrEmpty(currentPageTitle)
                        ? currentPageTitle
                        : "The Explorer's Log",
                    Url = "",
                    IsCurrentPage = true
                });

            return viewModel;
        }

        // Existing article post behavior.
        if (isArticlePost)
        {
            var landingLabel =
                !string.IsNullOrEmpty(articleLandingTitle)
                    ? articleLandingTitle
                    : "The Explorer's Log";

            viewModel.Items.Add(
                new BreadCrumbViewModel.BreadCrumbItem
                {
                    Label = landingLabel,
                    Url = "/articles",
                    IsCurrentPage = false
                });

            viewModel.Items.Add(
                new BreadCrumbViewModel.BreadCrumbItem
                {
                    Label = !string.IsNullOrEmpty(currentPageTitle)
                        ? currentPageTitle
                        : "Article",
                    Url = "",
                    IsCurrentPage = true
                });

            return viewModel;
        }

        // Existing normal-page behavior.
        var routeParts = currentPath
            .Trim('/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        var url = "";

        for (var i = 0; i < routeParts.Length; i++)
        {
            var routePart = routeParts[i];
            var isCurrentPage = i == routeParts.Length - 1;

            url += "/" + routePart;

            viewModel.Items.Add(
                new BreadCrumbViewModel.BreadCrumbItem
                {
                    Label = GetLabel(routePart),
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
                    char.ToUpperInvariant(word[0])
                    + word[1..].ToLowerInvariant()));
    }
}