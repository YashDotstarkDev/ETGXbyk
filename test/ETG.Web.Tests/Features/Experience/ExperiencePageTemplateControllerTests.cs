using System;
using System.Threading.Tasks;
using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Content;
using ETG.Web.Features.Experience;
using ETG.Web.Features.Search;
using ETG.Web.Infrastructure;
using ETG.Web.Services.GoogleReviews;
using ETG.Web.Services.Tour;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;

namespace ETG.Web.Tests.Features.Experience;

[TestFixture]
public class ExperienceTests : BaseWebTest
{
    private Mock<IMediator> mediator = null!;
    private Mock<IWebPageDataContextRetriever> context = null!;
    private Mock<IContentQueryExecutor> executor = null!;
    private Mock<IWebPageUrlRetriever> urls = null!;
    private Mock<IGoogleReviewsService> reviews = null!;
    private Mock<ITourPricingService> pricing = null!;
    private Mock<ITourSummaryService> summary = null!;

    private ExperiencePageTemplateController controller = null!;
    private HttpContextAccessor accessor = null!;
    private WebPageMetaService metadata = null!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();

        mediator = new(MockBehavior.Strict);
        context = new(MockBehavior.Strict);
        executor = new(MockBehavior.Strict);
        urls = new(MockBehavior.Strict);
        reviews = new(MockBehavior.Strict);
        pricing = new(MockBehavior.Strict);
        summary = new(MockBehavior.Strict);

        accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        };

        accessor.HttpContext.Request.Scheme = "https";
        accessor.HttpContext.Request.Host = new HostString("example.com");

        metadata = new WebPageMetaService(mediator.Object, accessor);

        controller = new ExperiencePageTemplateController(
            mediator.Object,
            context.Object,
            Mock.Of<IWebsiteChannelContext>(),
            executor.Object,
            urls.Object,
            Mock.Of<ITaxonomyRetriever>(),
            reviews.Object,
            pricing.Object,
            summary.Object,
            metadata,
            Mock.Of<IConfiguration>());
    }

    [TearDown]
    public void TearDown() => controller?.Dispose();

    [Test]
    public async Task Index_ContextNotRetrieved_Returns404()
    {
        WebPageDataContext data = null!;
        context.Setup(x => x.TryRetrieve(out data)).Returns(false);

        Assert.That(
            await controller.Index(),
            Is.TypeOf<NotFoundResult>());

        context.Verify(x => x.TryRetrieve(out data), Times.Once);
        VerifyNoDownstreamCalls();
    }

    [Test]
    public void Index_RetrievedNullContext_CurrentlyThrows()
    {
        // Documents the original controller, which has no null-data guard.
        WebPageDataContext data = null!;
        context.Setup(x => x.TryRetrieve(out data)).Returns(true);

        Assert.ThrowsAsync<NullReferenceException>(
            async () => await controller.Index());

        context.Verify(x => x.TryRetrieve(out data), Times.Once);
        VerifyNoDownstreamCalls();
    }

    [TestCase("")]
    [TestCase("Context unavailable")]
    public void Index_ContextFailure_PropagatesOriginalException(string message)
    {
        WebPageDataContext data = null!;
        var expected = new InvalidOperationException(message);

        context.Setup(x => x.TryRetrieve(out data)).Throws(expected);

        var actual = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await controller.Index());

        Assert.That(actual, Is.SameAs(expected));
        VerifyNoDownstreamCalls();
    }

    [TestCase(null, null)]
    [TestCase("", null)]
    [TestCase(" \t", null)]
    [TestCase("Adventure", "Adventure")]
    [TestCase(" Adventure ", "Adventure")]
    [TestCase("Food & Wine", "Food%20%26%20Wine")]
    [TestCase("<script>", "%3Cscript%3E")]
    [TestCase("a/b?x=1", "a%2Fb%3Fx%3D1")]
    [TestCase("A+B", "A%2BB")]
    [TestCase("100%", "100%25")]
    [TestCase("Café", "Caf%C3%A9")]
    [TestCase("Adventure&admin=true", "Adventure%26admin%3Dtrue")]
    public void SearchUrl_HandlesAndEncodesExperience(
        string? experience,
        string? encoded)
    {
        var expected = encoded == null
            ? "/search"
            : FilterUrl("Tours", encoded);

        Assert.That(
            SearchUrlHelper.GetExperienceFilterUrl("Tours", experience),
            Is.EqualTo(expected));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase("Tours")]
    [TestCase("TourPricing-Live")]
    [TestCase("TourPricing-Test")]
    [TestCase("bad&index=x")]
    public void SearchUrl_IndexIsNotValidated_CurrentBehavior(string? index)
    {
        Assert.That(
            SearchUrlHelper.GetExperienceFilterUrl(index!, "Adventure"),
            Is.EqualTo(FilterUrl(index, "Adventure")));
    }

    [TestCase(null, "TourPricing-Live")]
    [TestCase("", "")]
    [TestCase(" ", " ")]
    [TestCase("Tours", "Tours")]
    public void OriginalConfigurationExpression_OnlyNullUsesFallback(
        string? configured,
        string expectedIndex)
    {
        var configuration = new Mock<IConfiguration>();

        configuration
            .Setup(x => x["AlgoliaSettings:SearchIndexTour"])
            .Returns(configured);

        // Tests the original expression, not execution of the controller.
        var result = SearchUrlHelper.GetExperienceFilterUrl(
            configuration.Object["AlgoliaSettings:SearchIndexTour"]
                ?? "TourPricing-Live",
            "Adventure");

        Assert.That(result, Is.EqualTo(
            FilterUrl(expectedIndex, "Adventure")));
    }

    [TestCase(null, "", true)]
    [TestCase("", "", true)]
    [TestCase(" \t", "", true)]
    [TestCase("/images/hero.jpg",
        "https://example.com/images/hero.jpg", true)]
    [TestCase("~/images/hero.jpg",
        "https://example.com/images/hero.jpg", true)]
    [TestCase("//cdn.example.com/hero.jpg",
        "https://cdn.example.com/hero.jpg", true)]
    [TestCase("https://cdn.example.com/hero.jpg",
        "https://cdn.example.com/hero.jpg", true)]
    [TestCase("javascript:alert(1)", "", true)]
    [TestCase("data:text/plain,hello", "", true)]
    [TestCase("/images/hero.jpg", "", false)]
    [TestCase("https://cdn.example.com/hero.jpg",
        "https://cdn.example.com/hero.jpg", false)]
    public async Task Metadata_MapsTitleAndHandlesImageUrl(
        string? image,
        string expected,
        bool hasRequest)
    {
        if (!hasRequest)
            accessor.HttpContext = null;

        var experience = new ETG.Core.Content.Experience
        {
            MetaDataSchema__PageTitle = "Adventure",
            MetaDataSchema__PageDescription = "Explore",
            MetaDataSchema__PageKeywords = "travel"
        };

        metadata.SetMeta(experience, image);
        var result = await metadata.GetMeta();

        Assert.Multiple(() =>
        {
            Assert.That(result.Title, Is.EqualTo("Adventure"));
            Assert.That(result.ShareImage, Is.EqualTo(expected));
        });
    }

    [TestCase(800, 1000, true, 200)]
    [TestCase(1000, 1000, false, 0)]
    [TestCase(1200, 1000, false, 0)]
    [TestCase(0, 1000, false, 0)]
    [TestCase(0, 0, false, 0)]
    [TestCase(-100, 1000, false, 0)]
    public void PricingResult_CalculatesDiscount(
        int current,
        int original,
        bool hasDiscount,
        int discountAmount)
    {
        var result = new TourPricingResult(current, original);

        Assert.Multiple(() =>
        {
            Assert.That(result.HasDiscount, Is.EqualTo(hasDiscount));
            Assert.That(result.DiscountAmount, Is.EqualTo(discountAmount));
            Assert.That(result.CurrencyCode, Is.EqualTo("AUD"));
            Assert.That(result.CurrencySymbol, Is.EqualTo("$"));
        });
    }

    [Test]
    public void PricingResult_PreservesCustomCurrency()
    {
        var result = new TourPricingResult(800, 1000, "NZD", "NZ$");

        Assert.Multiple(() =>
        {
            Assert.That(result.CurrencyCode, Is.EqualTo("NZD"));
            Assert.That(result.CurrencySymbol, Is.EqualTo("NZ$"));
            Assert.That(result.DiscountAmount, Is.EqualTo(200));
        });
    }

    private static string FilterUrl(string? index, string encodedValue) =>
        $"/search?{index}%5BrefinementList%5D" +
        $"%5Bexperiences%5D%5B0%5D={encodedValue}";

    private void VerifyNoDownstreamCalls()
    {
        mediator.VerifyNoOtherCalls();
        executor.VerifyNoOtherCalls();
        urls.VerifyNoOtherCalls();
        reviews.VerifyNoOtherCalls();
        pricing.VerifyNoOtherCalls();
        summary.VerifyNoOtherCalls();
    }
}