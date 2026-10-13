using CMS.ContentEngine;
using ETG.Core.Components;
using ETG.Web.Components.PageBuilder.Widgets.FAQ;

namespace ETG.Web.Tests.Components.PageBuilder.Widgets;

/// <summary>
/// Tests for FAQWidget.
/// </summary>
[TestFixture]
public class FAQWidgetTests : BaseWebTest
{
    #region Widget Tests

    [Test]
    public void Widget_HasCorrectIdentifier()
    {
        Assert.That(FAQWidget.IDENTIFIER, Is.EqualTo("Website.FAQWidget"));
    }

    [Test]
    public void Widget_HasCorrectName()
    {
        Assert.That(FAQWidget.NAME, Is.EqualTo("FAQ"));
    }

    #endregion

    #region Properties Default Value Tests

    [Test]
    public void FAQWidgetProperties_HasDefaultHeading()
    {
        var properties = new FAQWidgetProperties();

        Assert.That(properties.Heading, Is.EqualTo(""));
    }

    [Test]
    public void FAQWidgetProperties_HasDefaultCount()
    {
        var properties = new FAQWidgetProperties();

        Assert.That(properties.Count, Is.EqualTo(0));
    }

    [Test]
    public void FAQWidgetProperties_HasDefaultHideViewAll()
    {
        var properties = new FAQWidgetProperties();

        Assert.That(properties.HideViewAll, Is.False);
    }

    [Test]
    public void FAQWidgetProperties_HasDefaultFaqCtaText()
    {
        var properties = new FAQWidgetProperties();

        Assert.That(properties.FaqCtaText, Is.EqualTo("View All"));
    }

    [Test]
    public void FAQWidgetProperties_HasDefaultFaqCtaLink()
    {
        var properties = new FAQWidgetProperties();

        Assert.That(properties.FaqCtaLink, Is.EqualTo("/faq"));
    }

    [Test]
    public void FAQWidgetProperties_HasDefaultPath()
    {
        var properties = new FAQWidgetProperties();

        Assert.That(properties.Path, Is.Not.Null);
        Assert.That(properties.Path, Is.Empty);
    }

    #endregion

    #region Properties Set Value Tests

    [Test]
    public void FAQWidgetProperties_CanSetHeading()
    {
        var properties = new FAQWidgetProperties
        {
            Heading = "Frequently Asked Questions"
        };

        Assert.That(properties.Heading, Is.EqualTo("Frequently Asked Questions"));
    }

    [Test]
    public void FAQWidgetProperties_CanSetCount()
    {
        var properties = new FAQWidgetProperties
        {
            Count = 10
        };

        Assert.That(properties.Count, Is.EqualTo(3));
    }

    [Test]
    public void FAQWidgetProperties_CanSetHideViewAll()
    {
        var properties = new FAQWidgetProperties
        {
            HideViewAll = true
        };

        Assert.That(properties.HideViewAll, Is.True);
    }

    [Test]
    public void FAQWidgetProperties_CanSetFaqCtaText()
    {
        var properties = new FAQWidgetProperties
        {
            FaqCtaText = "Explore FAQs"
        };

        Assert.That(properties.FaqCtaText, Is.EqualTo("Explore FAQs"));
    }

    [Test]
    public void FAQWidgetProperties_CanSetFaqCtaLink()
    {
        var properties = new FAQWidgetProperties
        {
            FaqCtaLink = "/frequently-asked-questions"
        };

        Assert.That(properties.FaqCtaLink, Is.EqualTo("/frequently-asked-questions"));
    }

    #endregion
}