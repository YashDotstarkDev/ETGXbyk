#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CMS.ContentEngine;
using CMS.Websites;
using ETG.Web.Components;
using ETG.Web.Components.PageBuilder.Widgets.SmallTiles;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using NUnit.Framework;

namespace ETG.Web.Tests.Components.PageBuilder.Widgets;

[TestFixture]
public class SmallTilesWidgetTests : BaseWebTest
{
    private const string Pass = "ExpectedToPass";
    private const string Gap = "ExpectedToExposeLogicOrValidationGap";
    private const string ErrorView = "~/Components/ComponentError.cshtml";

    private Mock<IContentRetriever> retriever = null!;
    private Mock<IWebPageUrlRetriever> urlRetriever = null!;
    private Mock<IPreferredLanguageRetriever> preferredLanguage = null!;
    private SmallTilesWidget widget = null!;
    private Random random = null!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();

        random = new Random(23);
        retriever = new(MockBehavior.Strict);
        urlRetriever = new(MockBehavior.Strict);
        preferredLanguage = new(MockBehavior.Strict);

        widget = new SmallTilesWidget(
            retriever.Object,
            urlRetriever.Object,
            preferredLanguage.Object)
        {
            ViewComponentContext = new ViewComponentContext
            {
                ViewContext = new ViewContext
                {
                    HttpContext = new DefaultHttpContext(),
                    ViewData = new ViewDataDictionary(
                        new EmptyModelMetadataProvider(),
                        new ModelStateDictionary())
                }
            }
        };
    }

    [Test]
    [Category(Pass)]
    public void IdentifierAndName_AreCorrect()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SmallTilesWidget.IDENTIFIER,
                Is.EqualTo("ETG.Web.Widget.SmallTiles"));
            Assert.That(SmallTilesWidget.NAME, Is.EqualTo("Small Tiles"));
        });
    }

    [TestCase("null")]
    [TestCase("empty")]
    [TestCase("empty-guid")]
    [TestCase("multiple-empty-guids")]
    [Category(Pass)]
    public async Task NoUsableIds_ReturnsExactError(string scenario)
    {
        // Arrange
        var props = new SmallTilesWidgetProperties
        {
            Path = scenario switch
            {
                "null" => null!,
                "empty" => Array.Empty<ContentItemReference>(),
                "empty-guid" => References(Guid.Empty),
                "multiple-empty-guids" => References(Guid.Empty, Guid.Empty),
                _ => throw new ArgumentOutOfRangeException(nameof(scenario))
            }
        };

        // Act
        var result = await widget.InvokeAsync(Model(props));

        // Assert
        Assert.That(result, Is.TypeOf<ViewViewComponentResult>());
        var view = (ViewViewComponentResult)result;

        Assert.That(view.ViewName, Is.EqualTo(ErrorView));
        Assert.That(view.ViewData.Model,
            Is.TypeOf<ComponentErrorViewModel>());

        var error = (ComponentErrorViewModel)view.ViewData.Model!;

        Assert.Multiple(() =>
        {
            Assert.That(error.ComponentLabel,
                Is.EqualTo("Small Tiles Widget"));
            Assert.That(error.Errors, Is.EqualTo(new[]
            {
                "No tiles have been selected."
            }));
        });

        VerifyNoDependencies();
    }

    [TestCase("null-model")]
    [TestCase("null-properties")]
    [TestCase("null-entry")]
    [TestCase("empty-guid-then-null-entry")]
    [Category(Gap)]
    public void InvalidNullInputs_DocumentMissingGuards(string scenario)
    {
        // Arrange
        var model = scenario switch
        {
            "null-model" => null,
            "null-properties" => Model(null),
            "null-entry" => Model(new SmallTilesWidgetProperties
            {
                Path = new ContentItemReference[] { null! }
            }),
            "empty-guid-then-null-entry" =>
                Model(new SmallTilesWidgetProperties
                {
                    Path = new ContentItemReference[]
                    {
                        new() { Identifier = Guid.Empty },
                        null!
                    }
                }),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };

        // Act / Assert: passing documents the crash, not safe handling.
        Assert.ThrowsAsync<NullReferenceException>(
            async () => { await widget.InvokeAsync(model!); });

        VerifyNoDependencies();
    }

    public static IEnumerable<TestCaseData> TextCases()
    {
        string?[] values =
        {
            null,
            "",
            " \t\r\n ",
            GenerateText(71),
            "日本語-é-مرحبا",
            "<script>alert(1)</script>",
            "%ZZ"
        };

        return values.Select(value =>
            new TestCaseData(new object?[] { value }));
    }

    [TestCaseSource(nameof(TextCases))]
    [Category(Pass)]
    public void ViewModel_CopiesTextAndCtaUrlUnchanged(string? value)
    {
        // Arrange
        var props = new SmallTilesWidgetProperties
        {
            Heading = value!,
            SectionId = value!,
            ShortDescription = value!
        };

        IReadOnlyList<SmallTileViewModel> tiles =
            Array.Empty<SmallTileViewModel>();

        // Act: the third argument is required.
        var model = new SmallTilesWidgetViewModel(
            props,
            tiles,
            value!);

        // Assert: these fields are copied, not normalized or sanitized.
        Assert.Multiple(() =>
        {
            Assert.That(model.Heading, Is.EqualTo(value));
            Assert.That(model.SectionId, Is.EqualTo(value));
            Assert.That(model.ShortDescription, Is.EqualTo(value));
            Assert.That(model.CtaButtonUrl, Is.EqualTo(value));
            Assert.That(model.Tiles, Is.SameAs(tiles));
        });
    }

    [TestCaseSource(nameof(TextCases))]
    [Category(Pass)]
    public void ViewModel_CtaButtonText_UsesActualFallback(string? value)
    {
        var props = new SmallTilesWidgetProperties
        {
            CtaButtonText = value!
        };

        var model = new SmallTilesWidgetViewModel(
            props,
            Array.Empty<SmallTileViewModel>(),
            "");

        Assert.That(model.CtaButtonText,
            Is.EqualTo(string.IsNullOrWhiteSpace(value)
                ? "View All"
                : value));
    }

    [Test]
    [Category(Pass)]
    public void ViewModel_PreservesGeneratedTilesAndCtaUrl()
    {
        var tile = new SmallTileViewModel(
            Text(),
            Text(),
            $"/images/{Text()}.jpg",
            $"/{Text()}",
            Text());

        IReadOnlyList<SmallTileViewModel> tiles = new[] { tile };
        var ctaUrl = $"/{Text()}";

        var model = new SmallTilesWidgetViewModel(
            new SmallTilesWidgetProperties(),
            tiles,
            ctaUrl);

        Assert.Multiple(() =>
        {
            Assert.That(model.Tiles, Is.SameAs(tiles));
            Assert.That(model.Tiles.Count, Is.EqualTo(1));
            Assert.That(model.Tiles[0], Is.SameAs(tile));
            Assert.That(model.CtaButtonUrl, Is.EqualTo(ctaUrl));
        });
    }

    [Test]
    [Category(Pass)]
    public void SmallTileViewModel_MapsDistinctGeneratedValues()
    {
        var caption = Text();
        var subcaption = Text();
        var imageUrl = $"/images/{Text()}.jpg";
        var ctaUrl = $"/{Text()}";
        var label = Text();

        var model = new SmallTileViewModel(
            caption, subcaption, imageUrl, ctaUrl, label);

        Assert.Multiple(() =>
        {
            Assert.That(model.Caption, Is.EqualTo(caption));
            Assert.That(model.SubCaption, Is.EqualTo(subcaption));
            Assert.That(model.ImageUrl, Is.EqualTo(imageUrl));
            Assert.That(model.CtaUrl, Is.EqualTo(ctaUrl));
            Assert.That(model.CtaLabel, Is.EqualTo(label));
        });
    }

    [TestCaseSource(nameof(TextCases))]
    [Category(Pass)]
    public void SmallTileViewModel_PreservesSubmittedValues(string? value)
    {
        var model = new SmallTileViewModel(
            value!, value!, value!, value!, value!);

        Assert.That(new[]
        {
            model.Caption,
            model.SubCaption,
            model.ImageUrl,
            model.CtaUrl,
            model.CtaLabel
        }, Is.EqualTo(Enumerable.Repeat(value, 5)));
    }

    [Test]
    [Category(Gap)]
    public void ViewModel_NullProperties_CurrentlyThrows()
    {
        Assert.Throws<NullReferenceException>(() =>
            new SmallTilesWidgetViewModel(
                null!,
                Array.Empty<SmallTileViewModel>(),
                ""));
    }

    [Test]
    [Category(Gap)]
    public void ViewModel_NullTiles_AreNotNormalized()
    {
        var model = new SmallTilesWidgetViewModel(
            new SmallTilesWidgetProperties(),
            null!,
            "");

        Assert.That(model.Tiles, Is.Null);
    }

    public static IEnumerable<TestCaseData> ThemeCases()
    {
        foreach (var name in Enum.GetNames<TileThemeOptions>())
            yield return new TestCaseData(name);

        foreach (var value in new string?[] { null, "", " ", "InvalidTheme" })
            yield return new TestCaseData(new object?[] { value });
    }

    [TestCaseSource(nameof(ThemeCases))]
    [Category(Pass)]
    public void ThemeProperty_PreservesSubmittedValue(string? theme)
    {
        var props = new SmallTilesWidgetProperties
        {
            Theme = theme!
        };

        Assert.That(props.Theme, Is.EqualTo(theme));
    }

    public static IEnumerable<TestCaseData> ValidThemes() =>
        Enum.GetValues<TileThemeOptions>()
            .Select(theme => new TestCaseData(theme));

    [TestCaseSource(nameof(ValidThemes))]
    [Category(Pass)]
    public void ViewModel_UsesSelectedTheme(TileThemeOptions theme)
    {
        var props = new SmallTilesWidgetProperties
        {
            Theme = theme.ToString()
        };

        var model = new SmallTilesWidgetViewModel(
            props,
            Array.Empty<SmallTileViewModel>(),
            "");

        Assert.That(model.Theme, Is.EqualTo(theme));
    }

    private void VerifyNoDependencies()
    {
        retriever.VerifyNoOtherCalls();
        urlRetriever.VerifyNoOtherCalls();
        preferredLanguage.VerifyNoOtherCalls();
    }

    private string Text() => CreateText(random);

    private static string GenerateText(int seed) =>
        CreateText(new Random(seed));

    private static string CreateText(Random generator) => new(
        Enumerable.Range(0, 16)
            .Select(_ => (char)('a' + generator.Next(26)))
            .ToArray());

    private static ContentItemReference[] References(params Guid[] ids) =>
        ids.Select(id => new ContentItemReference
        {
            Identifier = id
        }).ToArray();

    // Retains your existing test-only fixture technique.
    // Only Properties is initialized; no private widget methods are tested.
    private static ComponentViewModel<SmallTilesWidgetProperties> Model(
        SmallTilesWidgetProperties? props)
    {
        var type =
            typeof(ComponentViewModel<SmallTilesWidgetProperties>);

        var component =
            (ComponentViewModel<SmallTilesWidgetProperties>)
            RuntimeHelpers.GetUninitializedObject(type);

        const BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.DeclaredOnly;

        for (Type? current = type;
             current != null;
             current = current.BaseType)
        {
            var setter = current.GetProperty("Properties", flags)?
                .GetSetMethod(true);

            if (setter != null)
            {
                setter.Invoke(component, new object?[] { props });
                VerifyProperties();
                return component;
            }

            var field = current.GetField(
                "<Properties>k__BackingField", flags);

            if (field != null)
            {
                field.SetValue(component, props);
                VerifyProperties();
                return component;
            }
        }

        throw new InvalidOperationException(
            "Unsupported Kentico Properties storage. " +
            "This SDK version requires a different component fixture.");

        void VerifyProperties()
        {
            if (!ReferenceEquals(component.Properties, props))
                throw new InvalidOperationException(
                    "Component.Properties was not initialized correctly.");
        }
    }
}