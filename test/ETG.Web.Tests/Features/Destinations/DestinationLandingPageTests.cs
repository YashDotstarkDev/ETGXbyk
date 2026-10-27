using CMS.ContentEngine;
using CMS.Websites;
using ETG.Core.Content;
using ETG.Web.Features.Destinations;
using Kentico.Content.Web.Mvc;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace ETG.Web.Tests.Features.Destinations;

[TestFixture]
public class DestinationLandingPageTests : BaseWebTest
{
    private Mock<IWebPageUrlRetriever> _urlRetriever = null!;
    private Mock<ITaxonomyRetriever> _taxonomyRetriever = null!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();

        _urlRetriever = new Mock<IWebPageUrlRetriever>(
            MockBehavior.Strict);

        _taxonomyRetriever = new Mock<ITaxonomyRetriever>(
            MockBehavior.Strict);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" \t")]
    [TestCase("Explore destinations")]
    [TestCase("日本語-é-مرحبا")]
    [TestCase("<script>alert(1)</script>")]
    public void DetailsCreate_MapsTextWithoutValidation(string? value)
    {
        var container = new Container
        {
            ContainerHeading = value!,
            ContainerHeadingText = value!,
            ContainerHeroImageAltTag = value!,
            ContainerHeroImage = null!
        };

        var model =
            DestinationLandingPageDetailsViewModel.Create(container);

        Assert.Multiple(() =>
        {
            Assert.That(model.Heading, Is.EqualTo(value ?? ""));
            Assert.That(model.HeadingText, Is.EqualTo(value ?? ""));
            Assert.That(model.HeroImageAltText, Is.EqualTo(value ?? ""));
            Assert.That(model.HeroImage, Is.Null);
        });
    }

    [TestCase(true)]
    [TestCase(false)]
    public void DetailsCreate_NoMatchingMedia_ReturnsNullImage(
        bool emptyCollection)
    {
        var container = new Container
        {
            ContainerHeroImage = emptyCollection
                ? Array.Empty<IContentItemFieldsSource>()
                : new IContentItemFieldsSource[] { new Destination() }
        };

        var model =
            DestinationLandingPageDetailsViewModel.Create(container);

        Assert.That(model.HeroImage, Is.Null);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase("{\"@type\":\"CollectionPage\"}")]
    [TestCase("not-json")]
    public async Task CreateAsync_EmptyDestinations_MapsPageAndSchema(
        string? schema)
    {
        var container = new Container
        {
            ContainerHeading = "Destinations",
            ContainerHeadingText = "Explore",
            ContainerHeroImageAltTag = "Hero",
            ContainerHeroImage = null!,
            ContainerJsonSchema = schema!
        };

        var model = await DestinationLandingPageViewModel.CreateAsync(
            container,
            Array.Empty<Destination>(),
            _urlRetriever.Object,
            _taxonomyRetriever.Object,
            languageName: "en-AU");

        Assert.Multiple(() =>
        {
            Assert.That(model.Items, Is.Empty);
            Assert.That(
                model.ContainerJsonSchema,
                Is.EqualTo(schema ?? ""));
            Assert.That(model.Page.Heading, Is.EqualTo("Destinations"));
            Assert.That(model.Page.HeadingText, Is.EqualTo("Explore"));
            Assert.That(model.Page.HeroImageAltText, Is.EqualTo("Hero"));
            Assert.That(model.Page.HeroImage, Is.Null);
        });

        _urlRetriever.VerifyNoOtherCalls();
    }

    [Test]
    public void DetailsCreate_NullContainer_CurrentlyThrows()
    {
        Container container = null!;

        Assert.Throws<NullReferenceException>(() =>
            DestinationLandingPageDetailsViewModel.Create(container));
    }

    [Test]
    public void CreateAsync_NullDestinations_CurrentlyThrows()
    {
        var container = new Container();

        Assert.ThrowsAsync<NullReferenceException>(async () =>
        {
            await DestinationLandingPageViewModel.CreateAsync(
                container,
                null!,
                _urlRetriever.Object,
                _taxonomyRetriever.Object,
                languageName: "en-AU");
        });
    }

    [Test]
    public void DetailsCreate_MediaWithoutSystemFields_CurrentlyThrows()
    {
        var container = new Container
        {
            ContainerHeroImage = new IContentItemFieldsSource[]
            {
                new MediaFile { SystemFields = null! }
            }
        };

        Assert.Throws<NullReferenceException>(() =>
            DestinationLandingPageDetailsViewModel.Create(container));
    }

    [Test]
    [Category("ValidationGap")]
    [Explicit("Proposed requirement: reject invalid JSON schema.")]
    public void CreateAsync_InvalidSchema_ShouldBeRejected()
    {
        var container = new Container
        {
            ContainerJsonSchema = "not-json"
        };

        // Intentionally fails with the current implementation,
        // which copies the schema without validating it.
        Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await DestinationLandingPageViewModel.CreateAsync(
                container,
                Array.Empty<Destination>(),
                _urlRetriever.Object,
                _taxonomyRetriever.Object,
                languageName: "en-AU");
        });
    }
}