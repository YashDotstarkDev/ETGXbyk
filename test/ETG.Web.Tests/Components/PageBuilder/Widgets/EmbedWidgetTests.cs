using ETG.Core.Components;
using ETG.Web.Components.PageBuilder.Widgets.Embed;
using CMS.ContentEngine;

namespace ETG.Web.Tests.Components.PageBuilder.Widgets;

/// <summary>
/// Tests for EmbedWidget - HTML embed content widget
/// </summary>
[TestFixture]
public class EmbedWidgetTests : BaseWebTest
{
    #region Identifier Tests

    [Test]
    public void Widget_HasCorrectIdentifier()
    {
        // Assert
        Assert.That(EmbedWidget.IDENTIFIER, Is.EqualTo("Website.EmbedWidget"));
    }

    [Test]
    public void Widget_HasCorrectName()
    {
        // Assert
        Assert.That(EmbedWidget.NAME, Is.EqualTo("Embed"));
    }

    #endregion

    #region Properties Tests

    [Test]
    public void EmbedWidgetProperties_HasDefaultComponentWidth()
    {
        // Arrange & Act
        var properties = new EmbedWidgetProperties();

        // Assert
        Assert.That(properties.ComponentWidth, Is.EqualTo(nameof(ComponentWidths.Full)));
    }

    [Test]
    public void EmbedWidgetProperties_HasDefaultHorizontalAlignment()
    {
        // Arrange & Act
        var properties = new EmbedWidgetProperties();

        // Assert
        Assert.That(properties.HorizontalAlignment, Is.EqualTo(nameof(HorizontalAlignments.Left)));
    }

    [Test]
    public void EmbedWidgetProperties_CanSetComponentWidth()
    {
        // Arrange
        var properties = new EmbedWidgetProperties();

        // Act
        properties.ComponentWidth = nameof(ComponentWidths.Natural);

        // Assert
        Assert.That(properties.ComponentWidth, Is.EqualTo(nameof(ComponentWidths.Natural)));
    }

    [Test]
    public void EmbedWidgetProperties_CanSetAlignment()
    {
        // Arrange
        var properties = new EmbedWidgetProperties();

        // Act
        properties.HorizontalAlignment = nameof(HorizontalAlignments.Center);

        // Assert
        Assert.That(properties.HorizontalAlignment, Is.EqualTo(nameof(HorizontalAlignments.Center)));
    }

    [Test]
    public void EmbedWidgetProperties_HasDefaultSelectedEmbeds()
    {
        // Arrange & Act
        var properties = new EmbedWidgetProperties();

        // Assert
        Assert.That(properties.SelectedEmbeds, Is.Not.Null);
        Assert.That(properties.SelectedEmbeds, Is.Empty);
    }

    #endregion

    #region Enum Tests

    [Test]
    [TestCase(ComponentWidths.Full, "Full")]
    [TestCase(ComponentWidths.Natural, "Natural")]
    public void ComponentWidths_HasCorrectValues(ComponentWidths width, string expectedName)
    {
        // Assert
        Assert.That(width.ToString(), Is.EqualTo(expectedName));
    }

    [Test]
    [TestCase(HorizontalAlignments.Left, "Left")]
    [TestCase(HorizontalAlignments.Center, "Center")]
    public void HorizontalAlignments_HasCorrectValues(HorizontalAlignments alignment, string expectedName)
    {
        // Assert
        Assert.That(alignment.ToString(), Is.EqualTo(expectedName));
    }

    #endregion
}
