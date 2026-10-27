
using ETG.Core.Components;
using ETG.Web.Components.PageBuilder.Widgets.SixColumnCallout;

namespace ETG.Web.Tests.Components.PageBuilder.Widgets;

[TestFixture]
public class SixColumnCalloutWidgetTests : BaseWebTest
{
    #region Widget Tests

    [Test]
    public void Widget_HasCorrectIdentifier()
    {
        Assert.That(
            SixColumnCalloutWidget.IDENTIFIER,
            Is.EqualTo("ETG.Web.Widget.SixColumnCallout"));
    }

    [Test]
    public void Widget_HasCorrectName()
    {
        Assert.That(
            SixColumnCalloutWidget.NAME,
            Is.EqualTo("Six Column Callout"));
    }

    #endregion

    #region Properties Default Value Tests

    [Test]
    public void Properties_HaveExpectedDefaults()
    {
        var properties = new SixColumnCalloutWidgetProperties();

        Assert.Multiple(() =>
        {
            Assert.That(properties.DisplayType,
                Is.EqualTo("Feature Highlights"));
            Assert.That(properties.Heading, Is.EqualTo(""));

            Assert.That(properties.Label1, Is.EqualTo(""));
            Assert.That(properties.Label2, Is.EqualTo(""));
            Assert.That(properties.Label3, Is.EqualTo(""));
            Assert.That(properties.Label4, Is.EqualTo(""));
            Assert.That(properties.Label5, Is.EqualTo(""));
            Assert.That(properties.Label6, Is.EqualTo(""));

            Assert.That(properties.SVGCode1, Is.EqualTo(""));
            Assert.That(properties.SVGCode2, Is.EqualTo(""));
            Assert.That(properties.SVGCode3, Is.EqualTo(""));
            Assert.That(properties.SVGCode4, Is.EqualTo(""));
            Assert.That(properties.SVGCode5, Is.EqualTo(""));
            Assert.That(properties.SVGCode6, Is.EqualTo(""));

            Assert.That(properties.CTALabel, Is.EqualTo(""));
            Assert.That(properties.CTAUrl, Is.EqualTo(""));
            Assert.That(properties.SectionId, Is.EqualTo(""));
        });
    }

    #endregion

    #region Dynamic Property Assignment Tests

    [TestCase("Feature Highlights")]
    [TestCase("Brand Highlights")]
    [TestCase("Experience Highlights")]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("InvalidType")]
    public void Properties_CanSetDisplayType(string value)
    {
        var properties = new SixColumnCalloutWidgetProperties
        {
            DisplayType = value
        };

        Assert.That(properties.DisplayType, Is.EqualTo(value));
    }

    [TestCase("Our Highlights")]
    [TestCase("Fast Booking")]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("<script>alert(1)</script>")]
    public void Properties_CanSetHeading(string value)
    {
        var properties = new SixColumnCalloutWidgetProperties
        {
            Heading = value
        };

        Assert.That(properties.Heading, Is.EqualTo(value));
    }

    [TestCase("Feature One")]
    [TestCase("Fast Booking")]
    [TestCase("Expert Support")]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("<script>alert(1)</script>")]
    public void Properties_CanSetLabel(string value)
    {
        var properties = new SixColumnCalloutWidgetProperties
        {
            Label1 = value
        };

        Assert.That(properties.Label1, Is.EqualTo(value));
    }

    [TestCase("<svg></svg>")]
    [TestCase("<svg><path d='M0 0'/></svg>")]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("<script>alert(1)</script>")]
    [TestCase("<svg onload='alert(1)'></svg>")]
    public void Properties_CanSetSVGCode(string value)
    {
        var properties = new SixColumnCalloutWidgetProperties
        {
            SVGCode1 = value
        };

        Assert.That(properties.SVGCode1, Is.EqualTo(value));
    }

    [TestCase("Learn More")]
    [TestCase("Explore Now")]
    [TestCase("")]
    [TestCase("   ")]
    public void Properties_CanSetCTALabel(string value)
    {
        var properties = new SixColumnCalloutWidgetProperties
        {
            CTALabel = value
        };

        Assert.That(properties.CTALabel, Is.EqualTo(value));
    }

    [TestCase("/learn-more")]
    [TestCase("https://example.com")]
    [TestCase("")]
    [TestCase("not-a-url")]
    [TestCase("javascript:alert(1)")]
    public void Properties_CanSetCTAUrl(string value)
    {
        var properties = new SixColumnCalloutWidgetProperties
        {
            CTAUrl = value
        };

        Assert.That(properties.CTAUrl, Is.EqualTo(value));
    }

    [TestCase("our-highlights")]
    [TestCase("section-123")]
    [TestCase("")]
    [TestCase("   ")]
    public void Properties_CanSetSectionId(string value)
    {
        var properties = new SixColumnCalloutWidgetProperties
        {
            SectionId = value
        };

        Assert.That(properties.SectionId, Is.EqualTo(value));
    }

    #endregion

    #region Dynamic Label and SVG Pair Tests

    [TestCase("Feature One", "<svg></svg>")]
    [TestCase("Fast Booking", "<svg><path d='M0 0'/></svg>")]
    [TestCase("", "")]
    [TestCase("   ", "   ")]
    [TestCase("Feature One", "")]
    [TestCase("", "<svg></svg>")]
    public void Properties_CanSetLabelAndSVGCode(
        string label,
        string svgCode)
    {
        var properties = new SixColumnCalloutWidgetProperties
        {
            Label1 = label,
            SVGCode1 = svgCode
        };

        Assert.Multiple(() =>
        {
            Assert.That(properties.Label1, Is.EqualTo(label));
            Assert.That(properties.SVGCode1, Is.EqualTo(svgCode));
        });
    }

    #endregion

    #region Dynamic Multiple Item Tests

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    public void Properties_CanSetMultipleItems(int itemCount)
    {
        var properties = new SixColumnCalloutWidgetProperties();

        var labels = new[]
        {
            "Feature One",
            "Feature Two",
            "Feature Three",
            "Feature Four",
            "Feature Five",
            "Feature Six"
        };

        var svgCodes = new[]
        {
            "<svg id='one'></svg>",
            "<svg id='two'></svg>",
            "<svg id='three'></svg>",
            "<svg id='four'></svg>",
            "<svg id='five'></svg>",
            "<svg id='six'></svg>"
        };

        var labelSetters = new Action<string>[]
        {
            value => properties.Label1 = value,
            value => properties.Label2 = value,
            value => properties.Label3 = value,
            value => properties.Label4 = value,
            value => properties.Label5 = value,
            value => properties.Label6 = value
        };

        var svgSetters = new Action<string>[]
        {
            value => properties.SVGCode1 = value,
            value => properties.SVGCode2 = value,
            value => properties.SVGCode3 = value,
            value => properties.SVGCode4 = value,
            value => properties.SVGCode5 = value,
            value => properties.SVGCode6 = value
        };

        for (var i = 0; i < itemCount; i++)
        {
            labelSetters[i](labels[i]);
            svgSetters[i](svgCodes[i]);
        }

        Assert.Multiple(() =>
        {
            for (var i = 0; i < itemCount; i++)
            {
                Assert.That(
                    new[]
                    {
                        properties.Label1,
                        properties.Label2,
                        properties.Label3,
                        properties.Label4,
                        properties.Label5,
                        properties.Label6
                    }[i],
                    Is.EqualTo(labels[i]));

                Assert.That(
                    new[]
                    {
                        properties.SVGCode1,
                        properties.SVGCode2,
                        properties.SVGCode3,
                        properties.SVGCode4,
                        properties.SVGCode5,
                        properties.SVGCode6
                    }[i],
                    Is.EqualTo(svgCodes[i]));
            }
        });
    }

    #endregion
}
