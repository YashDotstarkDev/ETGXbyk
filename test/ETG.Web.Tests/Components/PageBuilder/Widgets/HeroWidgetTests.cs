using ETG.Web.Components.PageBuilder.Widgets.Hero;
using CMS.ContentEngine;

namespace ETG.Web.Tests.Components.PageBuilder.Widgets;

/// <summary>
/// Tests for HeroWidget - hero banner widget
/// </summary>
[TestFixture]
public class HeroWidgetTests : BaseWebTest
{
    private HeroWidget? _widget;

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        // HeroWidget requires IContentRetriever in production; tests here validate properties and view model only
        _widget = null;
    }

    #region Identifier Tests

    [Test]
    public void Widget_HasCorrectIdentifier()
    {
        // Assert
        Assert.That(HeroWidget.IDENTIFIER, Is.EqualTo("Website.HeroWidget"));
    }

    #endregion

    #region Properties Tests

    [Test]
    public void HeroWidgetProperties_HaveDefaults()
    {
        // Arrange & Act
        var props = new HeroWidgetProperties();

        // Assert
        Assert.That(props.Title, Is.EqualTo(string.Empty));
        Assert.That(props.Subtitle, Is.EqualTo(string.Empty));
        Assert.That(props.CTAButtonText, Is.EqualTo(string.Empty));
        Assert.That(props.CTAUrl, Is.EqualTo(string.Empty));
        Assert.That(props.TextAlignment, Is.EqualTo(nameof(HeroTextAlignments.Center)));
        Assert.That(props.TextAlignmentParsed, Is.EqualTo(HeroTextAlignments.Center));
        Assert.That(props.Overlay, Is.EqualTo(nameof(HeroOverlayOptions.None)));
        Assert.That(props.OverlayParsed, Is.EqualTo(HeroOverlayOptions.None));
        Assert.That(props.Padding, Is.EqualTo(nameof(MarginSizes.Medium)));
        Assert.That(props.PaddingParsed, Is.EqualTo(MarginSizes.Medium));
    }

    #endregion

    #region Enum Tests

    [Test]
    public void HeroEnums_HaveCorrectValues()
    {
        // Assert
        Assert.That(HeroTextAlignments.Left.ToString(), Is.EqualTo("Left"));
        Assert.That(HeroTextAlignments.Center.ToString(), Is.EqualTo("Center"));
        Assert.That(HeroTextAlignments.Right.ToString(), Is.EqualTo("Right"));

        Assert.That(HeroOverlayOptions.None.ToString(), Is.EqualTo("None"));
        Assert.That(HeroOverlayOptions.Dark_25.ToString(), Is.EqualTo("Dark_25"));
        Assert.That(HeroOverlayOptions.Dark_50.ToString(), Is.EqualTo("Dark_50"));

        Assert.That(MarginSizes.None.ToString(), Is.EqualTo("None"));
        Assert.That(MarginSizes.Small.ToString(), Is.EqualTo("Small"));
        Assert.That(MarginSizes.Medium.ToString(), Is.EqualTo("Medium"));
        Assert.That(MarginSizes.Large.ToString(), Is.EqualTo("Large"));
    }

    #endregion

    #region ViewModel Tests

    [Test]
    public void HeroWidgetViewModel_InitializesFromImageAndProperties()
    {
        // Arrange
        var props = new HeroWidgetProperties
        {
            Title = "Welcome",
            Subtitle = "Sub",
            CTAButtonText = "Learn",
            CTAUrl = "/learn",
            TextAlignment = nameof(HeroTextAlignments.Right),
            Overlay = nameof(HeroOverlayOptions.Dark_25),
            Padding = nameof(MarginSizes.Small)
        };

        // Create minimal ImageContent to pass into ImageAssetViewModel
        var img = new ETG.Core.Content.MediaFile();
        img.LegacyMediaFileTitle = "ImgTitle";
        img.LegacyMediaFileDescription = "Alt";
        img.LegacyMediaFileAsset = new ContentItemAsset { Url = "/media/img.jpg" };
        // Ensure system fields are populated to avoid null reference when constructing view model
        img.SystemFields = new ContentItemFields { ContentItemGUID = Guid.NewGuid() };

        var imageVm = new ETG.Web.Rendering.ImageAssetViewModel(img);

        // Act
        var vm = new HeroWidgetViewModel(imageVm, props);

        // Assert
        Assert.That(vm.Title, Is.EqualTo("Welcome"));
        Assert.That(vm.Subtitle, Is.EqualTo("Sub"));
        Assert.That(vm.CTAButtonText, Is.EqualTo("Learn"));
        Assert.That(vm.CTAUrl, Is.EqualTo("/learn"));
        Assert.That(vm.TextAlignment, Is.EqualTo(HeroTextAlignments.Right));
        Assert.That(vm.Overlay, Is.EqualTo(HeroOverlayOptions.Dark_25));
        Assert.That(vm.Padding, Is.EqualTo(MarginSizes.Small));
        Assert.That(vm.Image, Is.Not.Null);
        Assert.That(vm.Image.LegacyMediaFileAsset.Url, Is.EqualTo("/media/img.jpg"));
    }

    #endregion
}
