using System.Threading.Tasks;
using ETG.Web.Features.Brochures;
using ETG.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace ETG.Web.Tests.Features.Brochures;

[TestFixture]
public class BrochuresPageTests : BaseWebTest
{
    private Mock<IMediator> _mediator = null!;
    private BrochuresPageTemplateController _controller = null!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();

        _mediator = new Mock<IMediator>();

        var contextRetriever = new Mock<IWebPageDataContextRetriever>();

        var metaService = new WebPageMetaService(
            _mediator.Object,
            new HttpContextAccessor());

        _controller = new BrochuresPageTemplateController(
            _mediator.Object,
            contextRetriever.Object,
            metaService);
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
    public async Task Index_ReturnsNotFound_WhenPageContextIsMissing()
    {
        var result = await _controller.Index();

        Assert.That(result, Is.TypeOf<NotFoundResult>());
        _mediator.VerifyNoOtherCalls();
    }
}

[TestFixture]
public class BrochuresPageViewModelTests : BaseWebTest
{
    [TestCase(null, false, false)]
    [TestCase("", false, false)]
    [TestCase(" \t\r\n", false, false)]
    [TestCase("/brochures/example.pdf", false, true)]
    [TestCase("https://example.com/brochure", false, true)]
    [TestCase("/brochures/example.pdf", true, false)]
    public void CanView_DependsOnUrlAndComingSoon(
        string? url, bool comingSoon, bool expected)
    {
        var card = new BrochureCardViewModel
        {
            ViewUrl = url,
            IsComingSoon = comingSoon
        };

        Assert.That(card.CanView, Is.EqualTo(expected));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" \t")]
    [TestCase("Brochures")]
    [TestCase("<script>alert(1)</script>")]
    public void TextProperties_PreserveAssignedValues(string? value)
    {
        var page = new BrochuresPageViewModel
        {
            Heading = value!,
            IntroHtml = value!
        };

        var card = new BrochureCardViewModel
        {
            Name = value!,
            CoverAlt = value!
        };

        Assert.Multiple(() =>
        {
            Assert.That(page.Heading, Is.EqualTo(value));
            Assert.That(page.IntroHtml, Is.EqualTo(value));
            Assert.That(card.Name, Is.EqualTo(value));
            Assert.That(card.CoverAlt, Is.EqualTo(value));
        });
    }

    [TestCase("not a URL")]
    [TestCase("javascript:alert(1)")]
    [Category("ValidationGap")]
    public void CanView_CurrentlyAcceptsMalformedUrls(string url)
    {
        var card = new BrochureCardViewModel
        {
            ViewUrl = url
        };

        Assert.That(card.CanView, Is.True);
    }

    [TestCase("javascript:alert(1)")]
    [TestCase("data:text/html,<script>alert(1)</script>")]
    [Category("DefectDetection")]
    [Explicit("Proposed requirement: reject unsafe URL schemes.")]
    public void CanView_ShouldRejectUnsafeSchemes(string url)
    {
        var card = new BrochureCardViewModel
        {
            ViewUrl = url
        };

        Assert.That(card.CanView, Is.False);
    }
}