using CMS.ContentEngine;
using ETG.Core.Models.GoogleReviews;
using ETG.Web.Services.GoogleReviews;
using Microsoft.AspNetCore.Mvc;
using ETG.Core.Content;


namespace ETG.Web.Components.ViewComponents.StickyFooter;

public class StickyBarViewComponent : ViewComponent
{
    private readonly IContentQueryExecutor contentQueryExecutor;
    private readonly IGoogleReviewsService reviewsService;

    public StickyBarViewComponent(
        IContentQueryExecutor contentQueryExecutor,
        IGoogleReviewsService reviewsService)
    {
        this.contentQueryExecutor = contentQueryExecutor;
        this.reviewsService = reviewsService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var query = new ContentItemQueryBuilder()
            .ForContentType(
                StickyFooterBar.CONTENT_TYPE_NAME);

        var stickyFooterBars = await contentQueryExecutor.GetMappedResult<StickyFooterBar>(query);
        var stickyFooterBar = stickyFooterBars.FirstOrDefault();

        if (stickyFooterBar == null)
        {
            return Content(string.Empty);
        }

        var reviews = await reviewsService.GetReviewsAsync();

        var model = new StickyBarViewModel
        {
            StickyFooterBar = stickyFooterBar,
            Reviews = reviews
        };

        return View("~/Components/ViewComponents/StickyFooterBar/StickyBar.cshtml", model);
    }

    public class StickyBarViewModel
    {
        public StickyFooterBar? StickyFooterBar { get; set; }
        public ReviewListingResponse? Reviews { get; set; }
    }
}