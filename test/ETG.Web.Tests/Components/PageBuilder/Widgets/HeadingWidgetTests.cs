using ETG.Core.Components;
using ETG.Web.Components.PageBuilder.Widgets.Heading;
using Moq;

namespace ETG.Web.Tests.Components.PageBuilder.Widgets;

/// <summary>
/// Tests for HeadingWidget - Heading/Title content widget
/// </summary>
[TestFixture]
public class HeadingWidgetTests : BaseWebTest
{
    private HeadingWidget _widget = null!;
    private Mock<IHeadingContext> _mockHeadingContext = null!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        _mockHeadingContext = new Mock<IHeadingContext>();
        _widget = new HeadingWidget(_mockHeadingContext.Object);
    }

    #region Identifier Tests

    [Test]
    public void Widget_HasCorrectIdentifier()
    {
        // Assert
        Assert.That(HeadingWidget.IDENTIFIER, Is.EqualTo("Website.HeadingWidget"));
    }

    [Test]
    public void Widget_HasCorrectName()
    {
        // Assert
        Assert.That(HeadingWidget.NAME, Is.EqualTo("Heading"));
    }

    #endregion

    #region Properties Tests

    [Test]
    public void HeadingWidgetProperties_HasDefaultText()
    {
        // Arrange & Act
        var properties = new HeadingWidgetProperties();

        // Assert
        Assert.That(properties.HeadingText, Is.EqualTo(string.Empty));
    }

    [Test]
    public void HeadingWidgetProperties_HasDefaultLevel()
    {
        // Arrange & Act
        var properties = new HeadingWidgetProperties();

        // Assert
        Assert.That(properties.HeadingLevel, Is.EqualTo(nameof(HeadingLevels.H2)));
        Assert.That(properties.HeadingLevelsParsed, Is.EqualTo(HeadingLevels.H2));
    }

    [Test]
    public void HeadingWidgetProperties_HasDefaultAlignment()
    {
        // Arrange & Act
        var properties = new HeadingWidgetProperties();

        // Assert
        Assert.That(properties.HeadingAlignment, Is.EqualTo(nameof(HeadingAlignments.Left)));
        Assert.That(properties.HeadingAlignmentsParsed, Is.EqualTo(HeadingAlignments.Left));
    }

    [Test]
    public void HeadingWidgetProperties_ShowHeadingAnchorDefaultsToTrue()
    {
        // Arrange & Act
        var properties = new HeadingWidgetProperties();

        // Assert
        Assert.That(properties.ShowHeadingAnchor, Is.True);
    }

    [Test]
    public void HeadingWidgetProperties_AnchorOverrideDefaultsToEmpty()
    {
        // Arrange & Act
        var properties = new HeadingWidgetProperties();

        // Assert
        Assert.That(properties.AnchorOverride, Is.EqualTo(string.Empty));
    }

    [Test]
    public void HeadingWidgetProperties_CanSetText()
    {
        // Arrange
        var properties = new HeadingWidgetProperties();
        var expectedText = "About Us";

        // Act
        properties.HeadingText = expectedText;

        // Assert
        Assert.That(properties.HeadingText, Is.EqualTo(expectedText));
    }

    [Test]
    [TestCase(nameof(HeadingLevels.H1), HeadingLevels.H1)]
    [TestCase(nameof(HeadingLevels.H2), HeadingLevels.H2)]
    [TestCase(nameof(HeadingLevels.H3), HeadingLevels.H3)]
    [TestCase(nameof(HeadingLevels.H4), HeadingLevels.H4)]
    [TestCase(nameof(HeadingLevels.H5), HeadingLevels.H5)]
    [TestCase(nameof(HeadingLevels.H6), HeadingLevels.H6)]
    public void HeadingWidgetProperties_CanSetAllHeadingLevels(string levelName, HeadingLevels expectedLevel)
    {
        // Arrange
        var properties = new HeadingWidgetProperties();

        // Act
        properties.HeadingLevel = levelName;

        // Assert
        Assert.That(properties.HeadingLevelsParsed, Is.EqualTo(expectedLevel));
    }

    [Test]
    public void HeadingWidgetProperties_CanSetAlignment()
    {
        // Arrange
        var properties = new HeadingWidgetProperties();
        var expectedAlignment = nameof(HeadingAlignments.Center);

        // Act
        properties.HeadingAlignment = expectedAlignment;

        // Assert
        Assert.That(properties.HeadingAlignment, Is.EqualTo(expectedAlignment));
        Assert.That(properties.HeadingAlignmentsParsed, Is.EqualTo(HeadingAlignments.Center));
    }

    [Test]
    public void HeadingWidgetProperties_CanSetAnchorOverride()
    {
        // Arrange
        var properties = new HeadingWidgetProperties();
        var customAnchor = "custom-anchor-id";

        // Act
        properties.AnchorOverride = customAnchor;

        // Assert
        Assert.That(properties.AnchorOverride, Is.EqualTo(customAnchor));
    }

    #endregion

    #region Enum Tests

    [Test]
    [TestCase(HeadingLevels.H1, "H1")]
    [TestCase(HeadingLevels.H2, "H2")]
    [TestCase(HeadingLevels.H3, "H3")]
    [TestCase(HeadingLevels.H4, "H4")]
    [TestCase(HeadingLevels.H5, "H5")]
    [TestCase(HeadingLevels.H6, "H6")]
    public void HeadingLevels_HasCorrectValues(HeadingLevels level, string expectedName)
    {
        // Assert
        Assert.That(level.ToString(), Is.EqualTo(expectedName));
    }

    [Test]
    [TestCase(HeadingAlignments.Left, "Left")]
    [TestCase(HeadingAlignments.Center, "Center")]
    [TestCase(HeadingAlignments.Right, "Right")]
    public void HeadingAlignments_HasCorrectValues(HeadingAlignments alignment, string expectedName)
    {
        // Assert
        Assert.That(alignment.ToString(), Is.EqualTo(expectedName));
    }

    #endregion

    #region ViewModel Tests

    [Test]
    public void HeadingWidgetViewModel_InitializesCorrectly()
    {
        // Arrange
        _mockHeadingContext
            .Setup(x => x.GetUniqueSlug(It.IsAny<string>()))
            .Returns("main-title");

        var properties = new HeadingWidgetProperties
        {
            HeadingText = "Main Title",
            HeadingLevel = nameof(HeadingLevels.H1),
            HeadingAlignment = nameof(HeadingAlignments.Center),
            ShowHeadingAnchor = true,
            AnchorOverride = string.Empty
        };

        // Act
        var viewModel = new HeadingWidgetViewModel(properties, _mockHeadingContext.Object);

        // Assert
        Assert.That(viewModel, Is.Not.Null);
        Assert.That(viewModel.HeadingText, Is.EqualTo("Main Title"));
        Assert.That(viewModel.HeadingLevel, Is.EqualTo(HeadingLevels.H1));
        Assert.That(viewModel.HeadingAlignment, Is.EqualTo(HeadingAlignments.Center));
    }

    [Test]
    public void HeadingWidgetViewModel_WithCustomAnchor()
    {
        // Arrange
        var properties = new HeadingWidgetProperties
        {
            HeadingText = "Main Title",
            HeadingLevel = nameof(HeadingLevels.H1),
            HeadingAlignment = nameof(HeadingAlignments.Center),
            ShowHeadingAnchor = true,
            AnchorOverride = "custom-anchor"
        };

        // Act
        var viewModel = new HeadingWidgetViewModel(properties, _mockHeadingContext.Object);

        // Assert
        Assert.That(viewModel, Is.Not.Null);
        Assert.That(viewModel.HeadingText, Is.EqualTo("Main Title"));
    }

    [Test]
    public void HeadingWidgetViewModel_WithoutAnchor()
    {
        // Arrange
        var properties = new HeadingWidgetProperties
        {
            HeadingText = "Main Title",
            HeadingLevel = nameof(HeadingLevels.H2),
            HeadingAlignment = nameof(HeadingAlignments.Left),
            ShowHeadingAnchor = false,
            AnchorOverride = string.Empty
        };

        // Act
        var viewModel = new HeadingWidgetViewModel(properties, _mockHeadingContext.Object);

        // Assert
        Assert.That(viewModel, Is.Not.Null);
        Assert.That(viewModel.HeadingText, Is.EqualTo("Main Title"));
    }

    #endregion
}
