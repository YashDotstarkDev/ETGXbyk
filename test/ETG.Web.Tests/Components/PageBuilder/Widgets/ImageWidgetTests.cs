using ETG.Core.Components;
using ETG.Web.Components.PageBuilder.Widgets.Image;
using CMS.ContentEngine;

namespace ETG.Web.Tests.Components.PageBuilder.Widgets;

/// <summary>
/// Tests for ImageWidget - Image content display widget
/// </summary>
[TestFixture]
public class ImageWidgetTests : BaseWebTest
{
    #region Identifier Tests

    [Test]
    public void Widget_HasCorrectIdentifier()
    {
        // Assert
        Assert.That(ImageWidget.IDENTIFIER, Is.EqualTo("Website.ImageWidget"));
    }

    [Test]
    public void Widget_HasCorrectName()
    {
        // Assert
        Assert.That(ImageWidget.NAME, Is.EqualTo("Image"));
    }

    #endregion

    #region Properties Tests

    [Test]
    public void ImageWidgetProperties_HasDefaultAlignment()
    {
        // Arrange & Act
        var properties = new ImageWidgetProperties();

        // Assert
        Assert.That(properties.Alignment, Is.EqualTo(nameof(ImageAlignments.Center)));
    }

    [Test]
    public void ImageWidgetProperties_HasDefaultSize()
    {
        // Arrange & Act
        var properties = new ImageWidgetProperties();

        // Assert
        Assert.That(properties.Size, Is.EqualTo(nameof(ImageSizes.Full_Width)));
    }

    [Test]
    public void ImageWidgetProperties_CanSetAlignment()
    {
        // Arrange
        var properties = new ImageWidgetProperties();

        // Act
        properties.Alignment = nameof(ImageAlignments.Left);

        // Assert
        Assert.That(properties.Alignment, Is.EqualTo(nameof(ImageAlignments.Left)));
    }

    [Test]
    public void ImageWidgetProperties_CanSetSize()
    {
        // Arrange
        var properties = new ImageWidgetProperties();

        // Act
        properties.Size = nameof(ImageSizes.Full_Width);

        // Assert
        Assert.That(properties.Size, Is.EqualTo(nameof(ImageSizes.Full_Width)));
    }

    [Test]
    public void ImageWidgetProperties_HasDefaultSelectedImages()
    {
        // Arrange & Act
        var properties = new ImageWidgetProperties();

        // Assert
        Assert.That(properties.SelectedImages, Is.Not.Null);
        Assert.That(properties.SelectedImages, Is.Empty);
    }

    #endregion

    #region Enum Tests

    [Test]
    [TestCase(ImageAlignments.Left, "Left")]
    [TestCase(ImageAlignments.Center, "Center")]
    [TestCase(ImageAlignments.Right, "Right")]
    public void ImageAlignments_HasCorrectValues(ImageAlignments alignment, string expectedName)
    {
        // Assert
        Assert.That(alignment.ToString(), Is.EqualTo(expectedName));
    }

    [Test]
    [TestCase(ImageSizes.Full_Width, "Full_Width")]
    public void ImageSizes_HasCorrectValues(ImageSizes size, string expectedName)
    {
        // Assert
        Assert.That(size.ToString(), Is.EqualTo(expectedName));
    }

    #endregion
}
