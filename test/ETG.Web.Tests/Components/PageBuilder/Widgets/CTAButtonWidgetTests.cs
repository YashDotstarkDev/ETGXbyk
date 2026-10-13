using ETG.Core.Components;
using ETG.Web.Components.PageBuilder.Widgets.CTAButton;

namespace ETG.Web.Tests.Components.PageBuilder.Widgets;

/// <summary>
/// Tests for CTAButtonWidget - Call to Action Button Widget
/// </summary>
[TestFixture]
public class CTAButtonWidgetTests : BaseWebTest
{
    private CTAButtonWidget _widget = null!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        _widget = new CTAButtonWidget();
    }

    #region Identifier Tests

    [Test]
    public void Widget_HasCorrectIdentifier()
    {
        // Assert
        Assert.That(CTAButtonWidget.IDENTIFIER, Is.EqualTo("Website.CtaButtonWidget"));
    }

    #endregion

    #region Properties Tests

    [Test]
    public void CTAButtonWidgetProperties_HasDefaultText()
    {
        // Arrange & Act
        var properties = new CTAButtonWidgetProperties();

        // Assert
        Assert.That(properties.Text, Is.EqualTo(string.Empty));
    }

    [Test]
    public void CTAButtonWidgetProperties_HasDefaultLinkURL()
    {
        // Arrange & Act
        var properties = new CTAButtonWidgetProperties();

        // Assert
        Assert.That(properties.LinkURL, Is.EqualTo(string.Empty));
    }

    [Test]
    public void CTAButtonWidgetProperties_HasDefaultAlignment()
    {
        // Arrange & Act
        var properties = new CTAButtonWidgetProperties();

        // Assert
        Assert.That(properties.HorizontalAlignment, Is.EqualTo(nameof(HorizontalAlignments.Left)));
        Assert.That(properties.HorizontalAlignmentParsed, Is.EqualTo(HorizontalAlignments.Left));
    }

    [Test]
    public void CTAButtonWidgetProperties_HasDefaultTopMargin()
    {
        // Arrange & Act
        var properties = new CTAButtonWidgetProperties();

        // Assert
        Assert.That(properties.TopMargin, Is.EqualTo(nameof(MarginSizes.Medium)));
        Assert.That(properties.TopMarginParsed, Is.EqualTo(MarginSizes.Medium));
    }

    [Test]
    public void CTAButtonWidgetProperties_HasDefaultBottomMargin()
    {
        // Arrange & Act
        var properties = new CTAButtonWidgetProperties();

        // Assert
        Assert.That(properties.BottomMargin, Is.EqualTo(nameof(MarginSizes.Medium)));
        Assert.That(properties.BottomMarginParsed, Is.EqualTo(MarginSizes.Medium));
    }

    [Test]
    public void CTAButtonWidgetProperties_CanSetText()
    {
        // Arrange
        var properties = new CTAButtonWidgetProperties();
        var expectedText = "Learn More";

        // Act
        properties.Text = expectedText;

        // Assert
        Assert.That(properties.Text, Is.EqualTo(expectedText));
    }

    [Test]
    public void CTAButtonWidgetProperties_CanSetLinkURL()
    {
        // Arrange
        var properties = new CTAButtonWidgetProperties();
        var expectedUrl = "https://example.com/contact";

        // Act
        properties.LinkURL = expectedUrl;

        // Assert
        Assert.That(properties.LinkURL, Is.EqualTo(expectedUrl));
    }

    [Test]
    public void CTAButtonWidgetProperties_CanSetAlignment()
    {
        // Arrange
        var properties = new CTAButtonWidgetProperties();
        var expectedAlignment = nameof(HorizontalAlignments.Center);

        // Act
        properties.HorizontalAlignment = expectedAlignment;

        // Assert
        Assert.That(properties.HorizontalAlignment, Is.EqualTo(expectedAlignment));
        Assert.That(properties.HorizontalAlignmentParsed, Is.EqualTo(HorizontalAlignments.Center));
    }

    #endregion

    #region Enum Tests

    [Test]
    [TestCase(HorizontalAlignments.Left, "Left")]
    [TestCase(HorizontalAlignments.Center, "Center")]
    public void HorizontalAlignments_HasCorrectValues(HorizontalAlignments alignment, string expectedName)
    {
        // Assert
        Assert.That(alignment.ToString(), Is.EqualTo(expectedName));
    }

    [Test]
    [TestCase(MarginSizes.None, "None")]
    [TestCase(MarginSizes.Small, "Small")]
    [TestCase(MarginSizes.Medium, "Medium")]
    [TestCase(MarginSizes.Large, "Large")]
    public void MarginSizes_HasCorrectValues(MarginSizes margin, string expectedName)
    {
        // Assert
        Assert.That(margin.ToString(), Is.EqualTo(expectedName));
    }

    #endregion

    #region ViewModel Tests

    [Test]
    public void CTAButtonWidgetViewModel_InitializesFromProperties()
    {
        // Arrange
        var properties = new CTAButtonWidgetProperties
        {
            Text = "Click Me",
            LinkURL = "https://example.com",
            HorizontalAlignment = nameof(HorizontalAlignments.Center),
            TopMargin = nameof(MarginSizes.Large),
            BottomMargin = nameof(MarginSizes.Small)
        };

        // Act
        var viewModel = new CTAButtonWidgetViewModel(properties);

        // Assert
        Assert.That(viewModel.Text, Is.EqualTo("Click Me"));
        Assert.That(viewModel.LinkURL, Is.EqualTo("https://example.com"));
        Assert.That(viewModel.HorizontalAlignment, Is.EqualTo(HorizontalAlignments.Center));
        Assert.That(viewModel.TopMargin, Is.EqualTo(MarginSizes.Large));
    }

    [Test]
    public void CTAButtonWidgetViewModel_HandlesEmptyText()
    {
        // Arrange
        var properties = new CTAButtonWidgetProperties { Text = "" };

        // Act
        var viewModel = new CTAButtonWidgetViewModel(properties);

        // Assert
        Assert.That(viewModel.Text, Is.EqualTo(string.Empty));
    }

    #endregion
}
