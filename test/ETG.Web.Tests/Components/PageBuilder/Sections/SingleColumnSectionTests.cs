using ETG.Web.Components.PageBuilder.Sections.SingleColumn;

namespace ETG.Web.Tests.Components.PageBuilder.Sections;

/// <summary>
/// Tests for SingleColumnSection - Simple single-column layout section
/// </summary>
[TestFixture]
public class SingleColumnSectionTests : BaseWebTest
{
    private SingleColumnSection _section = null!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        _section = new SingleColumnSection();
    }

    #region Identifier Tests

    [Test]
    public void Section_HasCorrectIdentifier()
    {
        // Assert
        Assert.That(SingleColumnSection.IDENTIFIER, Is.EqualTo("Website.Sections.SingleColumn"));
    }

    #endregion

    #region Properties Tests

    [Test]
    public void SingleColumnSectionProperties_HasDefaultLayout()
    {
        // Arrange & Act
        var properties = new SingleColumnSectionProperties();

        // Assert
        Assert.That(properties.Layout, Is.EqualTo(nameof(Layouts.Standard)));
    }

    [Test]
    public void SingleColumnSectionProperties_HasDefaultAlignment()
    {
        // Arrange & Act
        var properties = new SingleColumnSectionProperties();

        // Assert
        Assert.That(properties.Alignment, Is.EqualTo(nameof(Alignments.Left)));
    }

    [Test]
    public void SingleColumnSectionProperties_HasDefaultPaddingTop()
    {
        // Arrange & Act
        var properties = new SingleColumnSectionProperties();

        // Assert
        Assert.That(properties.PaddingTop, Is.EqualTo(nameof(VerticalPaddings.Large)));
    }

    [Test]
    public void SingleColumnSectionProperties_HasDefaultPaddingBottom()
    {
        // Arrange & Act
        var properties = new SingleColumnSectionProperties();

        // Assert
        Assert.That(properties.PaddingBottom, Is.EqualTo(nameof(VerticalPaddings.Large)));
    }

    [Test]
    public void SingleColumnSectionProperties_HasDefaultBackgroundColor()
    {
        // Arrange & Act
        var properties = new SingleColumnSectionProperties();

        // Assert
        Assert.That(properties.BackgroundColor, Is.EqualTo(nameof(BackgroundColors.White)));
    }

    [Test]
    public void SingleColumnSectionProperties_CanSetLayout()
    {
        // Arrange
        var properties = new SingleColumnSectionProperties();
        var expectedLayout = nameof(Layouts.Standard);

        // Act
        properties.Layout = expectedLayout;

        // Assert
        Assert.That(properties.Layout, Is.EqualTo(expectedLayout));
    }

    [Test]
    public void SingleColumnSectionProperties_CanSetAlignment()
    {
        // Arrange
        var properties = new SingleColumnSectionProperties();
        var expectedAlignment = nameof(Alignments.Center);

        // Act
        properties.Alignment = expectedAlignment;

        // Assert
        Assert.That(properties.Alignment, Is.EqualTo(expectedAlignment));
    }

    [Test]
    public void SingleColumnSectionProperties_CanSetPaddingTop()
    {
        // Arrange
        var properties = new SingleColumnSectionProperties();
        var expectedPadding = nameof(VerticalPaddings.Small);

        // Act
        properties.PaddingTop = expectedPadding;

        // Assert
        Assert.That(properties.PaddingTop, Is.EqualTo(expectedPadding));
    }

    [Test]
    public void SingleColumnSectionProperties_CanSetBackgroundColor()
    {
        // Arrange
        var properties = new SingleColumnSectionProperties();
        var expectedColor = nameof(BackgroundColors.Dark);

        // Act
        properties.BackgroundColor = expectedColor;

        // Assert
        Assert.That(properties.BackgroundColor, Is.EqualTo(expectedColor));
    }

    #endregion

    #region Enum Tests

    [Test]
    [TestCase(Layouts.Standard, "Standard")]
    public void Layouts_HasCorrectValues(Layouts layout, string expectedName)
    {
        // Assert
        Assert.That(layout.ToString(), Is.EqualTo(expectedName));
    }

    [Test]
    [TestCase(Alignments.Left, "Left")]
    [TestCase(Alignments.Center, "Center")]
    public void Alignments_HasCorrectValues(Alignments alignment, string expectedName)
    {
        // Assert
        Assert.That(alignment.ToString(), Is.EqualTo(expectedName));
    }

    #endregion

    #region ViewModel Tests

    [Test]
    public void SingleColumnSectionViewModel_InitializesFromProperties()
    {
        // Arrange
        var properties = new SingleColumnSectionProperties
        {
            Layout = nameof(Layouts.Standard),
            Alignment = nameof(Alignments.Center),
            PaddingTop = nameof(VerticalPaddings.Large),
            PaddingBottom = nameof(VerticalPaddings.Medium),
            BackgroundColor = nameof(BackgroundColors.White)
        };

        // Act
        var viewModel = new SingleColumnSectionViewModel(properties);

        // Assert
        Assert.That(viewModel, Is.Not.Null);
    }

    #endregion
}
