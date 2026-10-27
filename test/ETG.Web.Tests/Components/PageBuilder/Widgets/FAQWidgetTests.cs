using CMS.ContentEngine;
using ETG.Core.Components;
using ETG.Web.Components.PageBuilder.Widgets.FAQ;

namespace ETG.Web.Tests.Components.PageBuilder.Widgets;

[TestFixture]
public class FAQWidgetTests : BaseWebTest
{
    private const string FixedHeading = "Frequently Asked Questions";

    [Test]
    public void Widget_HasCorrectIdentifierAndName()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                FAQWidget.IDENTIFIER,
                Is.EqualTo("ETG.Web.Widget.FAQWidget"));

            Assert.That(FAQWidget.NAME, Is.EqualTo("FAQ"));
        });
    }

    [Test]
    public void Properties_HaveCorrectDefaults()
    {
        var props = new FAQWidgetProperties();

        Assert.Multiple(() =>
        {
            Assert.That(props.Heading, Is.EqualTo(""));
            Assert.That(props.Count, Is.EqualTo(10));
            Assert.That(props.HideViewAll, Is.False);
            Assert.That(props.FaqCtaText, Is.EqualTo("View All"));
            Assert.That(props.FaqCtaLink, Is.EqualTo("/faq"));
            Assert.That(props.Path, Is.Not.Null.And.Empty);
        });
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("General FAQs")]
    public void Properties_KeepHeadingFixedDuringOtherTests(string headingInput)
    {
        var props = new FAQWidgetProperties
        {
            Heading = FixedHeading
        };

        // The test input is intentionally not assigned to Heading.
        props.FaqCtaText = headingInput;

        Assert.Multiple(() =>
        {
            Assert.That(props.Heading, Is.EqualTo(FixedHeading));
            Assert.That(props.FaqCtaText, Is.EqualTo(headingInput));
            Assert.That(
                FAQWidget.IDENTIFIER,
                Is.EqualTo("ETG.Web.Widget.FAQWidget"));
        });
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("View All FAQs")]
    [TestCase("<script>alert(1)</script>")]
    public void Properties_HandleDynamicCtaText(string value)
    {
        var props = new FAQWidgetProperties
        {
            Heading = FixedHeading,
            FaqCtaText = value
        };

        Assert.Multiple(() =>
        {
            Assert.That(props.FaqCtaText, Is.EqualTo(value));
            Assert.That(props.Heading, Is.EqualTo(FixedHeading));
        });
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("/frequently-asked-questions")]
    [TestCase("https://example.com/faq")]
    [TestCase("javascript:alert(1)")]
    public void Properties_HandleDynamicCtaLink(string value)
    {
        var props = new FAQWidgetProperties
        {
            Heading = FixedHeading,
            FaqCtaLink = value
        };

        Assert.Multiple(() =>
        {
            Assert.That(props.FaqCtaLink, Is.EqualTo(value));
            Assert.That(props.Heading, Is.EqualTo(FixedHeading));
        });
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(3)]
    [TestCase(10)]
    [TestCase(-1)]
    public void Properties_HandleDynamicCount(int count)
    {
        var props = new FAQWidgetProperties
        {
            Heading = FixedHeading,
            Count = count
        };

        Assert.Multiple(() =>
        {
            Assert.That(props.Count, Is.EqualTo(count));
            Assert.That(props.Heading, Is.EqualTo(FixedHeading));
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Properties_HandleDynamicHideViewAll(bool hideViewAll)
    {
        var props = new FAQWidgetProperties
        {
            Heading = FixedHeading,
            HideViewAll = hideViewAll
        };

        Assert.Multiple(() =>
        {
            Assert.That(props.HideViewAll, Is.EqualTo(hideViewAll));
            Assert.That(props.Heading, Is.EqualTo(FixedHeading));
        });
    }

    [Test]
    public void Properties_PathCanBeNull()
    {
        var props = new FAQWidgetProperties
        {
            Heading = FixedHeading,
            Path = null!
        };

        Assert.That(props.Path, Is.Null);
        Assert.That(props.Heading, Is.EqualTo(FixedHeading));
    }
}