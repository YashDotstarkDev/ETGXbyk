#nullable enable

using CMS.ContentEngine;
using ETG.Core.Components;
using ETG.Web.Components;
using ETG.Web.Components.PageBuilder.Widgets.TwoSideBySideImage;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ETG.Web.Tests.Components.PageBuilder.Widgets;

[TestFixture]
public class TwoSideBySideImageWidgetAdditionalTests : BaseWebTest
{
    private const string CaptionError = "Caption 1 is required.";
    private const string ImageError =
        "Image 1 is required and must contain a valid image asset.";

    private Mock<IContentRetriever> retriever = null!;
    private TwoSideBySideImageWidget widget = null!;

    public override void Setup()
    {
        base.Setup();
        retriever = new Mock<IContentRetriever>(MockBehavior.Strict);
        widget = new TwoSideBySideImageWidget(retriever.Object);
    }

    [Test]
    public void IdentifierAndName_AreCorrect()
    {
        Assert.Multiple(() =>
        {
            Assert.That(TwoSideBySideImageWidget.IDENTIFIER,
                Is.EqualTo("ETG.Web.Widget.TwoSideBySideImage"));
            Assert.That(TwoSideBySideImageWidget.NAME,
                Is.EqualTo("Two Side By Side Image"));
        });
    }

    [TestCase("null")]
    [TestCase("empty")]
    [TestCase("emptyGuid")]
    [Category("CurrentBehavior")]
    public async Task NoUsableFirstImage_ReturnsImageError(string scenario)
    {
        var props = new TwoSideBySideImageWidgetProperties
        {
            Caption1 = "Valid caption",
            Image1 = scenario switch
            {
                "null" => null!,
                "empty" => Array.Empty<ContentItemReference>(),
                _ => new[]
                {
                    new ContentItemReference { Identifier = Guid.Empty }
                }
            },
            Image2 = null!,
            Image3 = null!
        };

        AssertError(await widget.InvokeAsync(Model(props)), ImageError);
        retriever.VerifyNoOtherCalls();
    }

    private static IEnumerable<TestCaseData> StringCases()
    {
        var properties = typeof(TwoSideBySideImageWidgetProperties)
            .GetProperties(BindingFlags.Public |
                           BindingFlags.Instance |
                           BindingFlags.DeclaredOnly)
            .Where(p => p.PropertyType == typeof(string) && p.CanWrite)
            .OrderBy(p => p.Name);

        foreach (var property in properties)
        {
            yield return new TestCaseData(
                property.Name, "<script>alert(1)</script>")
                .SetName($"Strings_{property.Name}_UnexpectedValue");
        }

        yield return new TestCaseData("Caption1", null);
        yield return new TestCaseData("Caption1", "");
        yield return new TestCaseData("Caption1", " \t\r\n");
    }

    [TestCaseSource(nameof(StringCases))]
    [Category("CurrentBehavior")]
    public async Task StringInputs_RespectValidationAndAreCopied(
        string propertyName, string? value)
    {
        var props = new TwoSideBySideImageWidgetProperties
        {
            Caption1 = "Valid caption"
        };

        typeof(TwoSideBySideImageWidgetProperties)
            .GetProperty(propertyName)!.SetValue(props, value);

        var expected = propertyName == "Caption1" &&
                       string.IsNullOrWhiteSpace(value)
            ? CaptionError
            : ImageError;

        AssertError(await widget.InvokeAsync(Model(props)), expected);
        retriever.VerifyNoOtherCalls();

        // Constructor copying, not successful InvokeAsync rendering.
        var vm = new TwoSideBySideImageWidgetViewModel(
            null!, null, null, props);

        var output = typeof(TwoSideBySideImageWidgetViewModel)
            .GetProperty(propertyName);

        Assert.That(output, Is.Not.Null);
        Assert.That(output!.GetValue(vm), Is.EqualTo(value));
    }

    [TestCase("model")]
    [TestCase("properties")]
    [Category("ValidationGap")]
    public void NullModelOrProperties_Throws(string scenario)
    {
        var model = scenario == "model" ? null! : Model(null);

        Assert.ThrowsAsync<NullReferenceException>(
            async () => { await widget.InvokeAsync(model); });

        retriever.VerifyNoOtherCalls();
    }

    [TestCase("Image1")]
    [TestCase("Image2")]
    [TestCase("Image3")]
    [Category("ValidationGap")]
    public void NullImageEntry_Throws(string propertyName)
    {
        var props = new TwoSideBySideImageWidgetProperties();

        typeof(TwoSideBySideImageWidgetProperties)
            .GetProperty(propertyName)!
            .SetValue(props, new ContentItemReference[] { null! });

        Assert.ThrowsAsync<NullReferenceException>(
            async () => { await widget.InvokeAsync(Model(props)); });

        retriever.VerifyNoOtherCalls();
    }

    [TestCase(false)]
    [TestCase(true)]
    [Category("ViewModelCurrentBehavior")]
    public void HasSidePadding_IsCopied(bool value)
    {
        var vm = new TwoSideBySideImageWidgetViewModel(
            null!, null, null,
            new TwoSideBySideImageWidgetProperties
            {
                HasSidePadding = value
            });

        Assert.That(vm.HasSidePadding, Is.EqualTo(value));
    }

    private static void AssertError(
        IViewComponentResult result, string message)
    {
        Assert.That(result, Is.TypeOf<ViewViewComponentResult>());
        var view = (ViewViewComponentResult)result;

        Assert.That(view.ViewName,
            Is.EqualTo("~/Components/ComponentError.cshtml"));
        Assert.That(view.ViewData.Model,
            Is.TypeOf<ComponentErrorViewModel>());

        var error = (ComponentErrorViewModel)view.ViewData.Model!;
        Assert.Multiple(() =>
        {
            Assert.That(error.ComponentLabel,
                Is.EqualTo("Two Side By Side Image Widget"));
            Assert.That(error.Errors, Is.EqualTo(new[] { message }));
        });
    }

    private static ComponentViewModel<TwoSideBySideImageWidgetProperties>
        Model(TwoSideBySideImageWidgetProperties? props)
    {
        var type =
            typeof(ComponentViewModel<TwoSideBySideImageWidgetProperties>);
        const BindingFlags flags = BindingFlags.Public |
                                   BindingFlags.NonPublic |
                                   BindingFlags.Instance;

        foreach (var constructor in type.GetConstructors(flags))
        {
            var parameters = constructor.GetParameters();
            var index = Array.FindIndex(parameters,
                p => p.ParameterType ==
                     typeof(TwoSideBySideImageWidgetProperties));

            if (index < 0 || parameters.Where((_, i) => i != index)
                    .Any(p => !p.HasDefaultValue))
                continue;

            var args = parameters
                .Select(p => p.HasDefaultValue ? p.DefaultValue : null)
                .ToArray();
            args[index] = props;

            return (ComponentViewModel<TwoSideBySideImageWidgetProperties>)
                constructor.Invoke(args);
        }

        var empty = type.GetConstructor(
            flags, null, Type.EmptyTypes, null);
        var setter = type.GetProperty("Properties")
            ?.GetSetMethod(nonPublic: true);

        if (empty is not null && setter is not null)
        {
            var model =
                (ComponentViewModel<TwoSideBySideImageWidgetProperties>)
                empty.Invoke(Array.Empty<object>());
            setter.Invoke(model, new object?[] { props });
            return model;
        }

        throw new InvalidOperationException(
            "Use the project's existing ComponentViewModel factory.");
    }
}