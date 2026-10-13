using ETG.Core.Content;
using ETG.Web.Features.SEO;
using ETG.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ETG.Web.Tests.Features.SEO;

[TestFixture]
public class SeoControllerTests : BaseWebTest
{
    private SeoController _controller = null!;
    private Mock<IMediator> _mockMediator = null!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();

        _mockMediator = new Mock<IMediator>();

        // Pass null for SitemapRetriever (we won't execute it)
        _controller = new SeoController(null!, _mockMediator.Object);
    }

    [TearDown]
    public override void TearDown()
    {
        base.TearDown();
        _controller?.Dispose();
    }

    #region Routing Tests

    [Test]
    public void Controller_HasRouteAttribute()
    {
        var controllerType = typeof(SeoController);
        var routeAttribute = controllerType
            .GetCustomAttributes(typeof(RouteAttribute), true)
            .FirstOrDefault() as RouteAttribute;

        Assert.That(routeAttribute, Is.Not.Null);
        Assert.That(routeAttribute?.Template, Is.EqualTo(""));
    }

    #endregion

    #region Constructor Tests

    [Test]
    public void Constructor_InitializesWithDependencies()
    {
        Assert.That(_controller, Is.Not.Null);
    }

    #endregion

    #region Sitemap Method Tests

    [Test]
    public void Sitemap_Method_HasHttpGetAttribute()
    {
        var method = typeof(SeoController).GetMethod("Sitemap");
        var httpGetAttribute = method?
            .GetCustomAttributes(typeof(HttpGetAttribute), true)
            .FirstOrDefault();

        Assert.That(httpGetAttribute, Is.Not.Null);
    }

    [Test]
    public void Sitemap_Method_HasCorrectRoute()
    {
        var method = typeof(SeoController).GetMethod("Sitemap");
        var httpGetAttribute = method?
            .GetCustomAttributes(typeof(HttpGetAttribute), true)
            .FirstOrDefault() as HttpGetAttribute;

        Assert.That(httpGetAttribute?.Template, Is.EqualTo("sitemap.xml"));
    }

    [Test]
    public void Sitemap_Method_Exists()
    {
        var method = typeof(SeoController).GetMethod("Sitemap");

        Assert.That(method, Is.Not.Null);
    }

    #endregion

    #region RobotsTxt Method Tests

    [Test]
    public void RobotsTxt_Method_HasHttpGetAttribute()
    {
        var method = typeof(SeoController).GetMethod("RobotsTxt");
        var httpGetAttribute = method?
            .GetCustomAttributes(typeof(HttpGetAttribute), true)
            .FirstOrDefault();

        Assert.That(httpGetAttribute, Is.Not.Null);
    }

    [Test]
    public void RobotsTxt_Method_HasCorrectRoute()
    {
        var method = typeof(SeoController).GetMethod("RobotsTxt");
        var httpGetAttribute = method?
            .GetCustomAttributes(typeof(HttpGetAttribute), true)
            .FirstOrDefault() as HttpGetAttribute;

        Assert.That(httpGetAttribute?.Template, Is.EqualTo("robots.txt"));
    }

    [Test]
    public async Task RobotsTxt_ReturnsContentResult()
    {
        _mockMediator
            .Setup(x => x.Send(It.IsAny<WebsiteSettingsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WebsiteSettingsQueryResponse(
                null,
                new WebsiteGlobalContent
                {
                    WebsiteGlobalContentRobotsTxt = "User-agent: *"
                }));

        var result = await _controller.RobotsTxt();

        Assert.That(result, Is.TypeOf<ContentResult>());
    }

    [Test]
    public async Task RobotsTxt_SendsWebsiteSettingsQuery()
    {
        _mockMediator
            .Setup(x => x.Send(It.IsAny<WebsiteSettingsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WebsiteSettingsQueryResponse(
                null,
                new WebsiteGlobalContent
                {
                    WebsiteGlobalContentRobotsTxt = "User-agent: *"
                }));

        await _controller.RobotsTxt();

        _mockMediator.Verify(
            x => x.Send(It.IsAny<WebsiteSettingsQuery>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task RobotsTxt_ReturnsPlainTextContentType()
    {
        _mockMediator
            .Setup(x => x.Send(It.IsAny<WebsiteSettingsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WebsiteSettingsQueryResponse(
                null,
                new WebsiteGlobalContent
                {
                    WebsiteGlobalContentRobotsTxt = "User-agent: *"
                }));

        var result = await _controller.RobotsTxt() as ContentResult;

        Assert.That(result?.ContentType, Contains.Substring("text/plain"));
    }

    #endregion
}