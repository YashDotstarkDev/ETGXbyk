#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CMS.ContentEngine;
using ETG.Web.Components.PageBuilder.Widgets.Testimonial;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Moq;
using NUnit.Framework;

namespace ETG.Web.Tests.Components.PageBuilder.Widgets;

[TestFixture]
public class TestimonialWidgetTests : BaseWebTest
{
    private const string SuccessView =
        "~/Components/PageBuilder/Widgets/Testimonial/Testimonial.cshtml";

    private Mock<IContentRetriever> contentRetriever = null!;
    private TestimonialWidget widget = null!;

    public override void Setup()
    {
        base.Setup();

        // RetrieveContentByGuids is an extension method:
        // do not use it in Moq Setup or Verify expressions.
        contentRetriever =
            new Mock<IContentRetriever>(MockBehavior.Strict);

        widget = new TestimonialWidget(contentRetriever.Object);
    }

    [Test]
    [Category("CurrentBehavior")]
    public async Task DefaultProperties_ReturnSuccessWithDefaultValues()
    {
        var vm = AssertSuccess(
            await widget.InvokeAsync(Model(new TestimonialProperties())));

        Assert.Multiple(() =>
        {
            Assert.That(vm.Image, Is.Null);
            Assert.That(vm.Heading, Is.Empty);
            Assert.That(vm.TextContent, Is.Empty);
            Assert.That(vm.Date, Is.Empty);
            Assert.That(vm.Source, Is.Empty);
            Assert.That(vm.SourceLocation, Is.Empty);
            Assert.That(vm.CTALabel, Is.Empty);
            Assert.That(vm.Path, Is.Empty);
        });

        contentRetriever.VerifyNoOtherCalls();
    }

    [Test]
    [Category("CurrentBehavior")]
    public async Task ValidProperties_PreserveAllValues()
    {
        var props = new TestimonialProperties
        {
            Heading = "Customer testimonial",
            TextContent = "The service was excellent.",
            Date = "15 October 2026",
            Source = "Jane Smith",
            SourceLocation = "London",
            CTALabel = "Read the story",
            Path = "/customer-stories/jane"
        };

        var vm = AssertSuccess(await widget.InvokeAsync(Model(props)));

        Assert.Multiple(() =>
        {
            Assert.That(vm.Image, Is.Null);
            Assert.That(vm.Heading, Is.EqualTo(props.Heading));
            Assert.That(vm.TextContent, Is.EqualTo(props.TextContent));
            Assert.That(vm.Date, Is.EqualTo(props.Date));
            Assert.That(vm.Source, Is.EqualTo(props.Source));
            Assert.That(vm.SourceLocation, Is.EqualTo(props.SourceLocation));
            Assert.That(vm.CTALabel, Is.EqualTo(props.CTALabel));
            Assert.That(vm.Path, Is.EqualTo(props.Path));
        });

        contentRetriever.VerifyNoOtherCalls();
    }

    [TestCase("null")]
    [TestCase("empty")]
    [TestCase("emptyGuid")]
    [Category("CurrentBehavior")]
    public async Task NoUsableImageReferences_ReturnSuccessWithoutRetrieval(
        string scenario)
    {
        var props = new TestimonialProperties
        {
            Images = scenario switch
            {
                "null" => null!,
                "empty" => Array.Empty<ContentItemReference>(),
                "emptyGuid" => new[] { Reference(Guid.Empty) },
                _ => throw new ArgumentOutOfRangeException(nameof(scenario))
            }
        };

        var vm = AssertSuccess(await widget.InvokeAsync(Model(props)));

        Assert.That(vm.Image, Is.Null);
        contentRetriever.VerifyNoOtherCalls();
    }

    // One dynamically generated case per declared string property.
    private static IEnumerable<TestCaseData> StringCases()
    {
        var properties = typeof(TestimonialProperties)
            .GetProperties(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly)
            .Where(p =>
                p.PropertyType == typeof(string) &&
                p.CanWrite &&
                p.GetIndexParameters().Length == 0)
            .OrderBy(p => p.Name);

        foreach (var property in properties)
        {
            var value = property.Name switch
            {
                nameof(TestimonialProperties.Path) =>
                    "javascript:alert(1)",

                nameof(TestimonialProperties.Date) =>
                    "not-a-date",

                _ => "<script>alert(1)</script>"
            };

            yield return new TestCaseData(property.Name, value)
                .SetName(
                    $"InvokeAsync_{property.Name}_PreservesSubmittedValue");
        }
    }

    [TestCaseSource(nameof(StringCases))]
    [Category("CurrentBehavior")]
    public async Task UnexpectedStringValue_IsPassedThrough(
        string propertyName,
        string submittedValue)
    {
        var props = new TestimonialProperties();

        var inputProperty = typeof(TestimonialProperties)
            .GetProperty(propertyName);

        Assert.That(inputProperty, Is.Not.Null);
        inputProperty!.SetValue(props, submittedValue);

        var vm = AssertSuccess(await widget.InvokeAsync(Model(props)));

        var outputProperty = typeof(TestimonialWidgetViewModel)
            .GetProperty(propertyName);

        Assert.That(
            outputProperty,
            Is.Not.Null,
            $"The view model must expose '{propertyName}'.");

        Assert.That(
            outputProperty!.GetValue(vm),
            Is.EqualTo(submittedValue));

        contentRetriever.VerifyNoOtherCalls();
    }

    // These tests document current exceptions, not proposed safe behavior.

    [Test]
    [Category("ValidationGap")]
    public void NullComponentModel_ThrowsNullReferenceException()
    {
        Assert.ThrowsAsync<NullReferenceException>(
            async () => { await widget.InvokeAsync(null!); });

        contentRetriever.VerifyNoOtherCalls();
    }

    [Test]
    [Category("ValidationGap")]
    public void NullProperties_ThrowsNullReferenceException()
    {
        var model = Model(null);

        Assert.ThrowsAsync<NullReferenceException>(
            async () => { await widget.InvokeAsync(model); });

        contentRetriever.VerifyNoOtherCalls();
    }

    [Test]
    [Category("ValidationGap")]
    public void NullImageEntry_ThrowsNullReferenceException()
    {
        var props = new TestimonialProperties
        {
            Images = new ContentItemReference[] { null! }
        };

        Assert.ThrowsAsync<NullReferenceException>(
            async () => { await widget.InvokeAsync(Model(props)); });

        contentRetriever.VerifyNoOtherCalls();
    }

    private static TestimonialWidgetViewModel AssertSuccess(
        IViewComponentResult result)
    {
        Assert.That(result, Is.TypeOf<ViewViewComponentResult>());

        var view = (ViewViewComponentResult)result;

        Assert.Multiple(() =>
        {
            Assert.That(view.ViewName, Is.EqualTo(SuccessView));
            Assert.That(
                view.ViewData.Model,
                Is.TypeOf<TestimonialWidgetViewModel>());
        });

        return (TestimonialWidgetViewModel)view.ViewData.Model!;
    }

    private static ContentItemReference Reference(Guid id)
    {
        return new ContentItemReference { Identifier = id };
    }

    // Retains the factory pattern from your supplied test file.
    private static ComponentViewModel<TestimonialProperties> Model(
        TestimonialProperties? props)
    {
        var type = typeof(ComponentViewModel<TestimonialProperties>);

        const BindingFlags flags =
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.Instance;

        foreach (var constructor in type.GetConstructors(flags))
        {
            var parameters = constructor.GetParameters();

            var index = Array.FindIndex(
                parameters,
                p => p.ParameterType == typeof(TestimonialProperties));

            if (index < 0 ||
                parameters.Where((_, i) => i != index)
                    .Any(p => !p.HasDefaultValue))
            {
                continue;
            }

            var arguments = parameters
                .Select(p => p.HasDefaultValue ? p.DefaultValue : null)
                .ToArray();

            arguments[index] = props;

            return (ComponentViewModel<TestimonialProperties>)
                constructor.Invoke(arguments);
        }

        var emptyConstructor = type.GetConstructor(
            flags,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);

        var setter = type.GetProperty("Properties")
            ?.GetSetMethod(nonPublic: true);

        if (emptyConstructor is not null && setter is not null)
        {
            var model = (ComponentViewModel<TestimonialProperties>)
                emptyConstructor.Invoke(Array.Empty<object>());

            setter.Invoke(model, new object?[] { props });
            return model;
        }

        throw new InvalidOperationException(
            "Use the project's existing ComponentViewModel factory.");
    }
}