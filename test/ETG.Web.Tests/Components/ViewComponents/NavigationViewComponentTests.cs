using ETG.Web.Components.ViewComponents.MainNavigation;
using MediatR;
using Moq;
using CSharpFunctionalExtensions;
using ETG.Web.Components.ViewComponents.MainNavigation;

namespace ETG.Web.Tests.Components.ViewComponents;

[TestFixture]
public class NavigationViewComponentTests : BaseWebTest
{
    private Mock<IMediator> _mockMediator = null!;
    private NavigationViewComponent? _component;

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        _mockMediator = new Mock<IMediator>();
        _component = new NavigationViewComponent(_mockMediator.Object);
    }

    [Test]
    public async Task InvokeAsync_ReturnsEmptyModel_WhenMediatorReturnsNone()
    {
        _mockMediator.Setup(m => m.Send(It.IsAny<GetNavigationHierarchyQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<NavigationViewModel>.None);

        var result = await _component!.InvokeAsync();
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<Microsoft.AspNetCore.Mvc.ViewComponents.ViewViewComponentResult>());
        var viewResult = result as Microsoft.AspNetCore.Mvc.ViewComponents.ViewViewComponentResult;
        Assert.That(viewResult!.ViewData.Model, Is.InstanceOf<NavigationViewModel>());
    }

    [Test]
    public async Task InvokeAsync_ReturnsModel_WhenMediatorReturnsValue()
    {
        var vm = new NavigationViewModel();
        vm.RootItems.Add(new NavigationItemViewModel { Id = "1", Name = "Home" });

        _mockMediator.Setup(m => m.Send(It.IsAny<GetNavigationHierarchyQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(vm));

        var result = await _component!.InvokeAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<Microsoft.AspNetCore.Mvc.ViewComponents.ViewViewComponentResult>());
        var viewResult = result as Microsoft.AspNetCore.Mvc.ViewComponents.ViewViewComponentResult;
        Assert.That(viewResult!.ViewData.Model, Is.InstanceOf<NavigationViewModel>());
        var model = viewResult.ViewData.Model as NavigationViewModel;
        Assert.That(model!.RootItems.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task InvokeAsync_ReturnsFallbackModel_WhenMediatorThrows()
    {
        _mockMediator.Setup(m => m.Send(It.IsAny<GetNavigationHierarchyQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("fail"));

        var result = await _component!.InvokeAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<Microsoft.AspNetCore.Mvc.ViewComponents.ViewViewComponentResult>());
        var viewResult = result as Microsoft.AspNetCore.Mvc.ViewComponents.ViewViewComponentResult;
        Assert.That(viewResult!.ViewData.Model, Is.InstanceOf<NavigationViewModel>());
    }
}
