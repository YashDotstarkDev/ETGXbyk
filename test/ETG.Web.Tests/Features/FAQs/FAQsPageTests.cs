using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Web.Features.FAQs;
using ETG.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace ETG.Web.Tests.Features.FAQs;

[TestFixture]
public class FAQsPageTests : BaseWebTest
{
    private Mock<IWebPageDataContextRetriever> context = null!;
    private Mock<IContentQueryExecutor> executor = null!;
    private Mock<IWebsiteChannelContext> channel = null!;
    private Mock<IMediator> mediator = null!;

    private WebPageMetaService metadata = null!;
    private FAQsPageTemplateController controller = null!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();

        context = new(MockBehavior.Strict);
        executor = new(MockBehavior.Strict);
        channel = new(MockBehavior.Strict);
        mediator = new(MockBehavior.Strict);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("example.com");

        metadata = new WebPageMetaService(
            mediator.Object,
            new HttpContextAccessor
            {
                HttpContext = httpContext
            });

        controller = new FAQsPageTemplateController(
            context.Object,
            executor.Object,
            channel.Object,
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

    [TestCase(false)]
    [TestCase(true)]
    public async Task Index_MissingContext_Returns404_WithoutQuerying(
        bool preview)
    {
        // Arrange
        WebPageDataContext data = null!;

        context
            .Setup(x => x.TryRetrieve(out data))
            .Returns(false);

        channel
            .SetupGet(x => x.IsPreview)
            .Returns(preview);

        // Act
        var result = await controller.Index();

        // Assert
        Assert.That(result, Is.TypeOf<NotFoundResult>());

        context.Verify(
            x => x.TryRetrieve(out data),
            Times.Once);

        // Missing context short-circuits before reading preview state.
        VerifyNoDownstreamCalls();
    }

    [TestCase("")]
    [TestCase("Context unavailable")]
    [TestCase("Unexpected routing failure")]
    public void Index_ContextFailure_PropagatesOriginalException(
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
    public void Index_ContextCancellation_PropagatesOriginalException()
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

        // Assert
        Assert.That(actual, Is.SameAs(expected));

        VerifyNoDownstreamCalls();
    }

    private void VerifyNoDownstreamCalls()
    {
        executor.VerifyNoOtherCalls();
        channel.VerifyNoOtherCalls();
        mediator.VerifyNoOtherCalls();
    }
}