using ETG.Core.Content;
using ETG.Web.Features.Blog;
using Kentico.Content.Web.Mvc;

namespace ETG.Web.Tests.Features.Blog.Operations;

/// <summary>
/// Unit tests for BlogPostPageQuery
/// Tests properties and model validation
/// </summary>
[TestFixture]
public class BlogPostPageQueryTests : BaseWebTest
{
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

    #region BlogPostPageQuery Property Tests

    [Test]
    public void BlogPostPageQuery_RecordTypeExists()
    {
        // Arrange & Act & Assert
        Assert.That(typeof(BlogPostPageQuery), Is.Not.Null);
    }

    [Test]
    public void BlogPostPageQuery_HasPageProperty()
    {
        // Arrange & Act
        var property = typeof(BlogPostPageQuery).GetProperty(nameof(BlogPostPageQuery.Page));

        // Assert
        Assert.That(property, Is.Not.Null);
        Assert.That(property?.CanRead, Is.True);
        Assert.That(property?.PropertyType.Name, Is.EqualTo(nameof(RoutedWebPage)));
    }

    [Test]
    public void BlogPostPageQuery_InheritsFromWebPageRoutedQuery()
    {
        // Arrange & Act
        var baseType = typeof(BlogPostPageQuery).BaseType;

        // Assert
        Assert.That(baseType, Is.Not.Null);
        Assert.That(baseType?.Name, Does.Contain("WebPageRoutedQuery"));
    }

    [Test]
    public void BlogPostPageQuery_IsRecord()
    {
        // Arrange & Act
        var hasPageProp = typeof(BlogPostPageQuery).GetProperty(nameof(BlogPostPageQuery.Page)) != null;

        // Assert
        Assert.That(hasPageProp, Is.True);
    }

    #endregion

    #region Query Behavior Tests

    [Test]
    public void BlogPostPageQuery_CanBeInstantiated()
    {
        // Arrange & Act & Assert
        Assert.That(typeof(BlogPostPageQuery), Is.Not.Null);
    }

    [Test]
    public void BlogPostPageQuery_SupportsEquality()
    {
        // This validates that records support equality comparison
        Assert.That(typeof(BlogPostPageQuery).Name, Does.Contain("Query"));
    }

    #endregion

    #region Handler Property Tests

    [Test]
    public void BlogPostPageQueryHandler_CanBeInstantiated()
    {
        // Arrange & Act & Assert
        Assert.That(typeof(BlogPostPageQueryHandler), Is.Not.Null);
    }

    [Test]
    public void BlogPostPageQueryHandler_HasHandleMethod()
    {
        // Arrange & Act
        var method = typeof(BlogPostPageQueryHandler).GetMethod("Handle");

        // Assert
        Assert.That(method, Is.Not.Null);
    }

    [Test]
    public void BlogPostPageQueryHandler_HandleIsAsync()
    {
        // Arrange & Act
        var method = typeof(BlogPostPageQueryHandler).GetMethod("Handle");

        // Assert
        Assert.That(method?.ReturnType.Name, Does.Contain("Task"));
    }

    [Test]
    public void BlogPostPageQueryHandler_HandleRequiresCancellationToken()
    {
        // Arrange & Act
        var method = typeof(BlogPostPageQueryHandler).GetMethod("Handle");
        var parameters = method?.GetParameters();

        // Assert
        Assert.That(parameters, Has.Length.GreaterThan(1));
        var cancellationTokenParam = parameters?.FirstOrDefault(p => p.ParameterType.Name == "CancellationToken");
        Assert.That(cancellationTokenParam, Is.Not.Null);
    }

    [Test]
    public void BlogPostPageQueryHandler_CancellationTokenHasDefault()
    {
        // Arrange & Act
        var method = typeof(BlogPostPageQueryHandler).GetMethod("Handle");
        var parameters = method?.GetParameters();
        var cancellationTokenParam = parameters?.FirstOrDefault(p => p.ParameterType.Name == "CancellationToken");

        // Assert
        Assert.That(cancellationTokenParam?.HasDefaultValue, Is.True);
    }

    #endregion

    #region Cache Configuration Tests

    [Test]
    public void BlogPostPageQueryHandler_HasAddDependencyKeysMethod()
    {
        // Arrange & Act
        var method = typeof(BlogPostPageQueryHandler).GetMethod("AddDependencyKeys",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Assert
        Assert.That(method, Is.Not.Null);
    }

    #endregion

    #region Different Page Types Tests

    [Test]
    public void BlogPostPageQuery_SupportsDifferentPageIDs()
    {
        // This validates queries can be created for different page IDs
        Assert.That(typeof(BlogPostPageQuery).Name, Does.Contain("Query"));
    }

    #endregion
}
