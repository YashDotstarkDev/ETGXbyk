using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using CMS.ContentEngine;
using CSharpFunctionalExtensions;
using ETG.Core.Content;
using ETG.Web.Components;
using ETG.Web.Components.PageBuilder.Widgets.FullWidthImage;
using NUnit.Framework;

namespace ETG.Web.Tests.Components.PageBuilder.Widgets;

// Tests private validation and properties, not InvokeAsync or rendering.
// The null-collection test documents the existing bug, not safe handling.
[TestFixture]
public class FullWidthImageWidgetTests : BaseWebTest
{
    [Test]
    public void MetadataAndDefaults_Match()
    {
        var props = new FullWidthImageProperties();

        Assert.Multiple(() =>
        {
            Assert.That(FullWidthImageWidget.IDENTIFIER,
                Is.EqualTo("ETG.Web.Widget.FullWidthImage"));
            Assert.That(FullWidthImageWidget.NAME,
                Is.EqualTo("Full width Image"));
            Assert.That(props.Image, Is.Not.Null.And.Empty);
            Assert.That(props.Caption, Is.Empty);
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase("\t\r\n")]
    [TestCase("<script>alert(1)</script>")]
    public void Caption_PreservesInput(string? caption)
    {
        var props = Properties();
        props.Caption = caption!;

        Assert.That(props.Caption, Is.EqualTo(caption));
    }

    [Test]
    public void Caption_PreservesGeneratedInput()
    {
        var caption = $"Caption-{Guid.NewGuid():N}";
        var props = Properties();
        props.Caption = caption;

        Assert.That(props.Caption, Is.EqualTo(caption));
    }

    [TestCase("empty-selection")]
    [TestCase("null-entry")]
    [TestCase("empty-guid")]
    [TestCase("missing-media")]
    [TestCase("null-asset")]
    public void Validate_InvalidMedia_ReturnsFailure(string scenario)
    {
        var props = Properties();
        var media = Maybe<MediaFile>.None;

        switch (scenario)
        {
            case "empty-selection":
                props.Image = Array.Empty<ContentItemReference>();
                break;

            case "null-entry":
                props.Image = new ContentItemReference[] { null! };
                break;

            case "empty-guid":
                props.Image = new[]
                {
                    new ContentItemReference { Identifier = Guid.Empty }
                };
                break;

            case "null-asset":
                media = Maybe.From(
                    new MediaFile { LegacyMediaFileAsset = null });
                break;
        }

        var result = Validate(props, media);

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.Not.Null);
    }

    [Test]
    [Category("KnownValidationGap")]
    public void Validate_NullCollection_CurrentlyThrowsArgumentNullException()
    {
        var props = Properties();
        props.Image = null!;

        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            Validate(props, Maybe<MediaFile>.None);
        });

        Assert.That(exception!.ParamName, Is.EqualTo("source"));
    }

    private static FullWidthImageProperties Properties() => new()
    {
        Image = new[]
        {
            new ContentItemReference { Identifier = Guid.NewGuid() }
        },
        Caption = $"Caption-{Guid.NewGuid():N}"
    };

    private static Result<
        FullWidthImageWidgetViewModel,
        ComponentErrorViewModel> Validate(
            FullWidthImageProperties props,
            Maybe<MediaFile> image)
    {
        var method = typeof(FullWidthImageWidget).GetMethod(
            "Validate",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(
                nameof(FullWidthImageWidget), "Validate");

        try
        {
            return (Result<
                FullWidthImageWidgetViewModel,
                ComponentErrorViewModel>)method.Invoke(
                    null, new object[] { props, image })!;
        }
        catch (TargetInvocationException ex)
            when (ex.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }
}