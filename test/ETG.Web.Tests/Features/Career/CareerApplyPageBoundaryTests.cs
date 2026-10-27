using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Web.Features.Careers;
using ETG.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ETG.Web.Tests.Features.Career;

[TestFixture]
public class CareerApplyPageBoundaryTests : BaseWebTest
{
    private const string Pass = "ExpectedToPass";

    private Mock<IWebPageDataContextRetriever> context = null!;
    private Mock<IContentQueryExecutor> executor = null!;
    private Mock<IWebsiteChannelContext> channel = null!;
    private Mock<ILogger<CareerApplyPageTemplateController>> logger = null!;
    private Mock<IMediator> mediator = null!;

    private CareerApplyPageTemplateController controller = null!;
    private WebPageMetaService metadata = null!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();

        context = new(MockBehavior.Strict);
        executor = new(MockBehavior.Strict);
        channel = new(MockBehavior.Strict);
        mediator = new(MockBehavior.Strict);
        logger = new();

        var http = new DefaultHttpContext();

        metadata = new WebPageMetaService(
            mediator.Object,
            new HttpContextAccessor { HttpContext = http });

        controller = new CareerApplyPageTemplateController(
            context.Object,
            executor.Object,
            channel.Object,
            logger.Object,
            metadata)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = http
            }
        };
    }

    [TearDown]
    public override void TearDown()
    {
        try
        {
            controller?.Dispose();
        }
        finally
        {
            base.TearDown();
        }
    }

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    [Category(Pass)]
    public async Task MissingContext_Returns404AndLogsWarning(
        bool retrieved,
        bool requestAborted)
    {
        // Arrange: cover both failed retrieval and true-with-null output.
        WebPageDataContext data = null!;
        context.Setup(x => x.TryRetrieve(out data)).Returns(retrieved);

        using var cancellation = new CancellationTokenSource();
        if (requestAborted)
            cancellation.Cancel();

        controller.HttpContext.RequestAborted = cancellation.Token;
        var before = await metadata.GetMeta();

        // Act
        var result = await controller.Index();

        // Assert: the context guard runs even when the request is aborted.
        Assert.That(result, Is.TypeOf<NotFoundResult>());

        context.Verify(x => x.TryRetrieve(out data), Times.Once);
        VerifyLog(
            LogLevel.Warning,
            "Unable to retrieve Career Apply page context.",
            null);

        Assert.That(await metadata.GetMeta(), Is.EqualTo(before));
        VerifyNoDownstreamCalls();
        logger.VerifyNoOtherCalls();
    }

    public static IEnumerable<TestCaseData> ExceptionCases()
    {
        yield return new TestCaseData("ordinary", false, false);
        yield return new TestCaseData("ordinary", true, false);

        yield return new TestCaseData("cancellation", false, false);
        yield return new TestCaseData("cancellation", true, true);

        yield return new TestCaseData("task-cancellation", false, false);
        yield return new TestCaseData("task-cancellation", true, true);
    }

    [TestCaseSource(nameof(ExceptionCases))]
    [Category(Pass)]
    public async Task ContextException_UsesActualCancellationAndErrorBehavior(
        string kind,
        bool requestAborted,
        bool expectEmptyResult)
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        if (requestAborted)
            cancellation.Cancel();

        controller.HttpContext.RequestAborted = cancellation.Token;

        Exception expected = kind switch
        {
            "cancellation" => new OperationCanceledException("Cancelled"),
            "task-cancellation" => new TaskCanceledException("Cancelled"),
            _ => new InvalidOperationException("Dependency failed")
        };

        WebPageDataContext data = null!;
        context.Setup(x => x.TryRetrieve(out data)).Throws(expected);

        var before = await metadata.GetMeta();

        // Act
        var result = await controller.Index();

        // Assert
        if (expectEmptyResult)
        {
            Assert.That(result, Is.TypeOf<EmptyResult>());
            logger.VerifyNoOtherCalls();
        }
        else
        {
            Assert.That(result, Is.TypeOf<StatusCodeResult>());
            Assert.That(
                ((StatusCodeResult)result).StatusCode,
                Is.EqualTo(StatusCodes.Status500InternalServerError));

            VerifyLog(
                LogLevel.Error,
                "Error loading Career Apply page.",
                expected);

            logger.VerifyNoOtherCalls();
        }

        context.Verify(x => x.TryRetrieve(out data), Times.Once);
        Assert.That(await metadata.GetMeta(), Is.EqualTo(before));
        VerifyNoDownstreamCalls();
    }

    private void VerifyLog(
        LogLevel level,
        string message,
        Exception? exception)
    {
        logger.Verify(x => x.Log(
            level,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, _) =>
                state.ToString() == message),
            It.Is<Exception?>(actual =>
                ReferenceEquals(actual, exception)),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private void VerifyNoDownstreamCalls()
    {
        executor.VerifyNoOtherCalls();
        channel.VerifyNoOtherCalls();
        mediator.VerifyNoOtherCalls();
    }
}