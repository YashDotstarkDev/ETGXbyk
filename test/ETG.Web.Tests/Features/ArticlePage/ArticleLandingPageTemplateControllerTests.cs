using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Content;
using ETG.Web.Features.Article;
using ETG.Web.Infrastructure;
using ETG.Web.Tests;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;

namespace ETG.Web.Tests.Features.ArticlePage;

[TestFixture]
public class ArticleLandingPageTemplateControllerTests : BaseWebTest
{
    private Mock<IMediator> _mediator = null!;
    private Mock<IContentQueryExecutor> _executor = null!;
    private HttpContextAccessor _accessor = null!;
    private WebPageMetaService _metaService = null!;
    private ArticleLandingPageTemplateController _controller = null!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();

        _mediator = new Mock<IMediator>(MockBehavior.Strict);
        _executor = new Mock<IContentQueryExecutor>(MockBehavior.Strict);

        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("example.com");

        _accessor = new HttpContextAccessor
        {
            HttpContext = context
        };

        _metaService = new WebPageMetaService(
            _mediator.Object,
            _accessor);

        // The unconfigured retriever returns false from TryRetrieve.
        var pageContext = new Mock<IWebPageDataContextRetriever>();

        _controller = new ArticleLandingPageTemplateController(
            _mediator.Object,
            pageContext.Object,
            Mock.Of<IWebPageUrlRetriever>(),
            Mock.Of<IWebsiteChannelContext>(),
            Mock.Of<ITaxonomyRetriever>(),
            _executor.Object,
            new ConfigurationBuilder().Build(),
            _metaService);
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
    public async Task Index_MissingPageContext_Returns404WithoutQueries()
    {
        // Arrange:
        // Setup supplies a retriever with no current page.

        // Act
        var result = await _controller.Index();

        // Assert
        Assert.That(result, Is.TypeOf<NotFoundResult>());
        Assert.That(
            ((NotFoundResult)result).StatusCode,
            Is.EqualTo(404));

        // No mediator or content query should run after the early return.
        _mediator.VerifyNoOtherCalls();
        _executor.VerifyNoOtherCalls();
    }

    [TestCaseSource(typeof(ArticleShareImageCases))]
    public async Task Metadata_ResolvesShareImageAccordingToCurrentRules(
        string? imageUrl,
        bool hasHttpContext,
        string expectedUrl)
    {
        // Arrange
        if (!hasHttpContext)
        {
            _accessor.HttpContext = null;
        }

        var page = new Page
        {
            MetaDataSchema__PageTitle = "Articles",
            MetaDataSchema__PageDescription = "Travel articles",
            MetaDataSchema__PageKeywords = "travel"
        };

        // Act
        // Uses the same overload called by the controller.
        _metaService.SetMeta(page, imageUrl);

        var metadata = await _metaService.GetMeta();

        // Assert
        Assert.That(
            metadata.ShareImage,
            Is.EqualTo(expectedUrl));
    }
}

// Non-static dynamic case provider.
public sealed class ArticleShareImageCases : IEnumerable<TestCaseData>
{
    public IEnumerator<TestCaseData> GetEnumerator()
    {
        foreach (var value in new string?[] { null, "", " \t" })
        {
            yield return new TestCaseData(value, true, "");
        }

        yield return new TestCaseData(
            "images/hero.jpg",
            true,
            "https://example.com/images/hero.jpg");

        yield return new TestCaseData(
            "~/images/hero.jpg",
            true,
            "https://example.com/images/hero.jpg");

        yield return new TestCaseData(
            "https://cdn.example.com/hero.jpg",
            true,
            "https://cdn.example.com/hero.jpg");

        yield return new TestCaseData(
            "//cdn.example.com/hero.jpg",
            true,
            "https://cdn.example.com/hero.jpg");

        foreach (var value in new[]
        {
            "javascript:alert(1)",
            "data:text/html,test"
        })
        {
            yield return new TestCaseData(value, true, "");
        }

        // Relative images cannot be resolved without an HTTP context.
        yield return new TestCaseData(
            "images/hero.jpg",
            false,
            "");

        // Absolute HTTP(S) images do not require an HTTP context.
        yield return new TestCaseData(
            "https://cdn.example.com/hero.jpg",
            false,
            "https://cdn.example.com/hero.jpg");

        // Protocol-relative URLs default to HTTPS without a context.
        yield return new TestCaseData(
            "//cdn.example.com/hero.jpg",
            false,
            "https://cdn.example.com/hero.jpg");
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}