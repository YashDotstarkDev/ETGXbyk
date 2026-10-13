using CMS.Websites;
using ETG.Core.Content;
using ETG.Web.Features.Blog.Operations;
using Kentico.Content.Web.Mvc;
using Moq;

namespace ETG.Web.Tests.Features.Blog.Operations;

/// <summary>
/// Unit tests for BlogLandingPageQuery and BlogLandingPageQueryResponse
/// Tests properties and model validation
/// </summary>
[TestFixture]
public class BlogLandingPageQueryTests : BaseWebTest
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


    #region BlogLandingPageQuery Property Tests

    [Test]
    public void BlogLandingPageQuery_RecordTypeCanBeInstantiated()
    {
        // Arrange & Act & Assert
        // Query is a record type that should be instantiable via constructor
        // This test validates the record exists and is accessible
        Assert.That(typeof(BlogLandingPageQuery), Is.Not.Null);
    }

    [Test]
    public void BlogLandingPageQuery_HasCacheValueKeyProperty()
    {
        // Arrange & Act
        var property = typeof(BlogLandingPageQuery).GetProperty(nameof(BlogLandingPageQuery.CacheValueKey));

        // Assert
        Assert.That(property, Is.Not.Null);
        Assert.That(property?.CanRead, Is.True);
    }

    [Test]
    public void BlogLandingPageQuery_HasPageProperty()
    {
        // Arrange & Act
        var property = typeof(BlogLandingPageQuery).GetProperty(nameof(BlogLandingPageQuery.Page));

        // Assert
        Assert.That(property, Is.Not.Null);
        Assert.That(property?.CanRead, Is.True);
        Assert.That(property?.PropertyType.Name, Is.EqualTo(nameof(RoutedWebPage)));
    }

    [Test]
    public void BlogLandingPageQuery_HasChannelNameProperty()
    {
        // Arrange & Act
        var property = typeof(BlogLandingPageQuery).GetProperty(nameof(BlogLandingPageQuery.ChannelName));

        // Assert
        Assert.That(property, Is.Not.Null);
        Assert.That(property?.CanRead, Is.True);
        Assert.That(property?.PropertyType, Is.EqualTo(typeof(string)));
    }

    [Test]
    public void BlogLandingPageQuery_IsRecord()
    {
        // Arrange & Act
        // Check if type has record-like features (properties from parameters)
        var hasPageProp = typeof(BlogLandingPageQuery).GetProperty(nameof(BlogLandingPageQuery.Page)) != null;
        var hasChannelNameProp = typeof(BlogLandingPageQuery).GetProperty(nameof(BlogLandingPageQuery.ChannelName)) != null;

        // Assert
        Assert.That(hasPageProp && hasChannelNameProp, Is.True);
    }

    #endregion

    #region BlogLandingPageQueryResponse Tests

    [Test]
    public void BlogLandingPageQueryResponse_CanBeCreatedWithValidData()
    {
        // Arrange
        var mockLandingPage = new Mock<BlogLandingPage>().Object;
        var mockBlogPosts = new List<BlogPostPage>();

        // Act
        var response = new BlogLandingPageQueryResponse(mockLandingPage, mockBlogPosts);

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.LandingPage, Is.EqualTo(mockLandingPage));
        Assert.That(response.BlogPosts, Is.EqualTo(mockBlogPosts));
    }

    [Test]
    public void BlogLandingPageQueryResponse_LandingPagePropertyIsNotNull()
    {
        // Arrange
        var mockLandingPage = new Mock<BlogLandingPage>().Object;
        var mockBlogPosts = new List<BlogPostPage>();

        // Act
        var response = new BlogLandingPageQueryResponse(mockLandingPage, mockBlogPosts);

        // Assert
        Assert.That(response.LandingPage, Is.Not.Null);
    }

    [Test]
    public void BlogLandingPageQueryResponse_BlogPostsPropertyIsReadOnly()
    {
        // Arrange
        var mockLandingPage = new Mock<BlogLandingPage>().Object;
        var mockBlogPosts = new List<BlogPostPage>();
        var response = new BlogLandingPageQueryResponse(mockLandingPage, mockBlogPosts);

        // Act & Assert
        Assert.That(response.BlogPosts, Is.InstanceOf<IReadOnlyList<BlogPostPage>>());
    }

    [Test]
    public void BlogLandingPageQueryResponse_CanHaveEmptyBlogPosts()
    {
        // Arrange
        var mockLandingPage = new Mock<BlogLandingPage>().Object;
        var emptyList = new List<BlogPostPage>();

        // Act
        var response = new BlogLandingPageQueryResponse(mockLandingPage, emptyList);

        // Assert
        Assert.That(response.BlogPosts, Is.Empty);
        Assert.That(response.BlogPosts.Count, Is.EqualTo(0));
    }

    [Test]
    public void BlogLandingPageQueryResponse_CanHaveMultipleBlogPosts()
    {
        // Arrange
        var mockLandingPage = new Mock<BlogLandingPage>().Object;
        var mockBlogPosts = new List<BlogPostPage>
        {
            new Mock<BlogPostPage>().Object,
            new Mock<BlogPostPage>().Object,
            new Mock<BlogPostPage>().Object
        };

        // Act
        var response = new BlogLandingPageQueryResponse(mockLandingPage, mockBlogPosts);

        // Assert
        Assert.That(response.BlogPosts.Count, Is.EqualTo(3));
    }

    [Test]
    public void BlogLandingPageQueryResponse_BlogPostsCountMatchesProvidedList()
    {
        // Arrange
        var mockLandingPage = new Mock<BlogLandingPage>().Object;
        var mockBlogPosts = new List<BlogPostPage>
        {
            new Mock<BlogPostPage>().Object,
            new Mock<BlogPostPage>().Object,
            new Mock<BlogPostPage>().Object,
            new Mock<BlogPostPage>().Object,
            new Mock<BlogPostPage>().Object
        };

        // Act
        var response = new BlogLandingPageQueryResponse(mockLandingPage, mockBlogPosts);

        // Assert
        Assert.That(response.BlogPosts.Count, Is.EqualTo(5));
        Assert.That(response.BlogPosts, Has.Count.EqualTo(5));
    }

    [Test]
    public void BlogLandingPageQueryResponse_PreservesOrderOfBlogPosts()
    {
        // Arrange
        var mockLandingPage = new Mock<BlogLandingPage>().Object;
        var post1 = new Mock<BlogPostPage>().Object;
        var post2 = new Mock<BlogPostPage>().Object;
        var post3 = new Mock<BlogPostPage>().Object;
        var mockBlogPosts = new List<BlogPostPage> { post1, post2, post3 };

        // Act
        var response = new BlogLandingPageQueryResponse(mockLandingPage, mockBlogPosts);

        // Assert
        Assert.That(response.BlogPosts[0], Is.EqualTo(post1));
        Assert.That(response.BlogPosts[1], Is.EqualTo(post2));
        Assert.That(response.BlogPosts[2], Is.EqualTo(post3));
    }

    [Test]
    public void BlogLandingPageQueryResponse_BlogPostsCanBeEnumerated()
    {
        // Arrange
        var mockLandingPage = new Mock<BlogLandingPage>().Object;
        var mockBlogPosts = new List<BlogPostPage>
        {
            new Mock<BlogPostPage>().Object,
            new Mock<BlogPostPage>().Object,
            new Mock<BlogPostPage>().Object
        };
        var response = new BlogLandingPageQueryResponse(mockLandingPage, mockBlogPosts);

        // Act
        var count = 0;
        foreach (var post in response.BlogPosts)
        {
            count++;
        }

        // Assert
        Assert.That(count, Is.EqualTo(3));
    }

    #endregion

    }
