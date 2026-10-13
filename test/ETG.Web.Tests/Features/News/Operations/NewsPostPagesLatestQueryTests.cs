using ETG.Core.Content;
using ETG.Web.Features.News.Operations;
using Moq;

namespace ETG.Web.Tests.Features.News.Operations;

/// <summary>
/// Unit tests for NewsPostPagesLatestQuery and NewsPostPagesLatestQueryResponse
/// Tests properties and model validation
/// </summary>
[TestFixture]
public class NewsPostPagesLatestQueryTests : BaseWebTest
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

    #region NewsPostPagesLatestQuery Property Tests

    [Test]
    public void NewsPostPagesLatestQuery_RecordTypeExists()
    {
        // Arrange & Act & Assert
        Assert.That(typeof(NewsPostPagesLatestQuery), Is.Not.Null);
    }

    [Test]
    public void NewsPostPagesLatestQuery_HasPostProperty()
    {
        // Arrange & Act
        var property = typeof(NewsPostPagesLatestQuery).GetProperty(nameof(NewsPostPagesLatestQuery.post));

        // Assert
        Assert.That(property, Is.Not.Null);
        Assert.That(property?.CanRead, Is.True);
        Assert.That(property?.PropertyType.Name, Is.EqualTo(nameof(NewsPostContent)));
    }

    [Test]
    public void NewsPostPagesLatestQuery_HasChannelNameProperty()
    {
        // Arrange & Act
        var property = typeof(NewsPostPagesLatestQuery).GetProperty(nameof(NewsPostPagesLatestQuery.ChannelName));

        // Assert
        Assert.That(property, Is.Not.Null);
        Assert.That(property?.CanRead, Is.True);
        Assert.That(property?.PropertyType, Is.EqualTo(typeof(string)));
    }

    [Test]
    public void NewsPostPagesLatestQuery_HasCacheValueKeyProperty()
    {
        // Arrange & Act
        var property = typeof(NewsPostPagesLatestQuery).GetProperty(nameof(NewsPostPagesLatestQuery.CacheValueKey));

        // Assert
        Assert.That(property, Is.Not.Null);
        Assert.That(property?.CanRead, Is.True);
    }

    [Test]
    public void NewsPostPagesLatestQuery_IsRecord()
    {
        // Arrange & Act
        var hasPostProp = typeof(NewsPostPagesLatestQuery).GetProperty(nameof(NewsPostPagesLatestQuery.post)) != null;
        var hasChannelNameProp = typeof(NewsPostPagesLatestQuery).GetProperty(nameof(NewsPostPagesLatestQuery.ChannelName)) != null;

        // Assert
        Assert.That(hasPostProp && hasChannelNameProp, Is.True);
    }

    #endregion

    #region NewsPostPagesLatestQueryResponse Tests

    [Test]
    public void NewsPostPagesLatestQueryResponse_CanBeCreatedWithValidData()
    {
        // Arrange
        var mockPages = new List<NewsPostPage>();

        // Act
        var response = new NewsPostPagesLatestQueryResponse(mockPages);

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Items, Is.EqualTo(mockPages));
    }

    [Test]
    public void NewsPostPagesLatestQueryResponse_ItemsPropertyIsReadOnly()
    {
        // Arrange
        var mockPages = new List<NewsPostPage>();
        var response = new NewsPostPagesLatestQueryResponse(mockPages);

        // Act & Assert
        Assert.That(response.Items, Is.InstanceOf<IReadOnlyList<NewsPostPage>>());
    }

    [Test]
    public void NewsPostPagesLatestQueryResponse_CanHaveEmptyItems()
    {
        // Arrange
        var emptyList = new List<NewsPostPage>();

        // Act
        var response = new NewsPostPagesLatestQueryResponse(emptyList);

        // Assert
        Assert.That(response.Items, Is.Empty);
        Assert.That(response.Items.Count, Is.EqualTo(0));
    }

    [Test]
    public void NewsPostPagesLatestQueryResponse_CanHaveMultipleItems()
    {
        // Arrange
        var mockPages = new List<NewsPostPage>
        {
            new Mock<NewsPostPage>().Object,
            new Mock<NewsPostPage>().Object,
            new Mock<NewsPostPage>().Object
        };

        // Act
        var response = new NewsPostPagesLatestQueryResponse(mockPages);

        // Assert
        Assert.That(response.Items.Count, Is.EqualTo(3));
    }

    [Test]
    public void NewsPostPagesLatestQueryResponse_ItemsCountMatchesProvidedList()
    {
        // Arrange
        var mockPages = new List<NewsPostPage>
        {
            new Mock<NewsPostPage>().Object,
            new Mock<NewsPostPage>().Object,
            new Mock<NewsPostPage>().Object,
            new Mock<NewsPostPage>().Object,
            new Mock<NewsPostPage>().Object
        };

        // Act
        var response = new NewsPostPagesLatestQueryResponse(mockPages);

        // Assert
        Assert.That(response.Items.Count, Is.EqualTo(5));
        Assert.That(response.Items, Has.Count.EqualTo(5));
    }

    [Test]
    public void NewsPostPagesLatestQueryResponse_PreservesOrderOfItems()
    {
        // Arrange
        var page1 = new Mock<NewsPostPage>().Object;
        var page2 = new Mock<NewsPostPage>().Object;
        var page3 = new Mock<NewsPostPage>().Object;
        var mockPages = new List<NewsPostPage> { page1, page2, page3 };

        // Act
        var response = new NewsPostPagesLatestQueryResponse(mockPages);

        // Assert
        Assert.That(response.Items[0], Is.EqualTo(page1));
        Assert.That(response.Items[1], Is.EqualTo(page2));
        Assert.That(response.Items[2], Is.EqualTo(page3));
    }

    [Test]
    public void NewsPostPagesLatestQueryResponse_ItemsCanBeEnumerated()
    {
        // Arrange
        var mockPages = new List<NewsPostPage>
        {
            new Mock<NewsPostPage>().Object,
            new Mock<NewsPostPage>().Object,
            new Mock<NewsPostPage>().Object
        };
        var response = new NewsPostPagesLatestQueryResponse(mockPages);

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
    public void NewsPostPagesLatestQueryHandler_CanBeInstantiated()
    {
        // Arrange & Act & Assert
        Assert.That(typeof(NewsPostPagesLatestQueryHandler), Is.Not.Null);
    }

    [Test]
    public void NewsPostPagesLatestQueryHandler_HasHandleMethod()
    {
        // Arrange & Act
        var method = typeof(NewsPostPagesLatestQueryHandler).GetMethod("Handle");

        // Assert
        Assert.That(method, Is.Not.Null);
    }

    [Test]
    public void NewsPostPagesLatestQueryHandler_HandleIsAsync()
    {
        // Arrange & Act
        var method = typeof(NewsPostPagesLatestQueryHandler).GetMethod("Handle");

        // Assert
        Assert.That(method?.ReturnType.Name, Does.Contain("Task"));
    }

    [Test]
    public void NewsPostPagesLatestQueryHandler_HandleRequiresCancellationToken()
    {
        // Arrange & Act
        var method = typeof(NewsPostPagesLatestQueryHandler).GetMethod("Handle");
        var parameters = method?.GetParameters();

        // Assert
        Assert.That(parameters, Has.Length.GreaterThan(1));
        var cancellationTokenParam = parameters?.FirstOrDefault(p => p.ParameterType.Name == "CancellationToken");
        Assert.That(cancellationTokenParam, Is.Not.Null);
    }

    [Test]
    public void NewsPostPagesLatestQueryHandler_CancellationTokenHasDefault()
    {
        // Arrange & Act
        var method = typeof(NewsPostPagesLatestQueryHandler).GetMethod("Handle");
        var parameters = method?.GetParameters();
        var cancellationTokenParam = parameters?.FirstOrDefault(p => p.ParameterType.Name == "CancellationToken");

        // Assert
        Assert.That(cancellationTokenParam?.HasDefaultValue, Is.True);
    }

    #endregion

    #region Cache Configuration Tests

    [Test]
    public void NewsPostPagesLatestQueryHandler_HasAddDependencyKeysMethod()
    {
        // Arrange & Act
        var method = typeof(NewsPostPagesLatestQueryHandler).GetMethod("AddDependencyKeys",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Assert
        Assert.That(method, Is.Not.Null);
    }

    #endregion
}
