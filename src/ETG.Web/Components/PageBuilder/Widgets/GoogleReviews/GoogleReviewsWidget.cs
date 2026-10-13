using CSharpFunctionalExtensions;
using ETG.Core.Models.GoogleReviews;
using ETG.Web.Services.GoogleReviews;
using ETG.Web.Components.PageBuilder.Widgets.GoogleReviews;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Websites.FormAnnotations;
using Microsoft.AspNetCore.Mvc;
using XperienceCommunity.KenticoComponentIcons;

[assembly: RegisterWidget(
    identifier: GoogleReviewsWidget.IDENTIFIER,
    viewComponentType: typeof(GoogleReviewsWidget),
    name: GoogleReviewsWidget.NAME,
    propertiesType: typeof(GoogleReviewsWidgetProperties),
    Description = "Displays Google Reviews with star rating, average, and individual review cards.",
    IconClass = KenticoIcons.BUBBLE,
    AllowCache = true)]

namespace ETG.Web.Components.PageBuilder.Widgets.GoogleReviews;

public class GoogleReviewsWidget(IGoogleReviewsService reviewsService) : ViewComponent
{
    public const string IDENTIFIER = "ETG.Web.Widget.GoogleReviewsWidget";
    public const string NAME = "Google Reviews";

    private readonly IGoogleReviewsService reviewsService = reviewsService;

    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<GoogleReviewsWidgetProperties> cvm)
    {
        var props = cvm.Properties;

        var reviews = await reviewsService.GetReviewsAsync();

        return Validate(props, reviews)
        .Match(
        vm => View("~/Components/PageBuilder/Widgets/GoogleReviews/GoogleReviews.cshtml", vm),
        vm => View("~/Components/ComponentError.cshtml", vm));
    }

    private static Result<GoogleReviewsWidgetViewModel, ComponentErrorViewModel> Validate(GoogleReviewsWidgetProperties props, ReviewListingResponse? reviews)
    {
        if (reviews is null)
        {
            return Result.Failure<GoogleReviewsWidgetViewModel, ComponentErrorViewModel>(
            new ComponentErrorViewModel(NAME, ComponentType.Widget, "No Google Reviews data is available yet. Please run the scheduled task to fetch reviews."));
        }
        return new GoogleReviewsWidgetViewModel(props, reviews);
    }
}

public class GoogleReviewsWidgetProperties : BaseWidgetProperties
{
    [TextInputComponent(
        Label = "Heading",
        ExplanationText = "Heading displayed above the reviews",
        WatermarkText = "What our customers say",
        Order = 1)]
    public string Heading { get; set; } = "What our customers say";

    [UrlSelectorComponent(
        Label = "View all URL",
        ExplanationText = "Link to the business's full Google listing (optional)",
        Order = 2)]
    public string ViewAllUrl { get; set; } = "";
}

public class GoogleReviewsWidgetViewModel : BaseWidgetViewModel
{
    protected override string WidgetName => GoogleReviewsWidget.NAME;

    public string Heading { get; }
    public string? ViewAllUrl { get; }
    public ReviewListingResponse Reviews { get; }

    public GoogleReviewsWidgetViewModel(GoogleReviewsWidgetProperties props, ReviewListingResponse reviews)
    {
        Heading = props.Heading;
        ViewAllUrl = string.IsNullOrWhiteSpace(props.ViewAllUrl) ? null : props.ViewAllUrl;
        Reviews = reviews;
    }
}