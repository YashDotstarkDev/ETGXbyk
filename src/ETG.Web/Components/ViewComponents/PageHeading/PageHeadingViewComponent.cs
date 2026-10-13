using CSharpFunctionalExtensions;
using ETG.Core.Content;
using Microsoft.AspNetCore.Mvc;

namespace ETG.Web.Components.ViewComponents.PageHeading;

public class PageHeadingViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(
        BasePage page,
        bool displayDescription = true) =>
        View(
            "~/Components/ViewComponents/PageHeading/PageHeading.cshtml",
            new PageHeadingViewModel(
                page.Title,
                displayDescription
                    ? Maybe.From(page.ShortDescription)
                        .MapNullOrWhiteSpaceAsNone()
                    : Maybe<string>.None));
}

public record PageHeadingViewModel(
    string Title,
    Maybe<string> Description);