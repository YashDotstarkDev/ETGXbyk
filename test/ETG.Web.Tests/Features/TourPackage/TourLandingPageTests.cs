using System;
using System.Collections.Generic;
using ETG.Web.Features.TourPackage;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;

using PageContent = ETG.Core.Content.Page;

namespace ETG.Web.Tests.Features.TourPackage;

[TestFixture]
public class TourLandingPageTests : BaseWebTest
{
    private Mock<IConfiguration> configuration = null!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        configuration = new(MockBehavior.Strict);
    }

    private static IEnumerable<TestCaseData> MappingCases()
    {
        var headings = new (string Name, string? Value)[]
        {
            ("Null", null),
            ("Empty", ""),
            ("Whitespace", " \t"),
            ("Normal", "Find your next tour"),
            ("Padded", " Tours "),
            ("Unexpected", "<script>alert(1)</script>")
        };

        var settings = new (string Name, string? Value)[]
        {
            ("Missing", null),
            ("Empty", ""),
            ("Whitespace", " \t"),
            ("Normal", "configured-value"),
            ("Unexpected", "bad&index=other")
        };

        foreach (var heading in headings)
        {
            foreach (var setting in settings)
            {
                yield return new TestCaseData(
                        heading.Value,
                        setting.Value)
                    .SetName(
                        $"Create_Heading{heading.Name}_" +
                        $"Configuration{setting.Name}");
            }
        }
    }

    [TestCaseSource(nameof(MappingCases))]
    [Category("CurrentBehavior")]
    public void Create_MapsHeadingAndConfiguration(
        string? heading,
        string? configuredValue)
    {
        // Arrange
        var page = new PageContent
        {
            PageHeading = heading!
        };

        SetupConfiguration(
            configuredValue,
            configuredValue,
            configuredValue);

        // Act
        var result = TourLandingViewModel.Create(
            page,
            configuration.Object);

        // Assert: null fallback only; other values are preserved.
        Assert.Multiple(() =>
        {
            Assert.That(
                result.Heading,
                Is.EqualTo(heading ?? ""));

            Assert.That(
                result.ApplicationId,
                Is.EqualTo(configuredValue ?? ""));

            Assert.That(
                result.SearchKey,
                Is.EqualTo(configuredValue ?? ""));

            Assert.That(
                result.TourIndexName,
                Is.EqualTo(configuredValue ?? "TourPricing-Live"));
        });

        VerifyConfigurationReads();
    }

    [TestCase(null, "TourPricing-Live")]
    [TestCase("", "")]
    [TestCase(" ", " ")]
    [TestCase("TourPricing-Test", "TourPricing-Test")]
    [TestCase("bad&index=other", "bad&index=other")]
    [Category("CurrentBehavior")]
    public void Create_IndexFallback_DoesNotReplaceOtherSettings(
        string? index,
        string expectedIndex)
    {
        // Arrange
        var page = new PageContent
        {
            PageHeading = "Tours"
        };

        SetupConfiguration("application-id", "search-key", index);

        // Act
        var result = TourLandingViewModel.Create(
            page,
            configuration.Object);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(
                result.ApplicationId,
                Is.EqualTo("application-id"));

            Assert.That(
                result.SearchKey,
                Is.EqualTo("search-key"));

            Assert.That(
                result.TourIndexName,
                Is.EqualTo(expectedIndex));

            Assert.That(result.Heading, Is.EqualTo("Tours"));
        });

        VerifyConfigurationReads();
    }

    [Test]
    [Category("CurrentBehavior")]
    public void Create_MissingPage_StillMapsServerConfiguration()
    {
        // Arrange
        SetupConfiguration(
            "application-id",
            "search-key",
            "TourPricing-Test");

        // Act
        var result = TourLandingViewModel.Create(
            null!,
            configuration.Object);

        // Assert: missing page is tolerated by this factory.
        Assert.Multiple(() =>
        {
            Assert.That(result.Heading, Is.Empty);

            Assert.That(
                result.ApplicationId,
                Is.EqualTo("application-id"));

            Assert.That(
                result.SearchKey,
                Is.EqualTo("search-key"));

            Assert.That(
                result.TourIndexName,
                Is.EqualTo("TourPricing-Test"));
        });

        VerifyConfigurationReads();
    }

    [TestCase("CMSAlgoliaSearch:ApplicationId")]
    [TestCase("CMSAlgoliaSearch:SearchKey")]
    [TestCase("CMSAlgoliaSearch:SearchIndexTour")]
    [Category("CurrentBehavior")]
    public void Create_ConfigurationException_PropagatesSameInstance(
        string failingKey)
    {
        // Arrange
        SetupConfiguration("application-id", "search-key", "index");

        var expected = new InvalidOperationException(
            "Configuration unavailable");

        configuration
            .Setup(x => x[failingKey])
            .Throws(expected);

        var page = new PageContent
        {
            PageHeading = "Tours"
        };

        // Act
        var actual = Assert.Throws<InvalidOperationException>(
            () => TourLandingViewModel.Create(
                page,
                configuration.Object));

        // Assert
        Assert.That(actual, Is.SameAs(expected));
    }

    [Test]
    [Category("CurrentBehavior")]
    public void Create_NullConfiguration_Throws()
    {
        // Arrange
        var page = new PageContent
        {
            PageHeading = "Tours"
        };

        // Act / Assert
        Assert.Throws<NullReferenceException>(
            () => TourLandingViewModel.Create(page, null!));
    }

    private void SetupConfiguration(
        string? applicationId,
        string? searchKey,
        string? index)
    {
        configuration
            .Setup(x => x["CMSAlgoliaSearch:ApplicationId"])
            .Returns(applicationId);

        configuration
            .Setup(x => x["CMSAlgoliaSearch:SearchKey"])
            .Returns(searchKey);

        configuration
            .Setup(x => x["CMSAlgoliaSearch:SearchIndexTour"])
            .Returns(index);
    }

    private void VerifyConfigurationReads()
    {
        configuration.Verify(
            x => x["CMSAlgoliaSearch:ApplicationId"],
            Times.Once);

        configuration.Verify(
            x => x["CMSAlgoliaSearch:SearchKey"],
            Times.Once);

        configuration.Verify(
            x => x["CMSAlgoliaSearch:SearchIndexTour"],
            Times.Once);

        configuration.VerifyNoOtherCalls();
    }
}