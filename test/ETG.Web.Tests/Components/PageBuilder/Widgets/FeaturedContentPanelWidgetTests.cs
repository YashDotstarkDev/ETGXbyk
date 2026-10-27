using CMS.ContentEngine;
using ETG.Web.Components.PageBuilder.Widgets.FeaturedContentPanel;

namespace ETG.Web.Tests.Components.PageBuilder.Widgets;

[TestFixture]
public class FeaturedContentPanelWidgetTests : BaseWebTest
{
    [Test]
    public void Widget_HasCorrectIdentifierAndName()
    {
        Assert.Multiple(() =>
        {
            Assert.That(FeaturedContentPanelWidget.IDENTIFIER,
                Is.EqualTo("ETG.Web.Widget.FeaturedContentPanel"));
            Assert.That(FeaturedContentPanelWidget.NAME, Is.EqualTo("Feature"));
        });
    }

    [Test]
    public void Properties_HaveCorrectDefaults()
    {
        var props = new FeaturedContentPanelProperties();

        Assert.Multiple(() =>
        {
            Assert.That(props.Images, Is.Empty);
            Assert.That(props.Heading, Is.Empty);
            Assert.That(props.TextContent, Is.Empty);
            Assert.That(props.CTALabel, Is.Empty);
            Assert.That(props.ImageCaption, Is.Empty);
            Assert.That(props.Path, Is.Empty);
        });
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("Featured Content")]
    [TestCase("<script>alert(1)</script>")]
    public void Properties_HandleDynamicText(string value)
    {
        var props = new FeaturedContentPanelProperties
        {
            Heading = value,
            TextContent = value,
            CTALabel = value,
            ImageCaption = value,
            Path = value
        };

        Assert.Multiple(() =>
        {
            Assert.That(props.Heading, Is.EqualTo(value));
            Assert.That(props.TextContent, Is.EqualTo(value));
            Assert.That(props.CTALabel, Is.EqualTo(value));
            Assert.That(props.ImageCaption, Is.EqualTo(value));
            Assert.That(props.Path, Is.EqualTo(value));
        });
    }

    [Test]
    public void Properties_ImagesCanBeEmptyOrNull()
    {
        var props = new FeaturedContentPanelProperties { Images = [] };
        Assert.That(props.Images, Is.Empty);

        props.Images = null!;
        Assert.That(props.Images, Is.Null);
    }

    [Test]
    public void Properties_TextFieldsCanBeNull()
    {
        var props = new FeaturedContentPanelProperties
        {
            Heading = null!,
            TextContent = null!,
            CTALabel = null!,
            ImageCaption = null!,
            Path = null!
        };

        Assert.Multiple(() =>
        {
            Assert.That(props.Heading, Is.Null);
            Assert.That(props.TextContent, Is.Null);
            Assert.That(props.CTALabel, Is.Null);
            Assert.That(props.ImageCaption, Is.Null);
            Assert.That(props.Path, Is.Null);
        });
    }
}