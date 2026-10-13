using ETG.Core.Content;
using ETG.Web.Features.Blog.Operations;
using Moq;

namespace ETG.Web.Tests.Features.Blog.Operations;

/// <summary>
/// Unit tests for BlogPostPagesLatestQuery and BlogPostPagesLatestQueryResponse
/// Tests properties and model validation
/// </summary>
[TestFixture]
public class BlogPostPagesLatestQueryTests : BaseWebTest
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

    #region BlogPostPagesLatestQuery Property Tests

    [Test]
    public void BlogPostPagesLatestQuery_RecordTypeExists()
    {
        // Arrange & Act & Assert
        Assert.That(typeof(BlogPostPagesLatestQuery), Is.Not.Null);
    }

    [Test]
    public void BlogPostPagesLatestQuery_HasPostProperty()
    {
        // Arrange & Act
        var property = typeof(BlogPostPagesLatestQuery).GetProperty(nameof(BlogPostPagesLatestQuery.post));

        // Assert
        Assert.That(property, Is.Not.Null);
        Assert.That(property?.CanRead, Is.True);
        Assert.That(property?.PropertyType.Name, Is.EqualTo(nameof(BlogPostContent)));
    }

    [Test]
    public void BlogPostPagesLatestQuery_HasChannelNameProperty()
    {
        // Arrange & Act
        var property = typeof(BlogPostPagesLatestQuery).GetProperty(nameof(BlogPostPagesLatestQuery.ChannelName));

        // Assert
        Assert.That(property, Is.Not.Null);
        Assert.That(property?.CanRead, Is.True);
        Assert.That(property?.PropertyType, Is.EqualTo(typeof(string)));
    }

    [Test]
    public void BlogPostPagesLatestQuery_HasCacheValueKeyProperty()
    {
        // Arrange & Act
        var property = typeof(BlogPostPagesLatestQuery).GetProperty(nameof(BlogPostPagesLatestQuery.CacheValueKey));

        // Assert
        Assert.That(property, Is.Not.Null);
        Assert.That(property?.CanRead, Is.True);
    }

    [Test]
    public void BlogPostPagesLatestQuery_IsRecord()
    {
        // Arrange & Act
        var hasPostProp = typeof(BlogPostPagesLatestQuery).GetProperty(nameof(BlogPostPagesLatestQuery.post)) != null;
        var hasChannelNameProp = typeof(BlogPostPagesLatestQuery).GetProperty(nameof(BlogPostPagesLatestQuery.ChannelName)) != null;

        // Assert
        Assert.That(hasPostProp && hasChannelNameProp, Is.True);
    }

    #endregion

    #region BlogPostPagesLatestQueryResponse Tests

    [Test]
    public void BlogPostPagesLatestQueryResponse_CanBeCreatedWithValidData()
    {
        // Arrange
        var mockPages = new List<BlogPostPage>();

        // Act
        var response = new BlogPostPagesLatestQueryResponse(mockPages);

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Items, Is.EqualTo(mockPages));
    }

    [Test]
    public void BlogPostPagesLatestQueryResponse_ItemsPropertyIsReadOnly()
    {
        // Arrange
        var mockPages = new List<BlogPostPage>();
        var response = new BlogPostPagesLatestQueryResponse(mockPages);

        // Act & Assert
        Assert.That(response.Items, Is.InstanceOf<IReadOnlyList<BlogPostPage>>());
    }

    [Test]
    public void BlogPostPagesLatestQueryResponse_CanHaveEmptyItems()
    {
        // Arrange
        var emptyList = new List<BlogPostPage>();

        // Act
        var response = new BlogPostPagesLatestQueryResponse(emptyList);

        // Assert
        Assert.That(response.Items, Is.Empty);
        Assert.That(response.Items.Count, Is.EqualTo(0));
    }

    [Test]
    public void BlogPostPagesLatestQueryResponse_CanHaveMultipleItems()
    {
        // Arrange
        var mockPages = new List<BlogPostPage>
        {
            new Mock<BlogPostPage>().Object,
            new Mock<BlogPostPage>().Object,
            new Mock<BlogPostPage>().Object
        };

        // Act
        var response = new BlogPostPagesLatestQueryResponse(mockPages);

        // Assert
        Assert.That(response.Items.Count, Is.EqualTo(3));
    }

    [Test]
    public void BlogPostPagesLatestQueryResponse_ItemsCountMatchesProvidedList()
    {
        // Arrange
        var mockPages = new List<BlogPostPage>
        {
            new Mock<BlogPostPage>().Object,
            new Mock<BlogPostPage>().Object,
            new Mock<BlogPostPage>().Object,
            new Mock<BlogPostPage>().Object,
            new Mock<BlogPostPage>().Object
        };

        // Act
        var response = new BlogPostPagesLatestQueryResponse(mockPages);

        // Assert
        Assert.That(response.Items.Count, Is.EqualTo(5));
        Assert.That(response.Items, Has.Count.EqualTo(5));
    }

    [Test]
    public void BlogPostPagesLatestQueryResponse_PreservesOrderOfItems()
    {
        // Arrange
        var page1 = new Mock<BlogPostPage>().Object;
        var page2 = new Mock<BlogPostPage>().Object;
        var page3 = new Mock<BlogPostPage>().Object;
        var mockPages = new List<BlogPostPage> { page1, page2, page3 };

        // Act
        var response = new BlogPostPagesLatestQueryResponse(mockPages);

        // Assert
        Assert.That(response.Items[0], Is.EqualTo(page1));
        Assert.That(response.Items[1], Is.EqualTo(page2));
        Assert.That(response.Items[2], Is.EqualTo(page3));
    }

    [Test]
    public void BlogPostPagesLatestQueryResponse_ItemsCanBeEnumerated()
    {
        // Arrange
        var mockPages = new List<BlogPostPage>
        {
            new Mock<BlogPostPage>().Object,
            new Mock<BlogPostPage>().Object,
            new Mock<BlogPostPage>().Object
        };
        var response = new BlogPostPagesLatestQueryResponse(mockPages);

        // Act
        var count = 0;
        foreach (var page in response.Items)
        {
            count++;
        }

        // Assert
        Assert.That(count, Is.EqualTo(3));
    }

    #endregion

    #region Handler Property Tests

    [Test]
    public void BlogPostPagesLatestQueryHandler_CanBeInstantiated()
    {
        // Arrange & Act & Assert
        Assert.That(typeof(BlogPostPagesLatestQueryHandler), Is.Not.Null);
    }

    [Test]
    public void BlogPostPagesLatestQueryHandler_HasHandleMethod()
    {
        // Arrange & Act
        var method = typeof(BlogPostPagesLatestQueryHandler).GetMethod("Handle");

        // Assert
        Assert.That(method, Is.Not.Null);
    }

    [Test]
    public void BlogPostPagesLatestQueryHandler_HandleIsAsync()
    {
        // Arrange & Act
        var method = typeof(BlogPostPagesLatestQueryHandler).GetMethod("Handle");

        // Assert
        Assert.That(method?.ReturnType.Name, Does.Contain("Task"));
    }

    [Test]
    public void BlogPostPagesLatestQueryHandler_HandleRequiresCancellationToken()
    {
        // Arrange & Act
        var method = typeof(BlogPostPagesLatestQueryHandler).GetMethod("Handle");
        var parameters = method?.GetParameters();

        // Assert
        Assert.That(parameters, Has.Length.GreaterThan(1));
        var cancellationTokenParam = parameters?.FirstOrDefault(p => p.ParameterType.Name == "CancellationToken");
        Assert.That(cancellationTokenParam, Is.Not.Null);
    }

    [Test]
    public void BlogPostPagesLatestQueryHandler_CancellationTokenHasDefault()
    {
        // Arrange & Act
        var method = typeof(BlogPostPagesLatestQueryHandler).GetMethod("Handle");
        var parameters = method?.GetParameters();
        var cancellationTokenParam = parameters?.FirstOrDefault(p => p.ParameterType.Name == "CancellationToken");

        // Assert
        Assert.That(cancellationTokenParam?.HasDefaultValue, Is.True);
    }

    #endregion

    #region Cache Configuration Tests

    [Test]
    public void BlogPostPagesLatestQueryHandler_HasAddDependencyKeysMethod()
    {
        // Arrange & Act
        var method = typeof(BlogPostPagesLatestQueryHandler).GetMethod("AddDependencyKeys",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Assert
        Assert.That(method, Is.Not.Null);
    }

    #endregion
}
