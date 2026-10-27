using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CMS.ContentEngine;
using CSharpFunctionalExtensions;
using ETG.Core.Content;
using ETG.Web.Components;
using ETG.Web.Components.PageBuilder.Widgets.FourColumnIconCardGrid;
using NUnit.Framework;

using Props = ETG.Web.Components.PageBuilder.Widgets
    .FourColumnIconCardGrid.FourColumnIconCardGridWidgetProperties;
using VM = ETG.Web.Components.PageBuilder.Widgets
    .FourColumnIconCardGrid.FourColumnIconCardGridWidgetViewModel;

namespace ETG.Web.Tests.Components.PageBuilder.Widgets;

// Tests private validation, not InvokeAsync, retrieval, or rendering.
// KnownValidationGap tests document acceptance, not correct rejection.
// Not compiled or executed here.
[TestFixture]
public class FourColumnIconCardGridWidgetTests : BaseWebTest
{
    [Test]
    public void MetadataAndDefaults_Match()
    {
        var props = new Props();

        Assert.Multiple(() =>
        {
            Assert.That(FourColumnIconCardGridWidget.IDENTIFIER,
                Is.EqualTo("ETG.Web.Widget.FourColumnIconCardGrid"));
            Assert.That(FourColumnIconCardGridWidget.NAME,
                Is.EqualTo("Four Column Icon Card Grid"));
            Assert.That(props.Heading, Is.Empty);
            Assert.That(props.ShortDescription, Is.Empty);
            Assert.That(props.FooterText, Is.Empty);
            Assert.That(props.FeaturesCards, Is.Not.Null.And.Empty);
        });
    }

    [TestCase(1, null)]
    [TestCase(2, "")]
    [TestCase(3, " \t\r\n ")]
    [TestCase(4, "Generated")]
    public void ValidSelection_ReturnsExpectedModel(int count, string? text)
    {
        var value = text == "Generated" ? Guid.NewGuid().ToString() : text;
        var props = Properties(count);
        props.Heading = props.ShortDescription = props.FooterText = value!;
        var cards = Cards(count);

        var result = Validate(props, cards);

        Assert.That(result.IsSuccess, Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.TypeOf<VM>());
            Assert.That(result.Value.Heading, Is.EqualTo(value));
            Assert.That(result.Value.ShortDescription, Is.EqualTo(value));
            Assert.That(result.Value.FooterText, Is.EqualTo(value));
            Assert.That(result.Value.FeaturesCards, Is.SameAs(cards));
        });
    }

    [TestCase("null-selection")]
    [TestCase("empty-selection")]
    [TestCase("null-results")]
    [TestCase("empty-results")]
    public void MissingContent_ReturnsValidationError(string scenario)
    {
        var props = Properties(1);
        IEnumerable<Card> cards = Cards(1);

        switch (scenario)
        {
            case "null-selection":
                props.FeaturesCards = null!;
                break;
            case "empty-selection":
                props.FeaturesCards = Array.Empty<ContentItemReference>();
                break;
            case "null-results":
                cards = null!;
                break;
            case "empty-results":
                cards = Array.Empty<Card>();
                break;
        }

        var result = Validate(props, cards);

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.Not.Null);
    }

    [TestCase("empty-guid")]
    [TestCase("duplicate")]
    [TestCase("over-limit")]
    [TestCase("partial-results")]
    [Category("KnownValidationGap")]
    public void TamperedSelection_CurrentlyAcceptedByValidation(string scenario)
    {
        var props = Properties(scenario == "over-limit" ? 5 : 2);

        if (scenario == "empty-guid")
        {
            props.FeaturesCards = new[]
            {
                new ContentItemReference { Identifier = Guid.Empty }
            };
        }

        if (scenario == "duplicate")
        {
            var reference = props.FeaturesCards.First();
            props.FeaturesCards = new[] { reference, reference };
        }

        var cards = Cards(
            scenario == "partial-results" ? 1 : props.FeaturesCards.Count());

        var result = Validate(props, cards);

        Assert.That(result.IsSuccess, Is.True,
            "Documents current acceptance, not safe rejection.");
        Assert.That(result.Value.FeaturesCards, Is.SameAs(cards));
    }

    private static Props Properties(int count) => new()
    {
        FeaturesCards = Enumerable.Range(0, count)
            .Select(_ => new ContentItemReference
            {
                Identifier = Guid.NewGuid()
            })
            .ToArray()
    };

    // Minimal objects for validation only, not GUID-matched content fixtures.
    private static Card[] Cards(int count) =>
        Enumerable.Range(0, count)
            .Select(_ => new Card())
            .ToArray();

    private static Result<VM, ComponentErrorViewModel> Validate(
        Props props, IEnumerable<Card> cards)
    {
        var method = typeof(FourColumnIconCardGridWidget).GetMethod(
            "Validate",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(
                nameof(FourColumnIconCardGridWidget), "Validate");

        return (Result<VM, ComponentErrorViewModel>)method.Invoke(
            null, new object[] { props, cards })!;
    }
}