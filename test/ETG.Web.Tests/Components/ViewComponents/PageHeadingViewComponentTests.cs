using ETG.Web.Components.ViewComponents.PageHeading;
using ETG.Core.Content;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Mvc.ViewComponents;
namespace ETG.Web.Tests.Components.ViewComponents;

[TestFixture]
public class PageHeadingViewComponentTests : BaseWebTest
{
    [Test]
    public void Invoke_ReturnsViewModelWithDescription_WhenDisplayDescriptionTrue()
    {
        BasePage basePage = new("Test", "Desc");
        var page = basePage;
        var component = new PageHeadingViewComponent();

        var result = component.Invoke(page, true) as ViewViewComponentResult;

        Assert.That(result, Is.Not.Null);
        var model = result!.ViewData.Model as PageHeadingViewModel;
        Assert.That(model, Is.Not.Null);
        Assert.That(model!.Title, Is.EqualTo("Test"));
        Assert.That(model.Description.HasValue, Is.True);
    }

    [Test]
    public void Invoke_ReturnsViewModelWithoutDescription_WhenDisplayDescriptionFalse()
    {
        var page = new BasePage("Test", "Desc");
        var component = new PageHeadingViewComponent();

        var result = component.Invoke(page, false) as ViewViewComponentResult;

        Assert.That(result, Is.Not.Null);
        var model = result!.ViewData.Model as PageHeadingViewModel;
        Assert.That(model, Is.Not.Null);
        Assert.That(model!.Description.HasValue, Is.False);
    }
}