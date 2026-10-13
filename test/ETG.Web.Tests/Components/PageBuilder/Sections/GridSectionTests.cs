using ETG.Web.Components.PageBuilder.Sections.Grid;

namespace ETG.Web.Tests.Components.PageBuilder.Sections;

/// <summary>
/// Tests for GridSection - Multi-column grid layout section
/// </summary>
[TestFixture]
public class GridSectionTests : BaseWebTest
{
    private GridSection _section = null!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        _section = new GridSection();
    }

    #region Identifier Tests

    [Test]
    public void Section_HasCorrectIdentifier()
    {
        // Assert
        Assert.That(GridSection.IDENTIFIER, Is.EqualTo("Website.Components.Sections.Grid.Grid-Section"));
    }

    #endregion

    #region Properties Tests

    [Test]
    public void GridSectionProperties_HasDefaultHeading()
    {
        // Arrange & Act
        var properties = new GridSectionProperties();

        // Assert
        Assert.That(properties.Heading, Is.EqualTo(string.Empty));
    }

    [Test]
    public void GridSectionProperties_HasDefaultLayout()
    {
        // Arrange & Act
        var properties = new GridSectionProperties();

        // Assert
        Assert.That(properties.Layout, Is.EqualTo(nameof(Layouts._1_Column)));
    }

    [Test]
    public void GridSectionProperties_HasDefaultPaddingTop()
    {
        // Arrange & Act
        var properties = new GridSectionProperties();

        // Assert
        Assert.That(properties.PaddingTop, Is.EqualTo(nameof(VerticalPaddings.Large)));
    }

    [Test]
    public void GridSectionProperties_HasDefaultPaddingBottom()
    {
        // Arrange & Act
        var properties = new GridSectionProperties();

        // Assert
        Assert.That(properties.PaddingBottom, Is.EqualTo(nameof(VerticalPaddings.Large)));
    }

    [Test]
    public void GridSectionProperties_HasDefaultWidgetZonesCount()
    {
        // Arrange & Act
        var properties = new GridSectionProperties();

        // Assert
        Assert.That(properties.WidgetZonesCount, Is.EqualTo(1));
    }

    [Test]
    public void GridSectionProperties_HasDefaultBackgroundColor()
    {
        // Arrange & Act
        var properties = new GridSectionProperties();

        // Assert
        Assert.That(properties.BackgroundColor, Is.EqualTo(nameof(BackgroundColors.White)));
    }

    [Test]
    public void GridSectionProperties_CanSetHeading()
    {
        // Arrange
        var properties = new GridSectionProperties();
        var expectedHeading = "Featured Section";

        // Act
        properties.Heading = expectedHeading;

        // Assert
        Assert.That(properties.Heading, Is.EqualTo(expectedHeading));
    }

    [Test]
    public void GridSectionProperties_CanSetLayout()
    {
        // Arrange
        var properties = new GridSectionProperties();
        var expectedLayout = nameof(Layouts._2_Columns_50_50);

        // Act
        properties.Layout = expectedLayout;

        // Assert
        Assert.That(properties.Layout, Is.EqualTo(expectedLayout));
    }

    [Test]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(6)]
    public void GridSectionProperties_CanSetWidgetZonesCount(int count)
    {
        // Arrange
        var properties = new GridSectionProperties();

        // Act
        properties.WidgetZonesCount = count;

        // Assert
        Assert.That(properties.WidgetZonesCount, Is.EqualTo(count));
    }

    [Test]
    public void GridSectionProperties_CanSetBackgroundColor()
    {
        // Arrange
        var properties = new GridSectionProperties();
        var expectedColor = nameof(BackgroundColors.Dark);

        // Act
        properties.BackgroundColor = expectedColor;

        // Assert
        Assert.That(properties.BackgroundColor, Is.EqualTo(expectedColor));
    }

    #endregion

    #region Enum Tests

    [Test]
    [TestCase(VerticalPaddings.None, "None")]
    [TestCase(VerticalPaddings.Small, "Small")]
    [TestCase(VerticalPaddings.Medium, "Medium")]
    [TestCase(VerticalPaddings.Large, "Large")]
    public void VerticalPaddings_HasCorrectValues(VerticalPaddings padding, string expectedName)
    {
        // Assert
        Assert.That(padding.ToString(), Is.EqualTo(expectedName));
    }

    [Test]
    [TestCase(BackgroundColors.Light, "Light")]
    [TestCase(BackgroundColors.White, "White")]
    [TestCase(BackgroundColors.Dark, "Dark")]
    [TestCase(BackgroundColors.Secondary_Light, "Secondary_Light")]
    public void BackgroundColors_HasCorrectValues(BackgroundColors color, string expectedName)
    {
        // Assert
        Assert.That(color.ToString(), Is.EqualTo(expectedName));
    }

    #endregion

    #region ViewModel Tests

    [Test]
    public void GridSectionViewModel_InitializesFromProperties()
    {
        // Arrange
        var properties = new GridSectionProperties
        {
            Heading = "Featured",
            Layout = nameof(Layouts._2_Columns_50_50),
            PaddingTop = nameof(VerticalPaddings.Large),
            PaddingBottom = nameof(VerticalPaddings.Medium),
            WidgetZonesCount = 2,
            BackgroundColor = nameof(BackgroundColors.White)
        };

        // Act
        var viewModel = new GridSectionViewModel(properties);

        // Assert
        Assert.That(viewModel.Heading, Is.EqualTo("Featured"));
        Assert.That(viewModel.WidgetZoneCount, Is.EqualTo(2));
    }

    [Test]
    public void GridSectionViewModel_HandlesNullHeading()
    {
        // Arrange
        var properties = new GridSectionProperties { Heading = "" };

        // Act
        var viewModel = new GridSectionViewModel(properties);

        // Assert
        Assert.That(viewModel.Heading, Is.Null);
    }

    #endregion
}
