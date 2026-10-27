using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Content;
using ETG.Web.Features.TravelType;
using ETG.Web.Infrastructure;
using ETG.Web.Services.GoogleReviews;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using ContainerContent = ETG.Core.Content.Container;

namespace ETG.Web.Tests.Features.TravelType;

[TestFixture]
public class TravelTypeDetailPageTests : BaseWebTest
{
    private Mock<IMediator> mediator = null!;
    private Mock<IWebsiteChannelContext> channel = null!;
    private Mock<IWebPageDataContextRetriever> context = null!;
    private Mock<IGoogleReviewsService> reviews = null!;

    private HttpContextAccessor accessor = null!;
    private WebPageMetaService metadata = null!;
    private TravelTypeDetailPageTemplateController controller = null!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();

        mediator = new(MockBehavior.Strict);
        channel = new(MockBehavior.Strict);
        context = new(MockBehavior.Strict);
        reviews = new(MockBehavior.Strict);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("example.com");

        accessor = new HttpContextAccessor
        {
            HttpContext = httpContext
        };

        metadata = new WebPageMetaService(
            mediator.Object,
            accessor);

        controller = new TravelTypeDetailPageTemplateController(
            mediator.Object,
            channel.Object,
            context.Object,
            metadata,
            reviews.Object)
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

    private static IEnumerable<TestCaseData> ContextCases()
    {
        yield return new TestCaseData(false)
            .SetName("Index_ContextUnavailable_Returns404");

        yield return new TestCaseData(true)
            .SetName(
                "Index_RetrievalSucceedsWithNullContext_CurrentlyThrows");
    }

    [TestCaseSource(nameof(ContextCases))]
    [Category("CurrentBehavior")]
    public async Task Index_NullContext_FollowsCurrentBehavior(
        bool retrieved)
    {
        // Arrange
        WebPageDataContext data = null!;

        context
            .Setup(x => x.TryRetrieve(out data))
            .Returns(retrieved);

        metadata.SetMeta(
            new WebPageMeta(
                "Existing title",
                "Existing description",
                "existing-keywords"));

        // Act / Assert
        if (retrieved)
        {
            // Existing null-safety gap: data.WebPage is dereferenced.
            Assert.ThrowsAsync<NullReferenceException>(
                async () => await controller.Index());
        }
        else
        {
            var result = await controller.Index();

            Assert.That(
                result,
                Is.TypeOf<NotFoundResult>());
        }

        var actualMetadata = await metadata.GetMeta();

        Assert.Multiple(() =>
        {
            Assert.That(
                actualMetadata.Title,
                Is.EqualTo("Existing title"));

            Assert.That(
                actualMetadata.Description,
                Is.EqualTo("Existing description"));

            Assert.That(
                actualMetadata.Keywords,
                Is.EqualTo("existing-keywords"));
        });

        context.Verify(
            x => x.TryRetrieve(out data),
            Times.Once);

        VerifyNoDownstreamCalls();
    }

    private static IEnumerable<TestCaseData> ContextFailureCases()
    {
        yield return new TestCaseData(
                new InvalidOperationException(""))
            .SetName("Index_ContextException_EmptyMessage_Propagates");

        yield return new TestCaseData(
                new InvalidOperationException("Context unavailable"))
            .SetName("Index_ContextException_Propagates");

        yield return new TestCaseData(
                new OperationCanceledException())
            .SetName("Index_ContextCancellation_Propagates");
    }

    [TestCaseSource(nameof(ContextFailureCases))]
    [Category("CurrentBehavior")]
    public void Index_ContextFailure_PropagatesOriginalException(
        Exception expected)
    {
        // Arrange
        WebPageDataContext data = null!;

        context
            .Setup(x => x.TryRetrieve(out data))
            .Throws(expected);

        // Act
        var actual = Assert.CatchAsync(
            async () => await controller.Index());

        // Assert
        Assert.That(actual, Is.SameAs(expected));

        context.Verify(
            x => x.TryRetrieve(out data),
            Times.Once);

        VerifyNoDownstreamCalls();
    }

    [TestCase(null, "", "", null)]
    [TestCase("", "", "", "")]
    [TestCase(" \t", "", "", " \t")]
    [TestCase("Travel", "Travel", "Travel", "Travel")]
    [Category("MetadataService")]
    public async Task Metadata_NullOrBlankFields_UsesActualFallbacks(
        string? input,
        string expectedTitle,
        string expectedDescription,
        string? expectedKeywords)
    {
        // Arrange: exercise the real metadata service with
        // the concrete content model supplied previously.
        var page = new ContainerContent
        {
            MetaDataSchema__PageTitle = input!,
            MetaDataSchema__PageDescription = input!,
            MetaDataSchema__PageKeywords = input!
        };

        // Act
        metadata.SetMeta(page, null);
        var result = await metadata.GetMeta();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(
                result.Title,
                Is.EqualTo(expectedTitle));

            Assert.That(
                result.Description,
                Is.EqualTo(expectedDescription));

            Assert.That(
                result.Keywords,
                Is.EqualTo(expectedKeywords));

            Assert.That(result.ShareImage, Is.Empty);
        });

        VerifyNoDownstreamCalls();
    }

    [TestCase(null, true, "")]
    [TestCase("", true, "")]
    [TestCase(" \t", true, "")]
    [TestCase(null, false, "")]
    [TestCase("", false, "")]
    [TestCase(" \t", false, "")]
    [TestCase("/hero.jpg", false, "")]
    [TestCase(
        "/hero.jpg",
        true,
        "https://example.com/hero.jpg")]
    [TestCase(
        "https://cdn.example.com/hero.jpg",
        false,
        "https://cdn.example.com/hero.jpg")]
    [Category("MetadataService")]
    public async Task Metadata_MissingImageOrRequest_HandlesActualCases(
        string? imageUrl,
        bool hasHttpContext,
        string expectedImage)
    {
        // Arrange
        if (!hasHttpContext)
        {
            accessor.HttpContext = null;
        }

        var page = new ContainerContent
        {
            MetaDataSchema__PageTitle = "Travel"
        };

        // Act: use the overload called by the controller.
        metadata.SetMeta(page, imageUrl);
        var result = await metadata.GetMeta();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(
                result.Title,
                Is.EqualTo("Travel"));

            Assert.That(
                result.ShareImage,
                Is.EqualTo(expectedImage));
        });

        VerifyNoDownstreamCalls();
    }

    [Test]
    [Category("CurrentBehavior")]
    [Category("MetadataService")]
    public void Metadata_NullSchema_CurrentlyThrows()
    {
        // Arrange / Act / Assert:
        // documents a missing guard, not safe handling.
        Assert.Throws<NullReferenceException>(
            () => metadata.SetMeta(
                (IMetaDataSchema)null!,
                null));

        VerifyNoDownstreamCalls();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" \t")]
    [Category("MetadataService")]
    public async Task Metadata_MissingImage_ClearsPreviousShareImage(
        string? replacementImage)
    {
        // Arrange
        var page = new ContainerContent
        {
            MetaDataSchema__PageTitle = "Travel"
        };

        metadata.SetMeta(page, "/previous.jpg");

        var before = await metadata.GetMeta();

        Assert.That(
            before.ShareImage,
            Is.EqualTo("https://example.com/previous.jpg"));

        // Act
        metadata.SetMeta(page, replacementImage);
        var after = await metadata.GetMeta();

        // Assert: a previous image must not survive.
        Assert.Multiple(() =>
        {
            Assert.That(after.ShareImage, Is.Empty);
            Assert.That(after.Title, Is.EqualTo("Travel"));
        });

        VerifyNoDownstreamCalls();
    }

    private void VerifyNoDownstreamCalls()
    {
        mediator.VerifyNoOtherCalls();
        channel.VerifyNoOtherCalls();
        reviews.VerifyNoOtherCalls();
    }
}