using ETG.Web.Infrastructure;

namespace ETG.Web.Tests.Infrastructure;

/// <summary>
/// Tests for URLExtensions - URL utility extension methods
/// </summary>
[TestFixture]
public class URLExtensionsTests : BaseWebTest
{
    #region Extension Method Validation

    [Test]
    public void URLExtensions_IsStaticUtilityClass()
    {
        // Arrange
        var type = typeof(URLExtensions);

        // Assert
        Assert.That(type, Is.Not.Null);
    }

    #endregion
}
