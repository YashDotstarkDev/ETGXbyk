using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using ETG.Web.Components;
using ETG.Web.Components.PageBuilder.Widgets.IconsContent;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using NUnit.Framework;

namespace ETG.Web.Tests.Components.PageBuilder.Widgets;

[TestFixture]
public class IconsContentWidgetTests : BaseWebTest
{
    [Test, Order(0)]
    public void IdentifierAndDefaults_Match()
    {
        var props = new IconsContentWidgetProperties();

        Assert.That(IconsContentWidget.IDENTIFIER,
            Is.EqualTo("ETG.Web.Widget.IconsContent"));

        foreach (var field in Fields())
            Assert.That(Read(props, field), Is.EqualTo(""), field);
    }

    [Test]
    public void ValidProperties_MapAllFields()
    {
        AssertMapping(ValidProperties());
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" \t\r\n ")]
    public void MissingHeading_ReturnsExactError(string? heading)
    {
        var props = ValidProperties();
        props.Heading = heading!;

        var view = Invoke(Component(props));

        Assert.That(view.ViewName,
            Is.EqualTo("~/Components/ComponentError.cshtml"));
        Assert.That(view.ViewData.Model,
            Is.TypeOf<ComponentErrorViewModel>());

        var error = (ComponentErrorViewModel)view.ViewData.Model!;
        Assert.That(error.ComponentLabel, Is.EqualTo("Icons Content Widget"));
        Assert.That(error.Errors, Is.EqualTo(new[] { "Heading is required." }));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" \t\r\n ")]
    public void OptionalFields_AreAccepted(string? value)
    {
        // Tests each optional field independently within one NUnit case.
        foreach (var field in Fields())
        {
            if (field == nameof(IconsContentWidgetProperties.Heading))
                continue;

            var props = ValidProperties();
            Set(props, field, value);
            AssertMapping(props);
        }
    }

    [Test]
    [Category("ValidationBoundary")]
    public void UnexpectedInputs_CurrentlyAccepted()
    {
        var token = Guid.NewGuid().ToString("N");
        var inputs = new[]
        {
            $"日本語-é & < > \" ' {token}",
            new string('x', 100_000) + token,
            $"<script>alert('{token}')</script>",
            $"\" onmouseover=\"alert('{token}')"
        };

        foreach (var field in Fields())
            foreach (var input in inputs)
            {
                var props = ValidProperties();
                Set(props, field, input);
                AssertMapping(props);
            }

        // Acceptance does not prove safe rendering or sanitization.
    }

    [Test]
    [Category("KnownValidationGap")]
    public void NullInputs_CurrentlyThrow()
    {
        var widget = new IconsContentWidget();

        Assert.Throws<NullReferenceException>(() =>
        {
            widget.Invoke(null!);
        });

        var component = Component(null);
        Assert.Throws<NullReferenceException>(() =>
        {
            widget.Invoke(component);
        });
    }

    private static void AssertMapping(IconsContentWidgetProperties props)
    {
        var view = Invoke(Component(props));

        Assert.That(view.ViewName, Is.EqualTo(
            "~/Components/PageBuilder/Widgets/IconsContent/IconsContent.cshtml"));
        Assert.That(view.ViewData.Model,
            Is.TypeOf<IconsContentWidgetViewModel>());

        Assert.Multiple(() =>
        {
            foreach (var field in Fields())
                Assert.That(Read(view.ViewData.Model!, field),
                    Is.EqualTo(Read(props, field)), field);
        });
    }

    private static ViewViewComponentResult Invoke(
        ComponentViewModel<IconsContentWidgetProperties> component)
    {
        var result = new IconsContentWidget().Invoke(component);
        Assert.That(result, Is.TypeOf<ViewViewComponentResult>());
        return (ViewViewComponentResult)result;
    }

    private static IEnumerable<string> Fields()
    {
        yield return nameof(IconsContentWidgetProperties.Heading);

        for (var i = 1; i <= 4; i++)
            foreach (var suffix in new[] { "Icon", "Title", "Description" })
                yield return $"Stat{i}{suffix}";
    }

    private static IconsContentWidgetProperties ValidProperties()
    {
        var props = new IconsContentWidgetProperties();
        foreach (var field in Fields())
            Set(props, field, $"{field}-{Guid.NewGuid():N}");
        return props;
    }

    private static object? Read(object model, string field) =>
        model.GetType().GetProperty(field)!.GetValue(model);

    private static void Set(
        IconsContentWidgetProperties props, string field, string? value) =>
        typeof(IconsContentWidgetProperties)
            .GetProperty(field)!.SetValue(props, value);

    private static ComponentViewModel<IconsContentWidgetProperties> Component(
        IconsContentWidgetProperties? props)
    {
        var type = typeof(ComponentViewModel<IconsContentWidgetProperties>);
        var component = (ComponentViewModel<IconsContentWidgetProperties>)
            RuntimeHelpers.GetUninitializedObject(type);

        // Test-only: bypasses Kentico initialization.
        for (Type? current = type; current != null; current = current.BaseType)
        {
            const BindingFlags flags = BindingFlags.Instance |
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly;

            var property = current.GetProperty("Properties", flags);
            var field = current.GetField("<Properties>k__BackingField", flags);

            if (property?.GetSetMethod(true) is MethodInfo setter)
                setter.Invoke(component, new object?[] { props });
            else if (field != null)
                field.SetValue(component, props);
            else
                continue;

            if (!ReferenceEquals(component.Properties, props))
                throw new InvalidOperationException(
                    "Component.Properties initialization failed.");

            return component;
        }

        throw new InvalidOperationException(
            "Unsupported Kentico Properties storage. Use your supported factory.");
    }
}