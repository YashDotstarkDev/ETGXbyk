#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ETG.Web.Components;
using ETG.Web.Components.PageBuilder.Widgets.YoutubeVideo;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using NUnit.Framework;

namespace ETG.Web.Tests.Components.PageBuilder.Widgets;

[TestFixture]
public class YoutubeVideoWidgetAdditionalTests : BaseWebTest
{
    private const string ValidId = "dQw4w9WgXcQ";

    private const string SuccessView =
        "~/Components/PageBuilder/Widgets/YoutubeVideo/YoutubeVideo.cshtml";

    private const string ErrorView =
        "~/Components/ComponentError.cshtml";

    private YoutubeVideoWidget widget = null!;

    public override void Setup()
    {
        base.Setup();
        widget = new YoutubeVideoWidget();
    }

    private static PropertyInfo[] EditableProperties()
    {
        return typeof(YoutubeVideoWidgetProperties)
            .GetProperties(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly)
            .Where(p =>
                p.CanRead &&
                p.CanWrite &&
                p.GetIndexParameters().Length == 0)
            .OrderBy(p => p.Name)
            .ToArray();
    }

    public static IEnumerable<TestCaseData> PropertyCases()
    {
        foreach (var property in EditableProperties())
        {
            if (property.PropertyType == typeof(string))
            {
                string?[] values =
                {
                    null,
                    "",
                    " \t\r\n ",
                    "Sample content",
                    "日本語-é-مرحبا",
                    "<script>alert(1)</script>",
                    "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                    new string('x', 10000)
                };

                for (var i = 0; i < values.Length; i++)
                {
                    yield return new TestCaseData(
                            property.Name,
                            values[i])
                        .SetName(
                            $"Invoke_{property.Name}_StringInput_{i}");
                }
            }
            else if (property.PropertyType == typeof(bool))
            {
                foreach (var value in new[] { false, true })
                {
                    yield return new TestCaseData(
                            property.Name,
                            value)
                        .SetName(
                            $"Invoke_{property.Name}_{value}");
                }
            }
        }
    }

    [Test]
    public void DiscoveredProperties_UseSupportedTestTypes()
    {
        var properties = EditableProperties();

        Assert.That(properties, Is.Not.Empty);

        var unsupported = properties
            .Where(p =>
                p.PropertyType != typeof(string) &&
                p.PropertyType != typeof(bool))
            .Select(p => $"{p.Name}: {p.PropertyType.Name}")
            .ToArray();

        Assert.That(
            unsupported,
            Is.Empty,
            "Add input generation for these property types: " +
            string.Join(", ", unsupported));
    }

    [Test]
    public async Task ValidProperties_ReturnExpectedView()
    {
        var props = ValidProperties();

        var result = await widget.InvokeAsync(Component(props));
        var model = AssertSuccess(result);

        AssertMatchingProperties(props, model);
    }

    [TestCaseSource(nameof(PropertyCases))]
    public async Task PropertyInputs_FollowValidationAndCopyValues(
        string propertyName,
        object? value)
    {
        var props = ValidProperties();

        var property = typeof(YoutubeVideoWidgetProperties)
            .GetProperty(propertyName);

        Assert.That(property, Is.Not.Null);

        property!.SetValue(props, value);

        var result = await widget.InvokeAsync(Component(props));

        // The widget currently validates only whether the ID is blank.
        if (string.IsNullOrWhiteSpace(props.YoutubeID))
        {
            AssertError(result);
            return;
        }

        var model = AssertSuccess(result);

        AssertMatchingProperties(props, model);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase("\t\r\n")]
    public async Task BlankYoutubeId_ReturnsRequiredError(string? id)
    {
        var props = ValidProperties();
        props.YoutubeID = id!;

        var result = await widget.InvokeAsync(Component(props));

        AssertError(result);
    }

    [Test]
    public void ViewModel_CopiesAllDiscoveredProperties()
    {
        var props = ValidProperties();
        var model = new YoutubeVideoWidgetViewModel(props);

        AssertMatchingProperties(props, model);
    }

    [TestCase(true)]
    [TestCase(false)]
    [Category("CurrentBehavior")]
    public void NullInputs_CurrentlyThrow(bool nullComponent)
    {
        var component = nullComponent ? null : Component(null);

        Assert.ThrowsAsync<NullReferenceException>(
            async () =>
            {
                await widget.InvokeAsync(component!);
            });
    }

    private static YoutubeVideoWidgetProperties ValidProperties()
    {
        var props = new YoutubeVideoWidgetProperties();

        foreach (var property in EditableProperties())
        {
            if (property.PropertyType == typeof(string))
            {
                property.SetValue(
                    props,
                    $"Test-{property.Name}");
            }
            else if (property.PropertyType == typeof(bool))
            {
                property.SetValue(props, true);
            }
        }

        // Explicit valid baseline for the widget's required field.
        props.YoutubeID = ValidId;

        return props;
    }

    private static void AssertMatchingProperties(
        YoutubeVideoWidgetProperties props,
        YoutubeVideoWidgetViewModel model)
    {
        var properties = EditableProperties();

        Assert.That(properties, Is.Not.Empty);

        Assert.Multiple(() =>
        {
            foreach (var source in properties)
            {
                var target = typeof(YoutubeVideoWidgetViewModel)
                    .GetProperty(
                        source.Name,
                        BindingFlags.Public |
                        BindingFlags.Instance);

                Assert.That(
                    target,
                    Is.Not.Null,
                    $"View model is missing '{source.Name}'.");

                if (target == null)
                {
                    continue;
                }

                Assert.That(
                    target.CanRead,
                    Is.True,
                    $"View-model property '{source.Name}' is unreadable.");

                Assert.That(
                    target.PropertyType,
                    Is.EqualTo(source.PropertyType),
                    $"Type mismatch for '{source.Name}'.");

                if (target.CanRead)
                {
                    Assert.That(
                        target.GetValue(model),
                        Is.EqualTo(source.GetValue(props)),
                        $"Value mismatch for '{source.Name}'.");
                }
            }
        });
    }

    private static YoutubeVideoWidgetViewModel AssertSuccess(
        IViewComponentResult result)
    {
        Assert.That(
            result,
            Is.TypeOf<ViewViewComponentResult>());

        var view = (ViewViewComponentResult)result;

        Assert.Multiple(() =>
        {
            Assert.That(
                view.ViewName,
                Is.EqualTo(SuccessView));

            Assert.That(
                view.ViewData.Model,
                Is.TypeOf<YoutubeVideoWidgetViewModel>());
        });

        return (YoutubeVideoWidgetViewModel)view.ViewData.Model!;
    }

    private static void AssertError(IViewComponentResult result)
    {
        Assert.That(
            result,
            Is.TypeOf<ViewViewComponentResult>());

        var view = (ViewViewComponentResult)result;

        Assert.That(view.ViewName, Is.EqualTo(ErrorView));

        Assert.That(
            view.ViewData.Model,
            Is.TypeOf<ComponentErrorViewModel>());

        var error =
            (ComponentErrorViewModel)view.ViewData.Model!;

        Assert.That(
            error.Errors,
            Is.EqualTo(new[] { "YouTube ID is required." }));
    }

    private static ComponentViewModel<YoutubeVideoWidgetProperties>
        Component(YoutubeVideoWidgetProperties? props)
    {
        var type =
            typeof(ComponentViewModel<YoutubeVideoWidgetProperties>);

        // Test-only fallback for Kentico's component wrapper.
        // Prefer a supported constructor/factory when available.
        var component =
            (ComponentViewModel<YoutubeVideoWidgetProperties>)
            RuntimeHelpers.GetUninitializedObject(type);

        for (Type? current = type;
             current != null;
             current = current.BaseType)
        {
            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly;

            var property =
                current.GetProperty("Properties", flags);

            var setter = property?.GetSetMethod(true);

            if (setter != null)
            {
                setter.Invoke(
                    component,
                    new object?[] { props });

                Verify();
                return component;
            }

            var field = current.GetField(
                "<Properties>k__BackingField",
                flags);

            if (field != null)
            {
                field.SetValue(component, props);

                Verify();
                return component;
            }
        }

        throw new InvalidOperationException(
            "Cannot initialize ComponentViewModel.Properties. " +
            "Use your Kentico version's supported constructor " +
            "or the project's component-model factory.");

        void Verify()
        {
            if (!ReferenceEquals(component.Properties, props))
            {
                throw new InvalidOperationException(
                    "ComponentViewModel.Properties was not " +
                    "initialized correctly.");
            }
        }
    }
}