using ETG.Core.Components;
using ETG.Web.Components.PageBuilder.Widgets.Video;
using CMS.ContentEngine;

namespace ETG.Web.Tests.Components.PageBuilder.Widgets;

/// <summary>
/// Tests for VideoWidget - Video content display widget
/// </summary>
[TestFixture]
public class VideoWidgetTests : BaseWebTest
{
    #region Identifier Tests

    [Test]
    public void Widget_HasCorrectIdentifier()
    {
        // Assert
        Assert.That(VideoWidget.IDENTIFIER, Is.EqualTo("Website.VideoWidget"));
    }

    [Test]
    public void Widget_HasCorrectName()
    {
        // Assert
        Assert.That(VideoWidget.NAME, Is.EqualTo("Video"));
    }

    #endregion

    #region Properties Tests

    [Test]
    public void VideoWidgetProperties_HasDefaultAlignment()
    {
        // Arrange & Act
        var properties = new VideoWidgetProperties();

        // Assert
        Assert.That(properties.VideoAlignment, Is.EqualTo(nameof(VideoAlignments.Center)));
    }

    [Test]
    public void VideoWidgetProperties_HasDefaultSize()
    {
        // Arrange & Act
        var properties = new VideoWidgetProperties();

        // Assert
        Assert.That(properties.Size, Is.EqualTo(nameof(VideoSizes.Full_Width)));
    }

    [Test]
    public void VideoWidgetProperties_ShowDescriptionAsCaptionDefaultsFalse()
    {
        // Arrange & Act
        var properties = new VideoWidgetProperties();

        // Assert
        Assert.That(properties.ShowDescriptionAsCaption, Is.False);
    }

    [Test]
    public void VideoWidgetProperties_CanSetAlignment()
    {
        // Arrange
        var properties = new VideoWidgetProperties();

        // Act
        properties.VideoAlignment = nameof(VideoAlignments.Left);

        // Assert
        Assert.That(properties.VideoAlignment, Is.EqualTo(nameof(VideoAlignments.Left)));
    }

    [Test]
    public void VideoWidgetProperties_CanSetSize()
    {
        // Arrange
        var properties = new VideoWidgetProperties();

        // Act
        properties.Size = nameof(VideoSizes.Full_Width);

        // Assert
        Assert.That(properties.Size, Is.EqualTo(nameof(VideoSizes.Full_Width)));
    }

    [Test]
    public void VideoWidgetProperties_CanEnableCaption()
    {
        // Arrange
        var properties = new VideoWidgetProperties();

        // Act
        properties.ShowDescriptionAsCaption = true;

        // Assert
        Assert.That(properties.ShowDescriptionAsCaption, Is.True);
    }

    [Test]
    public void VideoWidgetProperties_HasDefaultSelectedVideos()
    {
        // Arrange & Act
        var properties = new VideoWidgetProperties();

        // Assert
        Assert.That(properties.SelectedVideos, Is.Not.Null);
        Assert.That(properties.SelectedVideos, Is.Empty);
    }

    #endregion

    #region Enum Tests

    [Test]
    [TestCase(VideoAlignments.Left, "Left")]
    [TestCase(VideoAlignments.Center, "Center")]
    [TestCase(VideoAlignments.Right, "Right")]
    public void VideoAlignments_HasCorrectValues(VideoAlignments alignment, string expectedName)
    {
        // Assert
        Assert.That(alignment.ToString(), Is.EqualTo(expectedName));
    }

    [Test]
    [TestCase(VideoSizes.Full_Width, "Full_Width")]
    public void VideoSizes_HasCorrectValues(VideoSizes size, string expectedName)
    {
        // Assert
        Assert.That(size.ToString(), Is.EqualTo(expectedName));
    }

    #endregion
}
