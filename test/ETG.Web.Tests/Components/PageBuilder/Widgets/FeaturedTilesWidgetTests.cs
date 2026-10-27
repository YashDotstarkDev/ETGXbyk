using System;
using NUnit.Framework;
using ETG.Web.Components.PageBuilder.Widgets.FeaturedTiles;

namespace ETG.Web.Tests.Components.PageBuilder.Widgets;

// Property and enum-parsing tests only; not InvokeAsync or rendering.
// KnownValidationGap documents existing behavior, not safe validation.
// Not compiled or executed here.
[TestFixture]
public class FeaturedTilesWidgetTests : BaseWebTest
{
    [Test]
    public void Widget_HasCorrectIdentifierAndName()
    {
        Assert.Multiple(() =>
        {
            Assert.That(FeaturedTilesWidget.IDENTIFIER,
                Is.EqualTo("ETG.Web.Widget.FeaturedTiles"));
            Assert.That(FeaturedTilesWidget.NAME,
                Is.EqualTo("Featured Tiles"));
        });
    }

    [Test]
    public void Properties_HaveCorrectDefaults()
    {
        var props = new FeaturedTilesWidgetProperties();

        Assert.Multiple(() =>
        {
            Assert.That(props.Heading, Is.Empty);
            Assert.That(props.Description, Is.Empty);
            Assert.That(props.SectionId, Is.Empty);
            Assert.That(props.Path, Is.Not.Null.And.Empty);
            Assert.That(props.DisplayLayout,
                Is.EqualTo(nameof(DisplayLayoutOptions.LargeTextCard)));
            Assert.That(props.TileType,
                Is.EqualTo(nameof(TileTypeOptions.TilesAreLinks)));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" \t\r\n ")]
    [TestCase("<script>alert(1)</script>")]
    public void TextProperties_PreserveAssignedInput(string? input)
    {
        var props = new FeaturedTilesWidgetProperties
        {
            Heading = input!,
            Description = input!,
            SectionId = input!
        };

        Assert.Multiple(() =>
        {
            Assert.That(props.Heading, Is.EqualTo(input));
            Assert.That(props.Description, Is.EqualTo(input));
            Assert.That(props.SectionId, Is.EqualTo(input));
        });
    }

    [Test]
    public void TextProperties_HandleGeneratedValuesIndependently()
    {
        var heading = $"Heading-{Guid.NewGuid():N}";
        var description = $"Description-{Guid.NewGuid():N}";
        var sectionId = $"section-{Guid.NewGuid():N}";

        var props = new FeaturedTilesWidgetProperties
        {
            Heading = heading,
            Description = description,
            SectionId = sectionId
        };

        Assert.Multiple(() =>
        {
            Assert.That(props.Heading, Is.EqualTo(heading));
            Assert.That(props.Description, Is.EqualTo(description));
            Assert.That(props.SectionId, Is.EqualTo(sectionId));
        });
    }

    [Test]
    public void DisplayLayout_ParsesEveryDefinedOption()
    {
        foreach (var expected in Enum.GetValues<DisplayLayoutOptions>())
        {
            var props = new FeaturedTilesWidgetProperties
            {
                DisplayLayout = expected.ToString()
            };

            Assert.That(props.DisplayLayoutParsed, Is.EqualTo(expected));
        }
    }

    [Test]
    public void TileType_ParsesEveryDefinedOption()
    {
        foreach (var expected in Enum.GetValues<TileTypeOptions>())
        {
            var props = new FeaturedTilesWidgetProperties
            {
                TileType = expected.ToString()
            };

            Assert.That(props.TileTypeParsed, Is.EqualTo(expected));
        }
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("UnknownOption")]
    public void InvalidTextOptions_FallBackWithoutThrowing(string? input)
    {
        var props = new FeaturedTilesWidgetProperties
        {
            DisplayLayout = input!,
            TileType = input!
        };

        Assert.Multiple(() =>
        {
            Assert.That(props.DisplayLayoutParsed,
                Is.EqualTo(DisplayLayoutOptions.LargeTextCard));
            Assert.That(props.TileTypeParsed,
                Is.EqualTo(TileTypeOptions.TilesAreLinks));
        });
    }

    [Test]
    [Category("KnownValidationGap")]
    public void UndefinedNumericOptions_CurrentlyRemainUndefined()
    {
        const int value = int.MaxValue;

        var props = new FeaturedTilesWidgetProperties
        {
            DisplayLayout = value.ToString(),
            TileType = value.ToString()
        };

        Assert.Multiple(() =>
        {
            Assert.That(props.DisplayLayoutParsed,
                Is.EqualTo((DisplayLayoutOptions)value));
            Assert.That(props.TileTypeParsed,
                Is.EqualTo((TileTypeOptions)value));

            Assert.That(Enum.IsDefined(
                typeof(DisplayLayoutOptions), props.DisplayLayoutParsed),
                Is.False);

            Assert.That(Enum.IsDefined(
                typeof(TileTypeOptions), props.TileTypeParsed),
                Is.False);
        });
    }

    [Test]
    public void Path_AcceptsEmptyAndNull()
    {
        var props = new FeaturedTilesWidgetProperties { Path = [] };
        Assert.That(props.Path, Is.Empty);

        props.Path = null!;
        Assert.That(props.Path, Is.Null);
    }
}