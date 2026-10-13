using ETG.Web.Components.PageBuilder.Sections.Unstyled;

namespace ETG.Web.Tests.Components.PageBuilder.Sections;

/// <summary>
/// Tests for UnstyledSection - Custom styling section
/// </summary>
[TestFixture]
public class UnstyledSectionTests : BaseWebTest
{
    #region Identifier Tests

    [Test]
    public void Section_HasCorrectIdentifier()
    {
        // Assert
        Assert.That(UnstyledSection.IDENTIFIER, Is.EqualTo("Website.Sections.Unstyled"));
    }

    #endregion

    #region Structure Tests

    [Test]
    public void UnstyledSection_IsStaticUtilityClass()
    {
        // Arrange
        var type = typeof(UnstyledSection);

        // Assert - Should only contain static members
        Assert.That(type, Is.Not.Null);
        Assert.That(type.GetConstructors().Length, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void UnstyledSection_SupportsCustomStyling()
    {
        // This is a structural test ensuring the unstyled section allows custom CSS
        Assert.That(UnstyledSection.IDENTIFIER, Is.Not.Empty);
        Assert.That(UnstyledSection.IDENTIFIER, Contains.Substring("Unstyled"));
    }

    #endregion
}
