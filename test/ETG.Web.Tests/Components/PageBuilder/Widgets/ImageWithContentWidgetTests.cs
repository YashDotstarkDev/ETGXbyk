using CMS.ContentEngine;
using CSharpFunctionalExtensions;
using ETG.Core.Content;
using ETG.Web.Components.PageBuilder.Widgets.ImageWithContent;
using ETG.Web.Rendering;

namespace ETG.Web.Tests.Components.PageBuilder.Widgets;

[TestFixture]
public class ImageWithContentWidgetTests : BaseWebTest
{
    [Test]
    public void Widget_HasCorrectIdentifier()
    {
        Assert.That(ImageWithContentWidget.IDENTIFIER, Is.EqualTo("Website.ImageWithContentWidget"));
    }

    [Test]
    public void Properties_HaveDefaults()
    {
        var props = new ImageWithContentWidgetProperties();

        Assert.That(props.SelectedImages, Is.Not.Null);
        Assert.That(props.SelectedImages, Is.Empty);
        Assert.That(props.Layout, Is.EqualTo(nameof(ImageWithContentLayouts.ImageLeft)));
        Assert.That(props.Background, Is.EqualTo(nameof(BackgroundOptions.Transparent)));
        Assert.That(props.TopMargin, Is.EqualTo(nameof(ImageWithContentMarginSizes.None)));
        Assert.That(props.BottomMargin, Is.EqualTo(nameof(ImageWithContentMarginSizes.None)));
        Assert.That(props.ShowCta, Is.False);
    }

    [Test]
    public void ViewModel_Mapping_CreatesImageViewModel()
    {
        var imageContent = new MediaFile
        {
            SystemFields = new ContentItemFields { ContentItemGUID = Guid.NewGuid() },
            LegacyMediaFileTitle = "Title",
            LegacyMediaFileDescription = "Alt",
            LegacyMediaFileAsset = new ContentItemAsset { Url = "https://example.com/image.jpg" }
        };

        var vm = ImageAssetViewModel.Create(imageContent);

        Assert.That(vm, Is.Not.Null);
        Assert.That(vm.LegacyMediaFileAsset, Is.Not.Null);
        Assert.That(vm.LegacyMediaFileAsset.Url, Is.EqualTo("https://example.com/image.jpg"));
    }

    [Test]
    public void Validate_ReturnsFailure_WhenNoSelectedImage()
    {
        var props = new ImageWithContentWidgetProperties();

        var result = typeof(ImageWithContentWidget)
            .GetMethod("Validate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            .Invoke(null, new object[] { props, Maybe<MediaFile>.None });

        // result is a CSharpFunctionalExtensions.Result<,>
        var isSuccess = (bool)result.GetType().GetProperty("IsSuccess").GetValue(result)!;
        Assert.That(isSuccess, Is.False);
    }

    [Test]
    public void Validate_ReturnsFailure_WhenImageHasNoAsset()
    {
        var props = new ImageWithContentWidgetProperties();
        props.SelectedImages = new[] { new ContentItemReference { Identifier = Guid.NewGuid() } };

        var image = Maybe<MediaFile>.From(new MediaFile
        {
            SystemFields = new ContentItemFields { ContentItemGUID = Guid.NewGuid() },
            LegacyMediaFileTitle = "Title",
            LegacyMediaFileDescription = "Alt",
            LegacyMediaFileAsset = null
        });

        var result = typeof(ImageWithContentWidget)
            .GetMethod("Validate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            .Invoke(null, new object[] { props, image });

        var isSuccess = (bool)result.GetType().GetProperty("IsSuccess").GetValue(result)!;
        Assert.That(isSuccess, Is.False);
    }
}
