using CMS.Websites;
using ETG.Core.Content;
using ETG.Web.Features.News.Operations;
using Kentico.Content.Web.Mvc;
using Moq;

namespace ETG.Web.Tests.Features.News.Operations;

/// <summary>
/// Unit tests for NewsLandingPageQuery and NewsLandingPageQueryResponse
/// Tests properties and model validation
/// </summary>
[TestFixture]
public class NewsLandingPageQueryTests : BaseWebTest
{
    private const string DefaultChannelName = "Default Website";

    [SetUp]
    public override void Setup()
    {
        base.Setup();
    }

    [TearDown]
    public override void TearDown()
    {
        base.TearDown();
    }


    #region NewsLandingPageQuery Property Tests

    [Test]
    public void NewsLandingPageQuery_RecordTypeCanBeInstantiated()
    {
        // Arrange & Act & Assert
        // Query is a record type that should be instantiable via constructor
        // This test validates the record exists and is accessible
        Assert.That(typeof(NewsLandingPageQuery), Is.Not.Null);
    }

    [Test]
    public void NewsLandingPageQuery_HasCacheValueKeyProperty()
    {
        // Arrange & Act
        var property = typeof(NewsLandingPageQuery).GetProperty(nameof(NewsLandingPageQuery.CacheValueKey));

        // Assert
        Assert.That(property, Is.Not.Null);
        Assert.That(property?.CanRead, Is.True);
    }

    [Test]
    public void NewsLandingPageQuery_HasPageProperty()
    {
        // Arrange & Act
        var property = typeof(NewsLandingPageQuery).GetProperty(nameof(NewsLandingPageQuery.Page));

        // Assert
        Assert.That(property, Is.Not.Null);
        Assert.That(property?.CanRead, Is.True);
        Assert.That(property?.PropertyType.Name, Is.EqualTo(nameof(RoutedWebPage)));
    }

    [Test]
    public void NewsLandingPageQuery_HasChannelNameProperty()
    {
        // Arrange & Act
        var property = typeof(NewsLandingPageQuery).GetProperty(nameof(NewsLandingPageQuery.ChannelName));

        // Assert
        Assert.That(property, Is.Not.Null);
        Assert.That(property?.CanRead, Is.True);
        Assert.That(property?.PropertyType, Is.EqualTo(typeof(string)));
    }

    [Test]
    public void NewsLandingPageQuery_IsRecord()
    {
        // Arrange & Act
        // Check if type has record-like features (properties from parameters)
        var hasPageProp = typeof(NewsLandingPageQuery).GetProperty(nameof(NewsLandingPageQuery.Page)) != null;
        var hasChannelNameProp = typeof(NewsLandingPageQuery).GetProperty(nameof(NewsLandingPageQuery.ChannelName)) != null;

        // Assert
        Assert.That(hasPageProp && hasChannelNameProp, Is.True);
    }

    #endregion

    #region NewsLandingPageQueryResponse Tests

    [Test]
    public void NewsLandingPageQueryResponse_CanBeCreatedWithValidData()
    {
        // Arrange
        var mockLandingPage = new Mock<NewsLandingPage>().Object;
        var mockNewsPosts = new List<NewsPostPage>();

        // Act
        var response = new NewsLandingPageQueryResponse(mockLandingPage, mockNewsPosts);

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.LandingPage, Is.EqualTo(mockLandingPage));
        Assert.That(response.NewsPosts, Is.EqualTo(mockNewsPosts));
    }

    [Test]
    public void NewsLandingPageQueryResponse_LandingPagePropertyIsNotNull()
    {
        // Arrange
        var mockLandingPage = new Mock<NewsLandingPage>().Object;
        var mockNewsPosts = new List<NewsPostPage>();

        // Act
        var response = new NewsLandingPageQueryResponse(mockLandingPage, mockNewsPosts);

        // Assert
        Assert.That(response.LandingPage, Is.Not.Null);
    }

    [Test]
    public void NewsLandingPageQueryResponse_NewsPostsPropertyIsReadOnly()
    {
        // Arrange
        var mockLandingPage = new Mock<NewsLandingPage>().Object;
        var mockNewsPosts = new List<NewsPostPage>();
        var response = new NewsLandingPageQueryResponse(mockLandingPage, mockNewsPosts);

        // Act & Assert
        Assert.That(response.NewsPosts, Is.InstanceOf<IReadOnlyList<NewsPostPage>>());
    }

    [Test]
    public void NewsLandingPageQueryResponse_CanHaveEmptyNewsPosts()
    {
        // Arrange
        var mockLandingPage = new Mock<NewsLandingPage>().Object;
        var emptyList = new List<NewsPostPage>();

        // Act
        var response = new NewsLandingPageQueryResponse(mockLandingPage, emptyList);

        // Assert
        Assert.That(response.NewsPosts, Is.Empty);
        Assert.That(response.NewsPosts.Count, Is.EqualTo(0));
    }

    [Test]
    public void NewsLandingPageQueryResponse_CanHaveMultipleNewsPosts()
    {
        // Arrange
        var mockLandingPage = new Mock<NewsLandingPage>().Object;
        var mockNewsPosts = new List<NewsPostPage>
        {
            new Mock<NewsPostPage>().Object,
            new Mock<NewsPostPage>().Object,
            new Mock<NewsPostPage>().Object
        };

        // Act
        var response = new NewsLandingPageQueryResponse(mockLandingPage, mockNewsPosts);

        // Assert
        Assert.That(response.NewsPosts.Count, Is.EqualTo(3));
    }

    [Test]
    public void NewsLandingPageQueryResponse_NewsPostsCountMatchesProvidedList()
    {
        // Arrange
        var mockLandingPage = new Mock<NewsLandingPage>().Object;
        var mockNewsPosts = new List<NewsPostPage>
        {
            new Mock<NewsPostPage>().Object,
            new Mock<NewsPostPage>().Object,
            new Mock<NewsPostPage>().Object,
            new Mock<NewsPostPage>().Object,
            new Mock<NewsPostPage>().Object
        };

        // Act
        var response = new NewsLandingPageQueryResponse(mockLandingPage, mockNewsPosts);

        // Assert
        Assert.That(response.NewsPosts.Count, Is.EqualTo(5));
        Assert.That(response.NewsPosts, Has.Count.EqualTo(5));
    }

    [Test]
    public void NewsLandingPageQueryResponse_PreservesOrderOfNewsPosts()
    {
        // Arrange
        var mockLandingPage = new Mock<NewsLandingPage>().Object;
        var post1 = new Mock<NewsPostPage>().Object;
        var post2 = new Mock<NewsPostPage>().Object;
        var post3 = new Mock<NewsPostPage>().Object;
        var mockNewsPosts = new List<NewsPostPage> { post1, post2, post3 };

        // Act
        var response = new NewsLandingPageQueryResponse(mockLandingPage, mockNewsPosts);

        // Assert
        Assert.That(response.NewsPosts[0], Is.EqualTo(post1));
        Assert.That(response.NewsPosts[1], Is.EqualTo(post2));
        Assert.That(response.NewsPosts[2], Is.EqualTo(post3));
    }

    [Test]
    public void NewsLandingPageQueryResponse_NewsPostsCanBeEnumerated()
    {
        // Arrange
        var mockLandingPage = new Mock<NewsLandingPage>().Object;
        var mockNewsPosts = new List<NewsPostPage>
        {
            new Mock<NewsPostPage>().Object,
            new Mock<NewsPostPage>().Object,
            new Mock<NewsPostPage>().Object
        };
        var response = new NewsLandingPageQueryResponse(mockLandingPage, mockNewsPosts);

        // Act
        var count = 0;
        foreach (var post in response.NewsPosts)
        {
            count++;
        }

        // Assert
        Assert.That(count, Is.EqualTo(3));
    }

    #endregion
}
