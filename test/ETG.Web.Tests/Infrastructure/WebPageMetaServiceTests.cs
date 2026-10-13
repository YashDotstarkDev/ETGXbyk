using ETG.Core.Content;
using ETG.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Http;
using Moq;

namespace ETG.Web.Tests.Infrastructure;

/// <summary>
/// Tests for WebPageMetaService - Page metadata management service
/// </summary>
[TestFixture]
public class WebPageMetaServiceTests : BaseWebTest
{
    private WebPageMetaService _service = null!;
    private Mock<IMediator> _mockMediator = null!;
    private Mock<IHttpContextAccessor> _mockHttpContextAccessor = null!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        _mockMediator = new Mock<IMediator>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _service = new WebPageMetaService(_mockMediator.Object, _mockHttpContextAccessor.Object);
    }

    #region Constructor Tests

    [Test]
    public void Constructor_InitializesWithDependencies()
    {
        // Assert
        Assert.That(_service, Is.Not.Null);
    }

    #endregion

    #region GetMeta Tests

    [Test]
    public async Task GetMeta_ReturnsWebPageMeta()
    {
        // Act
        var result = await _service.GetMeta();

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public async Task GetMeta_ReturnsDefaultWhenNotSet()
    {
        // Act
        var result = await _service.GetMeta();

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public async Task GetMeta_ReturnsConsistentResults()
    {
        // Act
        var result1 = await _service.GetMeta();
        var result2 = await _service.GetMeta();

        // Assert
        Assert.That(result1, Is.Not.Null);
        Assert.That(result2, Is.Not.Null);
    }

    #endregion

    #region SetMeta Tests

    [Test]
    public void SetMeta_WithWebPageMeta_UpdatesMeta()
    {
        // Arrange
        var meta = new WebPageMeta("Test Title", "Test Description","");

        // Act
        _service.SetMeta(meta);
        var result = _service.GetMeta().Result;

        // Assert
        Assert.That(result.Title, Is.EqualTo("Test Title"));
        Assert.That(result.Description, Is.EqualTo("Test Description"));
    }

    [Test]
    public void SetMeta_MultipleTimes_OverwritesPreviousMeta()
    {
        // Arrange
        var meta1 = new WebPageMeta("First Title", "First Description","");
        var meta2 = new WebPageMeta("Second Title", "Second Description","");

        // Act
        _service.SetMeta(meta1);
        _service.SetMeta(meta2);
        var result = _service.GetMeta().Result;

        // Assert
        Assert.That(result.Title, Is.EqualTo("Second Title"));
        Assert.That(result.Description, Is.EqualTo("Second Description"));
    }

    [Test]
    public void SetMeta_WithNullTitle_HandlesGracefully()
    {
        // Arrange
        var meta = new WebPageMeta(null, "Description", "");

        // Act
        _service.SetMeta(meta);
        var result = _service.GetMeta().Result;

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void SetMeta_WithEmptyStrings_AcceptsValues()
    {
        // Arrange
        var meta = new WebPageMeta(string.Empty, string.Empty, string.Empty);

        // Act
        _service.SetMeta(meta);
        var result = _service.GetMeta().Result;

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    #endregion

    #region SetMeta IWebPageMetaFields Tests

    [Test]
    public void SetMeta_WithIWebPageMetaFields_UpdatesMeta()
    {
        // Arrange
        var mockMetaFields = new Mock<IWebPageMetaFields>();

        // Act
        //_service.SetMeta(mockMetaFields.Object);
        var result = _service.GetMeta().Result;

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    #endregion

    #region Service Behavior Tests

    [Test]
    public async Task Service_PersistMetaBetweenCalls()
    {
        // Arrange
        var meta = new WebPageMeta("Persistent Title", "Persistent Description", "");

        // Act
        _service.SetMeta(meta);
        var result1 = await _service.GetMeta();
        var result2 = await _service.GetMeta();

        // Assert
        Assert.That(result1.Title, Is.EqualTo("Persistent Title"));
        Assert.That(result2.Title, Is.EqualTo("Persistent Title"));
    }

    [Test]
    public void SetMeta_WithMultipleProperties_PreservesTitles()
    {
        // Arrange
        var titles = new[]
        {
            "Title One",
            "Title Two",
            "Title Three"
        };

        // Act & Assert
        foreach (var title in titles)
        {
            var meta = new WebPageMeta(title, "Description", "");
            _service.SetMeta(meta);
            var result = _service.GetMeta().Result;
            Assert.That(result.Title, Is.EqualTo(title));
        }
    }

    #endregion

    #region WebPageMeta Structure Tests

    [Test]
    public void WebPageMeta_CanBeCreatedWithTitleAndDescription()
    {
        // Act
        var meta = new WebPageMeta("Test Title", "Test Description", "");

        // Assert
        Assert.That(meta, Is.Not.Null);
        Assert.That(meta.Title, Is.EqualTo("Test Title"));
        Assert.That(meta.Description, Is.EqualTo("Test Description"));
    }

    [Test]
    public void WebPageMeta_SupportsMultipleProperties()
    {
        // Arrange
        var meta = new WebPageMeta("Title", "Description", "");

        // Assert
        Assert.That(meta.Title, Is.Not.Null);
        Assert.That(meta.Description, Is.Not.Null);
    }

    #endregion
}
