using System;
using System.Threading.Tasks;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Content;
using ETG.Web.Features.Experience;
using ETG.Web.Infrastructure;
using ETG.Web.Rendering;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

using ContainerContent = ETG.Core.Content.Container;

namespace ETG.Web.Tests.Features.Experience;

[TestFixture]
public class ExperienceLandingPageTests : BaseWebTest
{
    private Mock<IMediator> mediator = null!;
    private Mock<IWebPageDataContextRetriever> context = null!;
    private Mock<IWebPageUrlRetriever> urls = null!;
    private Mock<IWebsiteChannelContext> channel = null!;

    private HttpContextAccessor accessor = null!;
    private WebPageMetaService metadata = null!;
    private ExperienceLandingPageTemplateController controller = null!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();

        mediator = new(MockBehavior.Strict);
        context = new(MockBehavior.Strict);
        urls = new(MockBehavior.Strict);
        channel = new(MockBehavior.Strict);

        accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        };

        accessor.HttpContext.Request.Scheme = "https";
        accessor.HttpContext.Request.Host =
            new HostString("example.com");

        metadata = new WebPageMetaService(
            mediator.Object,
            accessor);

        controller = new ExperienceLandingPageTemplateController(
            mediator.Object,
            urls.Object,
            channel.Object,
            context.Object,
            metadata)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = accessor.HttpContext
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
    public void Index_RetrievedNullContext_Throws_CurrentBehavior()
    {
        // Arrange: retrieval reports success but supplies no context.
        WebPageDataContext data = null!;
        context
            .Setup(x => x.TryRetrieve(out data))
            .Returns(true);

        // Act / Assert: the controller dereferences data.WebPage.
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
    public void Index_ContextException_PropagatesSameException(
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
    public void Index_ContextCancellation_PropagatesSameException()
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

        // Assert: propagation, not request-token forwarding.
        Assert.That(actual, Is.SameAs(expected));

        VerifyNoDownstreamCalls();
    }

    [TestCase(null, "", "", null)]
    [TestCase("", "", "", "")]
    [TestCase(" \t", "", "", " \t")]
    [TestCase("Adventure", "Adventure", "Adventure", "Adventure")]
    [TestCase(
        " Adventure ",
        " Adventure ",
        " Adventure ",
        " Adventure ")]
    [TestCase(
        "<script>alert(1)</script>",
        "<script>alert(1)</script>",
        "<script>alert(1)</script>",
        "<script>alert(1)</script>")]
    public async Task Metadata_MapsFields_WithoutInventedValidation(
        string? input,
        string expectedTitle,
        string expectedDescription,
        string? expectedKeywords)
    {
        // Arrange: these are CMS fields, not HTTP-bound input.
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
    }

    [TestCase(null, "", true)]
    [TestCase("", "", true)]
    [TestCase(" \t", "", true)]
    [TestCase(
        "/images/hero.jpg",
        "https://example.com/images/hero.jpg",
        true)]
    [TestCase(
        "~/images/hero.jpg",
        "https://example.com/images/hero.jpg",
        true)]
    [TestCase(
        "images/hero.jpg",
        "https://example.com/images/hero.jpg",
        true)]
    [TestCase(
        " /images/hero.jpg ",
        "https://example.com/images/hero.jpg",
        true)]
    [TestCase(
        "//cdn.example.com/hero.jpg",
        "https://cdn.example.com/hero.jpg",
        true)]
    [TestCase(
        "https://cdn.example.com/hero.jpg",
        "https://cdn.example.com/hero.jpg",
        true)]
    [TestCase(
        "http://cdn.example.com/hero.jpg",
        "http://cdn.example.com/hero.jpg",
        true)]
    [TestCase("javascript:alert(1)", "", true)]
    [TestCase("data:text/plain,hello", "", true)]
    [TestCase("ftp://cdn.example.com/hero.jpg", "", true)]
    [TestCase("/images/hero.jpg", "", false)]
    [TestCase(
        "https://cdn.example.com/hero.jpg",
        "https://cdn.example.com/hero.jpg",
        false)]
    [TestCase(
        "//cdn.example.com/hero.jpg",
        "https://cdn.example.com/hero.jpg",
        false)]
    public async Task Metadata_ExplicitImage_NormalizesActualUrl(
        string? input,
        string expected,
        bool hasRequest)
    {
        // Arrange
        if (!hasRequest)
        {
            accessor.HttpContext = null;
        }

        var page = new ContainerContent
        {
            MetaDataSchema__PageTitle = "Adventure",
            MetaDataSchema__PageDescription = "Explore",
            MetaDataSchema__PageKeywords = "travel"
        };

        // Act: this is the overload used by the controller.
        metadata.SetMeta(page, input);
        var result = await metadata.GetMeta();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Title, Is.EqualTo("Adventure"));
            Assert.That(result.Description, Is.EqualTo("Explore"));
            Assert.That(result.Keywords, Is.EqualTo("travel"));
            Assert.That(result.ShareImage, Is.EqualTo(expected));
        });
    }

    [TestCase(
        "https",
        "/travel",
        "images/hero.jpg",
        "https://example.com/travel/images/hero.jpg")]
    [TestCase(
        "https",
        "/travel",
        "~/images/hero.jpg",
        "https://example.com/travel/images/hero.jpg")]
    [TestCase(
        "https",
        "/travel",
        "/images/hero.jpg",
        "https://example.com/images/hero.jpg")]
    [TestCase(
        "http",
        "",
        "//cdn.example.com/hero.jpg",
        "http://cdn.example.com/hero.jpg")]
    public async Task Metadata_UsesRequestSchemeAndPathBase(
        string scheme,
        string pathBase,
        string image,
        string expected)
    {
        // Arrange
        var request = accessor.HttpContext!.Request;
        request.Scheme = scheme;
        request.PathBase = new PathString(pathBase);

        var page = new ContainerContent();

        // Act
        metadata.SetMeta(page, image);
        var result = await metadata.GetMeta();

        // Assert
        Assert.That(result.ShareImage, Is.EqualTo(expected));
    }

    [Test]
    public async Task Metadata_SecondAssignment_ReplacesPreviousState()
    {
        // Arrange
        var first = new ContainerContent
        {
            MetaDataSchema__PageTitle = "First",
            MetaDataSchema__PageDescription = "First description",
            MetaDataSchema__PageKeywords = "first"
        };

        var second = new ContainerContent
        {
            MetaDataSchema__PageTitle = "Second",
            MetaDataSchema__PageDescription = "Second description",
            MetaDataSchema__PageKeywords = "second"
        };

        metadata.SetMeta(first, "/first.jpg");

        // Act
        metadata.SetMeta(second, null);
        var result = await metadata.GetMeta();

        // Assert: no stale metadata or share image survives.
        Assert.Multiple(() =>
        {
            Assert.That(result.Title, Is.EqualTo("Second"));
            Assert.That(
                result.Description,
                Is.EqualTo("Second description"));
            Assert.That(result.Keywords, Is.EqualTo("second"));
            Assert.That(result.ShareImage, Is.Empty);
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" \t")]
    [TestCase("Adventure")]
    [TestCase("<script>alert(1)</script>")]
    public void ImageMapping_MissingSystemFields_Throws_CurrentBehavior(
        string? title)
    {
        // Arrange: a selected media item exists, but is malformed.
        var media = new MediaFile
        {
            SystemFields = null!,
            LegacyMediaFileTitle = title!,
            LegacyMediaFileDescription = "Description"
        };

        // Act / Assert
        Assert.Throws<NullReferenceException>(
            () => ImageAssetViewModel.Create(media));
    }

    [Test]
    public void ImageMapping_NullMedia_Throws_CurrentBehavior()
    {
        // Arrange / Act / Assert
        Assert.Throws<NullReferenceException>(
            () => ImageAssetViewModel.Create(null!));
    }

    private void VerifyNoDownstreamCalls()
    {
        mediator.VerifyNoOtherCalls();
        urls.VerifyNoOtherCalls();
        channel.VerifyNoOtherCalls();
    }
}