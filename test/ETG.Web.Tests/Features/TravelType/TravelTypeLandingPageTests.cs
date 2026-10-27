using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Content;
using ETG.Web.Features.TravelType;
using ETG.Web.Infrastructure;
using ETG.Web.Rendering;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace ETG.Web.Tests.Features.TravelType;

[TestFixture]
public class TravelTypeLandingPageTests : BaseWebTest
{
    private Mock<IMediator> mediator = null!;
    private Mock<IWebPageUrlRetriever> urls = null!;
    private Mock<IWebsiteChannelContext> channel = null!;
    private Mock<IWebPageDataContextRetriever> context = null!;

    private WebPageMetaService metadata = null!;
    private TravelTypeLandingPageTemplateController controller = null!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();

        mediator = new(MockBehavior.Strict);
        urls = new(MockBehavior.Strict);
        channel = new(MockBehavior.Strict);
        context = new(MockBehavior.Strict);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("example.com");

        metadata = new WebPageMetaService(
            mediator.Object,
            new HttpContextAccessor
            {
                HttpContext = httpContext
            });

        controller = new TravelTypeLandingPageTemplateController(
            mediator.Object,
            urls.Object,
            channel.Object,
            context.Object,
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

    // ---------------------------------------------------------
    // Controller context tests
    // ---------------------------------------------------------

    [TestCase(false)]
    [TestCase(true)]
    [Category("ExpectedToPass")]
    public async Task Index_ContextUnavailable_Returns404(
        bool requestCancelled)
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();

        if (requestCancelled)
        {
            cancellation.Cancel();
        }

        controller.HttpContext.RequestAborted = cancellation.Token;

        WebPageDataContext data = null!;

        context
            .Setup(x => x.TryRetrieve(out data))
            .Returns(false);

        SeedMetadata();

        // Act
        var result = await controller.Index();

        // Assert
        Assert.That(result, Is.TypeOf<NotFoundResult>());

        context.Verify(
            x => x.TryRetrieve(out data),
            Times.Once);

        await AssertMetadataUnchanged();
        VerifyNoDownstreamCalls();
    }

    [Test]
    [Category("DocumentsNullSafetyGap")]
    public async Task Index_RetrievedNullContext_CurrentlyThrows()
    {
        // Arrange
        WebPageDataContext data = null!;

        context
            .Setup(x => x.TryRetrieve(out data))
            .Returns(true);

        SeedMetadata();

        // Act / Assert:
        // the controller dereferences data.WebPage.
        Assert.ThrowsAsync<NullReferenceException>(
            async () => await controller.Index());

        context.Verify(
            x => x.TryRetrieve(out data),
            Times.Once);

        await AssertMetadataUnchanged();
        VerifyNoDownstreamCalls();
    }

    private static IEnumerable<TestCaseData> FailureCases()
    {
        yield return new TestCaseData(
                new InvalidOperationException(""))
            .SetName(
                "Index_ContextFailure_EmptyMessage_Propagates");

        yield return new TestCaseData(
                new InvalidOperationException("Context unavailable"))
            .SetName(
                "Index_ContextFailure_Propagates");

        yield return new TestCaseData(
                new OperationCanceledException())
            .SetName(
                "Index_ContextCancellation_Propagates");
    }

    [TestCaseSource(nameof(FailureCases))]
    [Category("ExpectedToPass")]
    public async Task Index_ContextException_PropagatesSameInstance(
        Exception expected)
    {
        // Arrange
        WebPageDataContext data = null!;

        context
            .Setup(x => x.TryRetrieve(out data))
            .Throws(expected);

        SeedMetadata();

        // Act
        var actual = Assert.CatchAsync(
            async () => await controller.Index());

        // Assert
        Assert.That(actual, Is.SameAs(expected));

        context.Verify(
            x => x.TryRetrieve(out data),
            Times.Once);

        await AssertMetadataUnchanged();
        VerifyNoDownstreamCalls();
    }

    // ---------------------------------------------------------
    // Image mapping tests
    // ---------------------------------------------------------

    [Test]
    [Category("DocumentsNullSafetyGap")]
    public void ImageMapping_NullMedia_CurrentlyThrows()
    {
        // Arrange / Act / Assert
        Assert.Throws<NullReferenceException>(
            () => ImageAssetViewModel.Create(null!));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" \t")]
    [TestCase("Hero image")]
    [Category("DocumentsNullSafetyGap")]
    public void ImageMapping_MissingSystemFields_CurrentlyThrows(
        string? title)
    {
        // Arrange
        var media = new MediaFile
        {
            SystemFields = null!,
            LegacyMediaFileAsset = null!,
            LegacyMediaFileTitle = title!,
            LegacyMediaFileDescription = null!
        };

        // Act / Assert
        Assert.Throws<NullReferenceException>(
            () => ImageAssetViewModel.Create(media));
    }

    // ---------------------------------------------------------
    // Gallery null-safety requirements
    //
    // These test the real public helper. Its implementation
    // was not supplied, so current pass/fail is unverified.
    // ---------------------------------------------------------

    [Test]
    [Category("NullSafetyRequirement")]
    public void Gallery_NullFolders_ShouldReturnEmptyCollection()
    {
        // Arrange / Act
        var result = HeroImageHelper.ExtractGalleryImages(null!);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
    }

    [Test]
    [Category("NullSafetyRequirement")]
    public void Gallery_EmptyFolders_ShouldReturnEmptyCollection()
    {
        // Arrange / Act
        var result = HeroImageHelper.ExtractGalleryImages([]);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
    }

    // ---------------------------------------------------------
    // Real metadata service tests
    // ---------------------------------------------------------

    [TestCase(null, "", "", null)]
    [TestCase("", "", "", "")]
    [TestCase(" \t", "", "", " \t")]
    [TestCase("Travel", "Travel", "Travel", "Travel")]
    [Category("ExpectedToPass")]
    [Category("MetadataService")]
    public async Task Metadata_NullOrBlankFields_UsesActualFallbacks(
        string? input,
        string expectedTitle,
        string expectedDescription,
        string? expectedKeywords)
    {
        // Arrange: use the supplied concrete metadata content type.
        var page = new Container
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

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" \t")]
    [Category("ExpectedToPass")]
    [Category("MetadataService")]
    public async Task Metadata_MissingImage_ClearsPreviousImage(
        string? replacementImage)
    {
        // Arrange
        var page = new Container
        {
            MetaDataSchema__PageTitle = "Travel types"
        };

        metadata.SetMeta(page, "/previous.jpg");

        var before = await metadata.GetMeta();

        Assert.That(
            before.ShareImage,
            Is.EqualTo("https://example.com/previous.jpg"));

        // Act
        metadata.SetMeta(page, replacementImage);
        var after = await metadata.GetMeta();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(after.ShareImage, Is.Empty);

            Assert.That(
                after.Title,
                Is.EqualTo("Travel types"));
        });

        VerifyNoDownstreamCalls();
    }

    [Test]
    [Category("DocumentsNullSafetyGap")]
    [Category("MetadataService")]
    public void Metadata_NullSchema_CurrentlyThrows()
    {
        // Arrange / Act / Assert
        Assert.Throws<NullReferenceException>(
            () => metadata.SetMeta(
                (IMetaDataSchema)null!,
                null));

        VerifyNoDownstreamCalls();
    }

    // ---------------------------------------------------------
    // Shared fixture helpers
    // ---------------------------------------------------------

    private void SeedMetadata()
    {
        metadata.SetMeta(
            new WebPageMeta(
                "Existing title",
                "Existing description",
                "existing-keywords"));
    }

    private async Task AssertMetadataUnchanged()
    {
        var result = await metadata.GetMeta();

        Assert.Multiple(() =>
        {
            Assert.That(
                result.Title,
                Is.EqualTo("Existing title"));

            Assert.That(
                result.Description,
                Is.EqualTo("Existing description"));

            Assert.That(
                result.Keywords,
                Is.EqualTo("existing-keywords"));
        });
    }

    private void VerifyNoDownstreamCalls()
    {
        mediator.VerifyNoOtherCalls();
        urls.VerifyNoOtherCalls();
        channel.VerifyNoOtherCalls();
    }
}