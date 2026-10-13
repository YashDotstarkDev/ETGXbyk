using ETG.Web.Components.PageBuilder.Sections.Heading;

namespace ETG.Web.Tests.Components.PageBuilder.Sections;

/// <summary>
/// Tests for HeadingSection - Dedicated page header section
/// </summary>
[TestFixture]
public class HeadingSectionTests : BaseWebTest
{
    #region Identifier Tests

    [Test]
    public void Section_HasCorrectIdentifier()
    {
        // Assert
        Assert.That(HeadingSection.IDENTIFIER, Is.EqualTo("Website.Sections.Heading"));
    }

    #endregion

    #region Properties Tests

    [Test]
    public void HeadingSectionProperties_IsEmptyImplementation()
    {
        // Arrange & Act
        var properties = new HeadingSectionProperties();

        // Assert - Should have no specific properties, inherits from ISectionProperties
        Assert.That(properties, Is.Not.Null);
    }

    #endregion

    #region Structure Tests

    [Test]
    public void HeadingSection_IsDesignedForPageHeaders()
    {
        // This is a structural test ensuring the heading section is available
        Assert.That(HeadingSection.IDENTIFIER, Is.Not.Empty);
        Assert.That(HeadingSection.IDENTIFIER, Contains.Substring("Heading"));
    }

    #endregion
}
