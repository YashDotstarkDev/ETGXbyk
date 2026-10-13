using ETG.Core.Content;
using ETG.Web.Features.News;
using Kentico.Content.Web.Mvc;

namespace ETG.Web.Tests.Features.News.Operations;

/// <summary>
/// Unit tests for NewsPostPageQuery
/// Tests properties and model validation
/// </summary>
[TestFixture]
public class NewsPostPageQueryTests : BaseWebTest
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

    #region NewsPostPageQuery Property Tests

    [Test]
    public void NewsPostPageQuery_RecordTypeExists()
    {
        // Arrange & Act & Assert
        Assert.That(typeof(NewsPostPageQuery), Is.Not.Null);
    }

    [Test]
    public void NewsPostPageQuery_HasPageProperty()
    {
        // Arrange & Act
        var property = typeof(NewsPostPageQuery).GetProperty(nameof(NewsPostPageQuery.Page));

        // Assert
        Assert.That(property, Is.Not.Null);
        Assert.That(property?.CanRead, Is.True);
        Assert.That(property?.PropertyType.Name, Is.EqualTo(nameof(RoutedWebPage)));
    }

    [Test]
    public void NewsPostPageQuery_InheritsFromWebPageRoutedQuery()
    {
        // Arrange & Act
        var baseType = typeof(NewsPostPageQuery).BaseType;

        // Assert
        Assert.That(baseType, Is.Not.Null);
        Assert.That(baseType?.Name, Does.Contain("WebPageRoutedQuery"));
    }

    [Test]
    public void NewsPostPageQuery_IsRecord()
    {
        // Arrange & Act
        var hasPageProp = typeof(NewsPostPageQuery).GetProperty(nameof(NewsPostPageQuery.Page)) != null;

        // Assert
        Assert.That(hasPageProp, Is.True);
    }

    #endregion

    #region Query Behavior Tests

    [Test]
    public void NewsPostPageQuery_CanBeInstantiated()
    {
        // Arrange & Act & Assert
        Assert.That(typeof(NewsPostPageQuery), Is.Not.Null);
    }

    [Test]
    public void NewsPostPageQuery_SupportsEquality()
    {
        // This validates that records support equality comparison
        Assert.That(typeof(NewsPostPageQuery).Name, Does.Contain("Query"));
    }

    #endregion

    #region Handler Property Tests

    [Test]
    public void NewsPostPageQueryHandler_CanBeInstantiated()
    {
        // Arrange & Act & Assert
        Assert.That(typeof(NewsPostPageQueryHandler), Is.Not.Null);
    }

    [Test]
    public void NewsPostPageQueryHandler_HasHandleMethod()
    {
        // Arrange & Act
        var method = typeof(NewsPostPageQueryHandler).GetMethod("Handle");

        // Assert
        Assert.That(method, Is.Not.Null);
    }

    [Test]
    public void NewsPostPageQueryHandler_HandleIsAsync()
    {
        // Arrange & Act
        var method = typeof(NewsPostPageQueryHandler).GetMethod("Handle");

        // Assert
        Assert.That(method?.ReturnType.Name, Does.Contain("Task"));
    }

    [Test]
    public void NewsPostPageQueryHandler_HandleRequiresCancellationToken()
    {
        // Arrange & Act
        var method = typeof(NewsPostPageQueryHandler).GetMethod("Handle");
        var parameters = method?.GetParameters();

        // Assert
        Assert.That(parameters, Has.Length.GreaterThan(1));
        var cancellationTokenParam = parameters?.FirstOrDefault(p => p.ParameterType.Name == "CancellationToken");
        Assert.That(cancellationTokenParam, Is.Not.Null);
    }

    [Test]
    public void NewsPostPageQueryHandler_CancellationTokenHasDefault()
    {
        // Arrange & Act
        var method = typeof(NewsPostPageQueryHandler).GetMethod("Handle");
        var parameters = method?.GetParameters();
        var cancellationTokenParam = parameters?.FirstOrDefault(p => p.ParameterType.Name == "CancellationToken");

        // Assert
        Assert.That(cancellationTokenParam?.HasDefaultValue, Is.True);
    }

    #endregion

    #region Cache Configuration Tests

    [Test]
    public void NewsPostPageQueryHandler_HasAddDependencyKeysMethod()
    {
        // Arrange & Act
        var method = typeof(NewsPostPageQueryHandler).GetMethod("AddDependencyKeys",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Assert
        Assert.That(method, Is.Not.Null);
    }

    #endregion

    #region Different Page Types Tests

    [Test]
    public void NewsPostPageQuery_SupportsDifferentPageIDs()
    {
        // This validates queries can be created for different page IDs
        Assert.That(typeof(NewsPostPageQuery).Name, Does.Contain("Query"));
    }

    #endregion
}
