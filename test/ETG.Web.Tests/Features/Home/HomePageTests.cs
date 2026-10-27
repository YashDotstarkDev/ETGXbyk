using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Web.Features.Home;
using ETG.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;

namespace ETG.Web.Tests.Features.Home;

[TestFixture]
public class HomePageTests : BaseWebTest
{
    private Mock<IMediator> mediator = null!;
    private Mock<IWebPageDataContextRetriever> context = null!;
    private Mock<IContentQueryExecutor> executor = null!;
    private Mock<IWebsiteChannelContext> channel = null!;
    private Mock<IWebPageUrlRetriever> urls = null!;
    private Mock<ITaxonomyRetriever> taxonomy = null!;
    private Mock<IConfiguration> configuration = null!;

    private HomePageTemplateController controller = null!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();

        mediator = new(MockBehavior.Strict);
        context = new(MockBehavior.Strict);
        executor = new(MockBehavior.Strict);
        channel = new(MockBehavior.Strict);
        urls = new(MockBehavior.Strict);
        taxonomy = new(MockBehavior.Strict);
        configuration = new(MockBehavior.Strict);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("example.com");

        var metadata = new WebPageMetaService(
            mediator.Object,
            new HttpContextAccessor
            {
                HttpContext = httpContext
            });

        controller = new HomePageTemplateController(
            mediator.Object,
            context.Object,
            executor.Object,
            channel.Object,
            urls.Object,
            taxonomy.Object,
            configuration.Object,
            metadata)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };
    }

    [TearDown]
    public void TearDown()
    {
        controller?.Dispose();
    }

    [Test]
    public async Task Index_MissingContext_Returns404()
    {
        // Arrange
        WebPageDataContext data = null!;

        context
            .Setup(x => x.TryRetrieve(out data))
            .Returns(false);

        // Act
        var result = await controller.Index();

        // Assert
        Assert.That(result, Is.TypeOf<NotFoundResult>());

        context.Verify(
            x => x.TryRetrieve(out data),
            Times.Once);

        VerifyNoDownstreamCalls();
    }

    [Test]
    [Category("CurrentBehavior")]
    public void Index_SuccessfulRetrievalWithNullContext_Throws()
    {
        // Arrange: the retriever violates its usual success contract.
        WebPageDataContext data = null!;

        context
            .Setup(x => x.TryRetrieve(out data))
            .Returns(true);

        // Act / Assert: data.WebPage has no null guard.
        Assert.ThrowsAsync<NullReferenceException>(
            async () => await controller.Index());

        context.Verify(
            x => x.TryRetrieve(out data),
            Times.Once);

        VerifyNoDownstreamCalls();
    }

    [TestCase("")]
    [TestCase("Context unavailable")]
    [TestCase("Unexpected routing failure")]
    public void Index_ContextException_PropagatesSameInstance(
        string message)
    {
        // Arrange
        WebPageDataContext data = null!;
        var expected = new InvalidOperationException(message);

        context
            .Setup(x => x.TryRetrieve(out data))
            .Throws(expected);

        // Act
        var actual = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await controller.Index());

        // Assert
        Assert.That(actual, Is.SameAs(expected));

        context.Verify(
            x => x.TryRetrieve(out data),
            Times.Once);

        VerifyNoDownstreamCalls();
    }

    [Test]
    public void Index_ContextCancellation_PropagatesSameInstance()
    {
        // Arrange
        WebPageDataContext data = null!;
        var expected = new OperationCanceledException();

        context
            .Setup(x => x.TryRetrieve(out data))
            .Throws(expected);

        // Act
        var actual = Assert.ThrowsAsync<OperationCanceledException>(
            async () => await controller.Index());

        // Assert: exception propagation, not token forwarding.
        Assert.That(actual, Is.SameAs(expected));

        context.Verify(
            x => x.TryRetrieve(out data),
            Times.Once);

        VerifyNoDownstreamCalls();
    }

    private void VerifyNoDownstreamCalls()
    {
        mediator.VerifyNoOtherCalls();
        executor.VerifyNoOtherCalls();
        channel.VerifyNoOtherCalls();
        urls.VerifyNoOtherCalls();
        taxonomy.VerifyNoOtherCalls();
        configuration.VerifyNoOtherCalls();
    }
}