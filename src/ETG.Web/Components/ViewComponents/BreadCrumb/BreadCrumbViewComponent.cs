using CMS.Websites;
using ETG.Core.Content;
using Kentico.Content.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using ExperienceContent = ETG.Core.Content.Experience;
using TourContent = ETG.Core.Content.Tour;

namespace ETG.Web.Components.ViewComponents.BreadCrumb;

public class BreadCrumbViewComponent(
    IContentRetriever contentRetriever,
    IWebPageUrlRetriever urlRetriever) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var currentPath = HttpContext.Request.Path.Value ?? "/";

        var currentPageTitle =
            ViewContext.ViewData["Title"]?.ToString();

        var modelType =
            ViewContext.ViewData.Model?.GetType().ToString() ?? "";

        // Existing article detection.
        bool isArticleLanding =
            modelType.Contains("ArticleLanding");

        bool isArticlePost =
            modelType.Contains("ArticlePost")
            || (modelType.Contains("Article") && !isArticleLanding);

        var articleLandingTitle =
            ViewContext.ViewData["ArticleLandingTitle"]?.ToString();

        BreadCrumbViewModel viewModel;

        // Applies only to the actual Tour supplied by the tour controller.
        if (ViewContext.ViewData["BreadCrumbTour"] is TourContent tour)
        {
            var parent = await GetTourParentAsync(tour);

            var title = !string.IsNullOrWhiteSpace(tour.TourName)
                ? tour.TourName
                : !string.IsNullOrWhiteSpace(currentPageTitle)
                    ? currentPageTitle
                    : !string.IsNullOrWhiteSpace(tour.TourCode)
                        ? tour.TourCode
                        : "Tour";

            viewModel = GetBreadCrumbQuery.Get(
                currentPath: currentPath,
                currentPageTitle: currentPageTitle,
                tourTitle: title,
                tourParent: parent);
        }
        else
        {
            // Preserve existing behavior for other pages.
            viewModel = GetBreadCrumbQuery.Get(
                currentPath,
                currentPageTitle,
                isArticleLanding,
                isArticlePost,
                articleLandingTitle);
        }

        return View(
            "~/Components/ViewComponents/BreadCrumb/BreadCrumb.cshtml",
            viewModel);
    }

    private async Task<BreadCrumbViewModel.BreadCrumbItem?>
        GetTourParentAsync(TourContent tour)
    {
        // Priority 1: primary country.
        var countryLink = tour.TourPrimaryCountry?
            .FirstOrDefault(item =>
                item is not null
                && item.WebPageGuid != Guid.Empty);

        if (countryLink is not null)
        {
            var countries = await contentRetriever
                .RetrievePagesByGuids<Destination>(
                    [countryLink.WebPageGuid]);

            var country = countries.FirstOrDefault();

            if (country is not null)
            {
                var label =
                    !string.IsNullOrWhiteSpace(country.DestinationName)
                        ? country.DestinationName
                        : country.DestinationHeading;

                if (!string.IsNullOrWhiteSpace(label))
                {
                    var url = await urlRetriever.Retrieve(country);

                    if (!string.IsNullOrWhiteSpace(url?.RelativePath))
                    {
                        return CreateParent(label, url.RelativePath);
                    }
                }
            }
        }

        // Priority 2: primary experience.
        var experienceLink = tour.TourPrimaryExperience?
            .FirstOrDefault(item =>
                item is not null
                && item.WebPageGuid != Guid.Empty);

        if (experienceLink is not null)
        {
            var experiences = await contentRetriever
                .RetrievePagesByGuids<ExperienceContent>(
                    [experienceLink.WebPageGuid]);

            var experience = experiences.FirstOrDefault();

            if (experience is not null)
            {
                var label =
                    !string.IsNullOrWhiteSpace(experience.ExperienceName)
                        ? experience.ExperienceName
                        : experience.ExperienceHeading;

                if (!string.IsNullOrWhiteSpace(label))
                {
                    var url = await urlRetriever.Retrieve(experience);

                    if (!string.IsNullOrWhiteSpace(url?.RelativePath))
                    {
                        return CreateParent(label, url.RelativePath);
                    }
                }
            }
        }

        // Priority 3: cruise ONLY when explicitly enabled.
        if (tour.TourIsCruise
            && !string.IsNullOrWhiteSpace(tour.TourCruiseType))
        {
            // Optional real landing URL supplied by existing routing.
            // Do not manufacture a URL from the cruise-type text.
            var cruiseUrl =
                ViewContext.ViewData["TourCruiseBreadcrumbUrl"]
                    ?.ToString();

            return CreateParent(
                tour.TourCruiseType.Trim(),
                string.IsNullOrWhiteSpace(cruiseUrl) ? "" : cruiseUrl);
        }

        return null;
    }

    private static BreadCrumbViewModel.BreadCrumbItem CreateParent(
        string label,
        string url)
    {
        return new BreadCrumbViewModel.BreadCrumbItem
        {
            Label = label,
            Url = url,
            IsCurrentPage = false
        };
    }
}