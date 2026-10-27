#nullable enable

using CMS.ContentEngine;
using ETG.Core.Components;
using ETG.Web.Components;
using ETG.Web.Components.PageBuilder.Widgets.ThreeColumnCallouts;
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
public class ThreeColumnCalloutsWidgetAdditionalTests : BaseWebTest
{
    private const string ErrorView =
        "~/Components/ComponentError.cshtml";

    private Mock<IContentRetriever> retriever = null!;
    private ThreeColumnCalloutsWidget widget = null!;

    public override void Setup()
    {
        base.Setup();

        retriever = new Mock<IContentRetriever>(MockBehavior.Strict);
        widget = new ThreeColumnCalloutsWidget(retriever.Object);
    }

    [Test]
    [Category("CurrentBehavior")]
    public void IdentifierAndName_MatchExpectedValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                ThreeColumnCalloutsWidget.IDENTIFIER,
                Is.EqualTo("ETG.Web.Widget.ThreeColumnCallouts"));

            Assert.That(
                ThreeColumnCalloutsWidget.NAME,
                Is.EqualTo("Three Column Callouts"));
        });
    }

    [TestCase("null", "No image has been selected.")]
    [TestCase("empty", "No image has been selected.")]
    [TestCase(
        "emptyGuid",
        "The selected content item or image file no longer exists.")]
    [Category("CurrentBehavior")]
    public async Task InvokeAsync_NoUsableFirstImage_ReturnsExactError(
        string scenario,
        string message)
    {
        var props = new ThreeColumnCalloutsWidgetProperties
        {
            Callout1Images = scenario switch
            {
                "null" => null!,
                "empty" => Array.Empty<ContentItemReference>(),
                "emptyGuid" => new[]
                {
                    new ContentItemReference { Identifier = Guid.Empty }
                },
                _ => throw new ArgumentOutOfRangeException(nameof(scenario))
            }
        };

        AssertError(await widget.InvokeAsync(Model(props)), message);

        retriever.VerifyNoOtherCalls();
    }

    [TestCase("Callout2Images")]
    [TestCase("Callout3Images")]
    [Category("CurrentBehavior")]
    public async Task InvokeAsync_NullOptionalCollection_ReachesValidation(
        string propertyName)
    {
        var props = new ThreeColumnCalloutsWidgetProperties();

        typeof(ThreeColumnCalloutsWidgetProperties)
            .GetProperty(propertyName)!
            .SetValue(props, null);

        AssertError(
            await widget.InvokeAsync(Model(props)),
            "No image has been selected.");

        retriever.VerifyNoOtherCalls();
    }

    // These tests assert exceptions in the current implementation.

    [Test]
    [Category("ValidationGap")]
    public void InvokeAsync_NullModel_Throws()
    {
        Assert.ThrowsAsync<NullReferenceException>(
            async () => { await widget.InvokeAsync(null!); });

        retriever.VerifyNoOtherCalls();
    }

    [Test]
    [Category("ValidationGap")]
    public void InvokeAsync_NullProperties_Throws()
    {
        var model = Model(null);

        Assert.ThrowsAsync<NullReferenceException>(
            async () => { await widget.InvokeAsync(model); });

        retriever.VerifyNoOtherCalls();
    }

    [TestCase("Callout1Images")]
    [TestCase("Callout2Images")]
    [TestCase("Callout3Images")]
    [Category("ValidationGap")]
    public void InvokeAsync_NullImageEntry_Throws(string propertyName)
    {
        var props = new ThreeColumnCalloutsWidgetProperties();

        typeof(ThreeColumnCalloutsWidgetProperties)
            .GetProperty(propertyName)!
            .SetValue(props, new ContentItemReference[] { null! });

        Assert.ThrowsAsync<NullReferenceException>(
            async () => { await widget.InvokeAsync(Model(props)); });

        retriever.VerifyNoOtherCalls();
    }

    // One constructor test per declared string property.
    // DisplayType is covered separately.
    private static IEnumerable<TestCaseData> StringCases()
    {
        var properties = typeof(ThreeColumnCalloutsWidgetProperties)
            .GetProperties(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly)
            .Where(p =>
                p.PropertyType == typeof(string) &&
                p.CanWrite &&
                p.GetIndexParameters().Length == 0 &&
                p.Name !=
                    nameof(ThreeColumnCalloutsWidgetProperties.DisplayType))
            .OrderBy(p => p.Name);

        foreach (var property in properties)
        {
            var value = property.Name.EndsWith(
                "Url",
                StringComparison.Ordinal)
                    ? "javascript:alert(1)"
                    : "<script>alert(1)</script>";

            yield return new TestCaseData(property.Name, value)
                .SetName($"ViewModel_{property.Name}_CopiesSubmittedValue");
        }
    }

    [TestCaseSource(nameof(StringCases))]
    [Category("ViewModelCurrentBehavior")]
    public void ViewModel_StringProperty_IsCopied(
        string propertyName,
        string value)
    {
        var props = new ThreeColumnCalloutsWidgetProperties();

        typeof(ThreeColumnCalloutsWidgetProperties)
            .GetProperty(propertyName)!
            .SetValue(props, value);

        var vm = CreateViewModel(props);

        var output = typeof(ThreeColumnCalloutsWidgetViewModel)
            .GetProperty(propertyName);

        Assert.That(
            output,
            Is.Not.Null,
            $"Missing view-model property: {propertyName}");

        Assert.That(output!.GetValue(vm), Is.EqualTo(value));
    }

    private static IEnumerable<TestCaseData> DisplayCases()
    {
        foreach (var name in Enum.GetNames(typeof(DisplayThemeType)))
        {
            yield return new TestCaseData(name);
        }

        yield return new TestCaseData(new object?[] { null });
        yield return new TestCaseData("");
        yield return new TestCaseData(" ");
        yield return new TestCaseData("InvalidTheme");
    }

    [TestCaseSource(nameof(DisplayCases))]
    [Category("ViewModelCurrentBehavior")]
    public void ViewModel_DisplayType_UsesExactComparison(
        string? displayType)
    {
        var vm = CreateViewModel(
            new ThreeColumnCalloutsWidgetProperties
            {
                DisplayType = displayType!
            });

        Assert.Multiple(() =>
        {
            Assert.That(vm.DisplayType, Is.EqualTo(displayType));

            Assert.That(
                vm.IsImageWithContent,
                Is.EqualTo(
                    displayType ==
                    nameof(DisplayThemeType.ImageWithContent)));

            Assert.That(
                vm.IsIconWithContent,
                Is.EqualTo(
                    displayType ==
                    nameof(DisplayThemeType.IconWithContent)));
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    [Category("ViewModelCurrentBehavior")]
    public void ViewModel_StartFromRight_IsCopied(bool value)
    {
        var vm = CreateViewModel(
            new ThreeColumnCalloutsWidgetProperties
            {
                StartFromRight = value
            });

        Assert.That(vm.StartFromRight, Is.EqualTo(value));
    }

    private static ThreeColumnCalloutsWidgetViewModel CreateViewModel(
        ThreeColumnCalloutsWidgetProperties props)
    {
        // Constructor-only tests; no media handling is tested here.
        return new ThreeColumnCalloutsWidgetViewModel(
            null!, null!, null!, props);
    }

    private static void AssertError(
        IViewComponentResult result,
        string message)
    {
        Assert.That(result, Is.TypeOf<ViewViewComponentResult>());

        var view = (ViewViewComponentResult)result;

        Assert.Multiple(() =>
        {
            Assert.That(view.ViewName, Is.EqualTo(ErrorView));

            Assert.That(
                view.ViewData.Model,
                Is.TypeOf<ComponentErrorViewModel>());
        });

        var error = (ComponentErrorViewModel)view.ViewData.Model!;

        Assert.Multiple(() =>
        {
            Assert.That(
                error.ComponentLabel,
                Is.EqualTo("Three Column Callouts Widget"));

            Assert.That(error.Errors, Is.EqualTo(new[] { message }));
        });
    }

    private static ComponentViewModel<ThreeColumnCalloutsWidgetProperties>
        Model(ThreeColumnCalloutsWidgetProperties? props)
    {
        var type =
            typeof(ComponentViewModel<ThreeColumnCalloutsWidgetProperties>);

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
                    typeof(ThreeColumnCalloutsWidgetProperties));

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

            return
                (ComponentViewModel<ThreeColumnCalloutsWidgetProperties>)
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
                (ComponentViewModel<ThreeColumnCalloutsWidgetProperties>)
                empty.Invoke(Array.Empty<object>());

            setter.Invoke(model, new object?[] { props });
            return model;
        }

        throw new InvalidOperationException(
            "Use the project's existing ComponentViewModel factory.");
    }
}