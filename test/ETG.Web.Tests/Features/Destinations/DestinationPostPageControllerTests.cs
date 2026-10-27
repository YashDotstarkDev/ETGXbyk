using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ETG.Core.Content;
using ETG.Core.Operations;
using ETG.Web.Features.DestinationPostPage;
using ETG.Web.Rendering;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace ETG.Web.Tests.Features.Destinations;

[TestFixture]
public class DestinationPostPageControllerTests : BaseWebTest
{
    private Mock<IMediator> _mediator = null!;
    private DestinationPostPageTemplateController _controller = null!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();

        _mediator = new Mock<IMediator>(MockBehavior.Strict);

        var contextRetriever =
            new Mock<IWebPageDataContextRetriever>();

        // No other dependency should be reached when context is missing.
        _controller = new DestinationPostPageTemplateController(
            _mediator.Object,
            contextRetriever.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);
    }

    [TearDown]
    public override void TearDown()
    {
        try
        {
            _controller?.Dispose();
        }
        finally
        {
            base.TearDown();
        }
    }

    [Test]
    public async Task Index_MissingContext_ReturnsNotFound()
    {
        // Arrange: the context mock returns false by default.

        // Act
        var result = await _controller.Index();

        // Assert
        Assert.That(result, Is.TypeOf<NotFoundResult>());
        _mediator.VerifyNoOtherCalls();
    }
}

[TestFixture]
public class DestinationPostPageViewModelTests : BaseWebTest
{
    public static IEnumerable<TestCaseData> TextCases()
    {
        yield return new TestCaseData((object?)null);
        yield return new TestCaseData("");
        yield return new TestCaseData(" \t\r\n");
        yield return new TestCaseData("Japan");
        yield return new TestCaseData("日本語-é-مرحبا");
        yield return new TestCaseData("<script>alert(1)</script>");
    }

    [Test]
    public void Defaults_ReturnExpectedValues()
    {
        var model = new DestinationPostPageViewModel();

        Assert.Multiple(() =>
        {
            Assert.That(model.DestinationName, Is.Empty);
            Assert.That(model.DestinationHeading, Is.Empty);
            Assert.That(model.DestinationSummary, Is.Empty);
            Assert.That(model.DestinationDetail, Is.Empty);
            Assert.That(model.DestinationWhenToVisit, Is.Empty);
            Assert.That(model.DestinationCampaignTitle, Is.Empty);
            Assert.That(model.DestinationHeroImageAltTag, Is.Empty);
            Assert.That(model.DestinationJsonSchema, Is.Empty);
            Assert.That(model.DestinationRelatedArticlesHeader, Is.Empty);

            Assert.That(model.CtaButtonText, Is.Null);
            Assert.That(model.CtaButtonUrl, Is.Null);
            Assert.That(model.DestinationHeroImage, Is.Null);
            Assert.That(model.HeroForegroundImage, Is.Null);
            Assert.That(model.DestinationHeroDisableOverlay, Is.False);

            Assert.That(model.RelatedArticles, Is.Empty);
            Assert.That(model.SelectedAccordionItemFolders, Is.Empty);
            Assert.That(model.SelectedImageFolders, Is.Empty);
            Assert.That(model.SelectedImageTileCtaFolders, Is.Empty);

            Assert.That(model.DestinationStickyModel, Is.Not.Null);
            Assert.That(model.CTA, Is.Not.Null);
            Assert.That(model.Reviews, Is.Null);
        });
    }

    [TestCaseSource(nameof(TextCases))]
    public void TextProperties_PreserveAssignedValues(string? value)
    {
        var model = new DestinationPostPageViewModel
        {
            DestinationName = value!,
            DestinationHeading = value!,
            DestinationSummary = value!,
            DestinationDetail = value!,
            DestinationWhenToVisit = value!,
            DestinationCampaignTitle = value!,
            DestinationHeroImageAltTag = value!,
            DestinationJsonSchema = value!,
            DestinationRelatedArticlesHeader = value!,
            CtaButtonText = value,
            CtaButtonUrl = value
        };

        Assert.Multiple(() =>
        {
            Assert.That(model.DestinationName, Is.EqualTo(value));
            Assert.That(model.DestinationHeading, Is.EqualTo(value));
            Assert.That(model.DestinationSummary, Is.EqualTo(value));
            Assert.That(model.DestinationDetail, Is.EqualTo(value));
            Assert.That(model.DestinationWhenToVisit, Is.EqualTo(value));
            Assert.That(model.DestinationCampaignTitle, Is.EqualTo(value));
            Assert.That(model.DestinationHeroImageAltTag, Is.EqualTo(value));
            Assert.That(model.DestinationJsonSchema, Is.EqualTo(value));
            Assert.That(
                model.DestinationRelatedArticlesHeader,
                Is.EqualTo(value));
            Assert.That(model.CtaButtonText, Is.EqualTo(value));
            Assert.That(model.CtaButtonUrl, Is.EqualTo(value));
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Overlay_PreservesAssignedValue(bool enabled)
    {
        var model = new DestinationPostPageViewModel
        {
            DestinationHeroDisableOverlay = enabled
        };

        Assert.That(
            model.DestinationHeroDisableOverlay,
            Is.EqualTo(enabled));
    }

    [Test]
    public void ExplicitNullRelationships_AreNotNormalized()
    {
        var model = new DestinationPostPageViewModel
        {
            RelatedArticles = null!,
            SelectedAccordionItemFolders = null!,
            SelectedImageFolders = null!,
            SelectedImageTileCtaFolders = null!,
            DestinationStickyModel = null!,
            CTA = null!
        };

        Assert.Multiple(() =>
        {
            Assert.That(model.RelatedArticles, Is.Null);
            Assert.That(model.SelectedAccordionItemFolders, Is.Null);
            Assert.That(model.SelectedImageFolders, Is.Null);
            Assert.That(model.SelectedImageTileCtaFolders, Is.Null);
            Assert.That(model.DestinationStickyModel, Is.Null);
            Assert.That(model.CTA, Is.Null);
        });
    }

    [TestCaseSource(nameof(TextCases))]
    public void StickyModel_PreservesAssignedValues(string? value)
    {
        var model = new DestinationStickyModel
        {
            CampaignDetail1 = value!,
            CampaignDetail2 = value!,
            CampaignDetail3 = value!,
            CountdownLabel = value!,
            EndOfCampaign = value!
        };

        Assert.Multiple(() =>
        {
            Assert.That(model.CampaignDetail1, Is.EqualTo(value));
            Assert.That(model.CampaignDetail2, Is.EqualTo(value));
            Assert.That(model.CampaignDetail3, Is.EqualTo(value));
            Assert.That(model.CountdownLabel, Is.EqualTo(value));
            Assert.That(model.EndOfCampaign, Is.EqualTo(value));
        });
    }

    [TestCase(true)]
    [TestCase(false)]
    public void MediaMapping_NullMediaOrSystemFields_CurrentlyThrows(
        bool nullMedia)
    {
        MediaFile media = nullMedia
            ? null!
            : new MediaFile
            {
                SystemFields = null!
            };

        Assert.Throws<NullReferenceException>(() =>
        {
            ImageAssetViewModel.Create(media);
        });
    }
}

[TestFixture]
public class DestinationPostPageQueryHandlerTests : BaseWebTest
{
    [TestCase(false)]
    [TestCase(true)]
    public void Handle_NullRequest_ThrowsBeforeQueryExecution(
        bool cancelled)
    {
        var tools = new WebPageQueryTools(
            null!, null!, null!, null!);

        var handler = new DestinationPostPageQueryHandler(tools);

        using var cancellation = new CancellationTokenSource();

        if (cancelled)
            cancellation.Cancel();

        Assert.ThrowsAsync<NullReferenceException>(async () =>
        {
            await handler.Handle(null!, cancellation.Token);
        });
    }
}