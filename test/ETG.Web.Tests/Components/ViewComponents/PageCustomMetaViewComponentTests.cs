using ETG.Web.Components.ViewComponents.PageCustomMeta;
using ETG.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;

namespace ETG.Web.Tests.Components.ViewComponents;

[TestFixture]
public class PageCustomMetaViewComponentTests : BaseWebTest
{
    private Mock<IMediator> _mockMediator = null!;
    private Mock<IHttpContextAccessor> _mockAccessor = null!;
    private WebPageMetaService _metaService = null!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        _mockMediator = new Mock<IMediator>();
        _mockAccessor = new Mock<IHttpContextAccessor>();
        var options = Options.Create(new ReCaptchaSettings { SiteKey = "site-key" });
        _metaService = new WebPageMetaService(_mockMediator.Object, _mockAccessor.Object);
    }

    //[Test]
    //public async Task InvokeAsync_ReturnsView_WithMeta()
    //{
    //    var component = new PageCustomMetaViewComponent(_mockMediator.Object, _metaService, _mockAccessor.Object, Options.Create(new ReCaptchaSettings { SiteKey = "site-key" }));

    //    _mockAccessor.Setup(a => a.HttpContext).Returns(new DefaultHttpContext());

    //    var result = await component.InvokeAsync();

    //    Assert.That(result, Is.Not.Null);
    //}
}
