
using CMS.ContentEngine;
using ETG.Core.Components;
using ETG.Web.Components.PageBuilder.Widgets.CTAPanel;

namespace ETG.Web.Tests.Components.PageBuilder.Widgets;

[TestFixture]
public class CTAPanelWidgetTests : BaseWebTest
{
    #region Widget Tests

    [Test]
    public void Widget_HasCorrectIdentifier()
    {
        Assert.That(
            CTAPanelWidget.IDENTIFIER,
            Is.EqualTo("ETG.Web.Widget.CTAPanel"));
    }

    [Test]
    public void Widget_HasCorrectName()
    {
        Assert.That(
            CTAPanelWidget.NAME,
            Is.EqualTo("CTA Panel"));
    }

    #endregion

    #region Properties Default Value Tests

    [Test]
    public void Properties_HaveExpectedDefaults()
    {
        var properties = new CTAPanelProperties();

        Assert.Multiple(() =>
        {
            Assert.That(properties.Heading, Is.EqualTo(""));
            Assert.That(properties.TextContent, Is.EqualTo(""));
            Assert.That(properties.CTALabel, Is.EqualTo(""));
            Assert.That(properties.Path, Is.EqualTo(""));
            Assert.That(properties.CTALabel2, Is.EqualTo(""));
            Assert.That(properties.Path2, Is.EqualTo(""));

            Assert.That(properties.Images, Is.Not.Null);
            Assert.That(properties.Images, Is.Empty);
        });
    }

    #endregion

    #region Dynamic Property Assignment Tests

    [TestCase("CTA Panel Heading")]
    [TestCase("Explore Destinations")]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("<script>alert(1)</script>")]
    public void Properties_CanSetHeading(string value)
    {
        var properties = new CTAPanelProperties
        {
            Heading = value
        };

        Assert.That(properties.Heading, Is.EqualTo(value));
    }

    [TestCase("Discover our latest offers.")]
    [TestCase("Explore more information.")]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("<script>alert(1)</script>")]
    public void Properties_CanSetTextContent(string value)
    {
        var properties = new CTAPanelProperties
        {
            TextContent = value
        };

        Assert.That(properties.TextContent, Is.EqualTo(value));
    }

    [TestCase("Learn More")]
    [TestCase("Book Now")]
    [TestCase("")]
    [TestCase("   ")]
    public void Properties_CanSetCTALabel(string value)
    {
        var properties = new CTAPanelProperties
        {
            CTALabel = value
        };

        Assert.That(properties.CTALabel, Is.EqualTo(value));
    }

    [TestCase("/destinations")]
    [TestCase("https://example.com")]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("not-a-url")]
    [TestCase("javascript:alert(1)")]
    public void Properties_CanSetPath(string value)
    {
        var properties = new CTAPanelProperties
        {
            Path = value
        };

        Assert.That(properties.Path, Is.EqualTo(value));
    }

    [TestCase("Explore Now")]
    [TestCase("Contact Us")]
    [TestCase("")]
    [TestCase("   ")]
    public void Properties_CanSetCTALabel2(string value)
    {
        var properties = new CTAPanelProperties
        {
            CTALabel2 = value
        };

        Assert.That(properties.CTALabel2, Is.EqualTo(value));
    }

    [TestCase("/contact")]
    [TestCase("https://example.com/contact")]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("not-a-url")]
    [TestCase("javascript:alert(1)")]
    public void Properties_CanSetPath2(string value)
    {
        var properties = new CTAPanelProperties
        {
            Path2 = value
        };

        Assert.That(properties.Path2, Is.EqualTo(value));
    }

    #endregion

    #region Dynamic CTA Pair Tests

    [TestCase("Learn More", "/learn-more")]
    [TestCase("Book Now", "/booking")]
    [TestCase("", "")]
    [TestCase("   ", "   ")]
    [TestCase("Learn More", "")]
    [TestCase("", "/learn-more")]
    [TestCase("Contact Us", "javascript:alert(1)")]
    public void Properties_CanSetFirstCTAPair(
        string label,
        string path)
    {
        var properties = new CTAPanelProperties
        {
            CTALabel = label,
            Path = path
        };

        Assert.Multiple(() =>
        {
            Assert.That(properties.CTALabel, Is.EqualTo(label));
            Assert.That(properties.Path, Is.EqualTo(path));
        });
    }

    [TestCase("Explore Now", "/explore")]
    [TestCase("Contact Us", "/contact")]
    [TestCase("", "")]
    [TestCase("   ", "   ")]
    [TestCase("Explore Now", "")]
    [TestCase("", "/explore")]
    [TestCase("Contact Us", "javascript:alert(1)")]
    public void Properties_CanSetSecondCTAPair(
        string label,
        string path)
    {
        var properties = new CTAPanelProperties
        {
            CTALabel2 = label,
            Path2 = path
        };

        Assert.Multiple(() =>
        {
            Assert.That(properties.CTALabel2, Is.EqualTo(label));
            Assert.That(properties.Path2, Is.EqualTo(path));
        });
    }

    #endregion

    #region Dynamic Combined Properties Tests

    [TestCase(
        "Discover More",
        "Explore our destinations.",
        "Learn More",
        "/learn-more",
        "Book Now",
        "/booking")]
    [TestCase(
        "",
        "",
        "",
        "",
        "",
        "")]
    [TestCase(
        "   ",
        "   ",
        "   ",
        "   ",
        "   ",
        "   ")]
    [TestCase(
        "<script>alert(1)</script>",
        "<script>alert(1)</script>",
        "Click",
        "javascript:alert(1)",
        "Open",
        "javascript:alert(1)")]
    public void Properties_CanSetAllValues(
        string heading,
        string textContent,
        string ctaLabel,
        string path,
        string ctaLabel2,
        string path2)
    {
        var properties = new CTAPanelProperties
        {
            Heading = heading,
            TextContent = textContent,
            CTALabel = ctaLabel,
            Path = path,
            CTALabel2 = ctaLabel2,
            Path2 = path2
        };

        Assert.Multiple(() =>
        {
            Assert.That(properties.Heading, Is.EqualTo(heading));
            Assert.That(properties.TextContent, Is.EqualTo(textContent));
            Assert.That(properties.CTALabel, Is.EqualTo(ctaLabel));
            Assert.That(properties.Path, Is.EqualTo(path));
            Assert.That(properties.CTALabel2, Is.EqualTo(ctaLabel2));
            Assert.That(properties.Path2, Is.EqualTo(path2));
        });
    }

    #endregion

    #region Image Properties Tests

    [Test]
    public void Properties_HasEmptyImagesByDefault()
    {
        var properties = new CTAPanelProperties();

        Assert.That(properties.Images, Is.Not.Null);
        Assert.That(properties.Images, Is.Empty);
    }

    [Test]
    public void Properties_CanSetEmptyImagesCollection()
    {
        var properties = new CTAPanelProperties
        {
            Images = Array.Empty<ContentItemReference>()
        };

        Assert.That(properties.Images, Is.Empty);
    }

    [Test]
    public void Properties_CanSetNullImages()
    {
        var properties = new CTAPanelProperties
        {
            Images = null!
        };

        Assert.That(properties.Images, Is.Null);
    }

    #endregion
}
