#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ETG.Web.Components;
using ETG.Web.Components.PageBuilder.Widgets.SingleColumn;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using NUnit.Framework;

namespace ETG.Web.Tests.Components.PageBuilder.Widgets;

[TestFixture]
public class SingleColumnWidgetTests : BaseWebTest
{
    private const string SuccessView =
        "~/Components/PageBuilder/Widgets/SingleColumn/SingleColumn.cshtml";

    private const string ErrorView =
        "~/Components/ComponentError.cshtml";

    private SingleColumnWidget widget = null!;

    public override void Setup()
    {
        base.Setup();
        widget = new SingleColumnWidget();
    }

    [Test]
    [Category("CurrentBehavior")]
    public void IdentifierAndName_AreCorrect()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                SingleColumnWidget.IDENTIFIER,
                Is.EqualTo("ETG.Web.Widget.SingleColumn"));

            Assert.That(
                SingleColumnWidget.NAME,
                Is.EqualTo("Single Column"));
        });
    }

    [TestCase(true)]
    [TestCase(false)]
    [Category("CurrentBehavior")]
    public void NullModelOrProperties_ReturnsExactError(bool nullModel)
    {
        AssertError(widget.Invoke(nullModel ? null! : Model(null)));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" \t\r\n ")]
    [Category("CurrentBehavior")]
    public void MissingContent_ReturnsExactError(string? content)
    {
        AssertError(widget.Invoke(Model(new()
        {
            Content = content!
        })));
    }

    [TestCase("Valid content")]
    [TestCase("<script>alert(1)</script>")]
    [Category("CurrentBehavior")]
    public void NonWhitespaceContent_IsCopiedUnchanged(string content)
    {
        var props = new SingleColumnWidgetProperties
        {
            Content = content,
            LargeContent = "Heading",
            CTALabel = "Read more",
            CTAUrl = "/details"
        };

        AssertSuccess(widget.Invoke(Model(props)), props);
    }

    private static IEnumerable<TestCaseData> OptionalFieldCases()
    {
        var properties = typeof(SingleColumnWidgetProperties)
            .GetProperties(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly)
            .Where(p =>
                p.PropertyType == typeof(string) &&
                p.CanWrite &&
                p.GetIndexParameters().Length == 0 &&
                p.Name != nameof(SingleColumnWidgetProperties.Content))
            .OrderBy(p => p.Name);

        foreach (var property in properties)
        {
            var value = property.Name ==
                        nameof(SingleColumnWidgetProperties.CTAUrl)
                ? "javascript:alert(1)"
                : "<script>alert(1)</script>";

            yield return new TestCaseData(property.Name, value)
                .SetName(
                    $"OptionalField_{property.Name}_CopiesUnexpectedValue");
        }
    }

    [TestCaseSource(nameof(OptionalFieldCases))]
    public void OptionalFields_AreCopiedWithoutValidation(
        string field,
        string value)
    {
        var props = new SingleColumnWidgetProperties
        {
            Content = "Valid content",
            LargeContent = "Heading",
            CTALabel = "Read more",
            CTAUrl = "/details"
        };

        var property = typeof(SingleColumnWidgetProperties)
            .GetProperty(field);

        Assert.That(property, Is.Not.Null);
        property!.SetValue(props, value);

        AssertSuccess(widget.Invoke(Model(props)), props);
    }

    [Test]
    [Category("CurrentBehavior")]
    public void ValidContent_WithAllOptionalFieldsNull_ReturnsSuccess()
    {
        var props = new SingleColumnWidgetProperties
        {
            Content = "Valid content",
            LargeContent = null!,
            CTALabel = null!,
            CTAUrl = null!
        };

        AssertSuccess(widget.Invoke(Model(props)), props);
    }


    private static void AssertError(IViewComponentResult result)
    {
        var view = AssertView(result, ErrorView);

        Assert.That(
            view.ViewData?.Model,
            Is.TypeOf<ComponentErrorViewModel>());

        var error = (ComponentErrorViewModel)view.ViewData.Model!;

        Assert.Multiple(() =>
        {
            Assert.That(
                error.ComponentLabel,
                Is.EqualTo("Single Column Widget"));

            Assert.That(
                error.Errors,
                Is.EqualTo(new[]
                {
                    "No content has been set for this widget."
                }));
        });
    }

    private static void AssertSuccess(
        IViewComponentResult result,
        SingleColumnWidgetProperties expected)
    {
        var view = AssertView(result, SuccessView);

        Assert.That(
            view.ViewData.Model,
            Is.TypeOf<SingleColumnWidgetViewModel>());

        var model = (SingleColumnWidgetViewModel)view.ViewData.Model!;

        Assert.Multiple(() =>
        {
            Assert.That(model.Content, Is.EqualTo(expected.Content));
            Assert.That(model.LargeContent, Is.EqualTo(expected.LargeContent));
            Assert.That(model.CTALabel, Is.EqualTo(expected.CTALabel));
            Assert.That(model.CTAUrl, Is.EqualTo(expected.CTAUrl));
        });
    }

    private static ViewViewComponentResult AssertView(
        IViewComponentResult result,
        string path)
    {
        Assert.That(result, Is.TypeOf<ViewViewComponentResult>());

        var view = (ViewViewComponentResult)result;
        Assert.That(view.ViewName, Is.EqualTo(path));

        return view;
    }

    private static ComponentViewModel<SingleColumnWidgetProperties> Model(
        SingleColumnWidgetProperties? props)
    {
        var type =
            typeof(ComponentViewModel<SingleColumnWidgetProperties>);

        const BindingFlags flags =
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.Instance;

        foreach (var constructor in type.GetConstructors(flags))
        {
            var parameters = constructor.GetParameters();

            var index = Array.FindIndex(
                parameters,
                p => p.ParameterType ==
                    typeof(SingleColumnWidgetProperties));

            if (index < 0 ||
                parameters
                    .Where((_, i) => i != index)
                    .Any(p => !p.HasDefaultValue))
            {
                continue;
            }

            var arguments = parameters
                .Select(p => p.HasDefaultValue ? p.DefaultValue : null)
                .ToArray();

            arguments[index] = props;

            return (ComponentViewModel<SingleColumnWidgetProperties>)
                constructor.Invoke(arguments);
        }

        var empty = type.GetConstructor(
            flags,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);

        var setter = type.GetProperty("Properties")
            ?.GetSetMethod(nonPublic: true);

        if (empty is not null && setter is not null)
        {
            var model =
                (ComponentViewModel<SingleColumnWidgetProperties>)
                empty.Invoke(Array.Empty<object>());

            setter.Invoke(model, new object?[] { props });
            return model;
        }

        throw new InvalidOperationException(
            "Use the project's existing ComponentViewModel factory.");
    }
}