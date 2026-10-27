using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using ETG.Web.Components;
using ETG.Web.Components.PageBuilder.Widgets.HtmlText;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using NUnit.Framework;

namespace ETG.Web.Tests.Components.PageBuilder.Widgets;

[TestFixture]
public class HtmlTextWidgetTests : BaseWebTest
{
    private const string ContentView =
        "~/Components/PageBuilder/Widgets/HtmlText/HtmlText.cshtml";

    private const string ErrorView =
        "~/Components/ComponentError.cshtml";

    private const string NoText =
        "No text has been set for this widget.";

    public static IEnumerable<TestCaseData> TextCases()
    {
        var scenarios = new[]
        {
            "ValidHtml",
            "Unicode",
            "SpecialCharacters",
            "EncodedHtml",
            "MalformedHtml",
            "ScriptPayload",
            "EventPayload",
            "UnsafeLink",
            "Null",
            "Empty",
            "Whitespace"
        };

        foreach (var scenario in scenarios)
        {
            var test = new TestCaseData(scenario)
                .SetName($"Invoke_{scenario}");

            if (scenario.EndsWith("Payload") || scenario == "UnsafeLink")
            {
                test.SetCategory("SecurityBoundary");
            }

            yield return test;
        }
    }

    [Test]
    public void MetadataAndDefaults_Match()
    {
        var props = new HtmlTextWidgetProperties();

        Assert.Multiple(() =>
        {
            Assert.That(
                HtmlTextWidget.IDENTIFIER,
                Is.EqualTo("ETG.Web.Widget.HtmlTextWidget"));

            Assert.That(HtmlTextWidget.NAME, Is.EqualTo("Html Text"));
            Assert.That(props.Text, Is.Empty);
        });
    }

    [TestCaseSource(nameof(TextCases))]
    public void Invoke_RuntimeInputs_ReturnExpectedResult(string scenario)
    {
        var token = Guid.NewGuid().ToString("N");

        string? text = scenario switch
        {
            "ValidHtml" => $"<p>Content-{token}</p>",
            "Unicode" => $"<p>日本語-é-مرحبا-{token}</p>",
            "SpecialCharacters" => $"& < > \" ' {token}",
            "EncodedHtml" => $"&lt;b&gt;{token}&lt;/b&gt;",
            "MalformedHtml" => $"<div><strong>{token}",
            "ScriptPayload" => $"<script>alert('{token}')</script>",
            "EventPayload" =>
                $"<img src=x onerror=\"alert('{token}')\">",
            "UnsafeLink" =>
                $"<a href=\"javascript:alert('{token}')\">Open</a>",
            "Null" => null,
            "Empty" => string.Empty,
            "Whitespace" =>
                new string(' ', 1 + token.Length % 7) + "\t\r\n",
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };

        var component = Component(new HtmlTextWidgetProperties
        {
            Text = text!
        });

        var view = Invoke(component);

        if (scenario is "Null" or "Empty" or "Whitespace")
        {
            AssertError(view);
            return;
        }

        Assert.That(view.ViewName, Is.EqualTo(ContentView));
        Assert.That(
            view.ViewData.Model,
            Is.TypeOf<HtmlTextWidgetViewModel>());

        var model = (HtmlTextWidgetViewModel)view.ViewData.Model!;

        Assert.That(model.Text, Is.EqualTo(text));

        // Payload preservation documents current behavior.
        // It does not verify safe encoding or sanitization in Razor.
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Invoke_NullInputs_ReturnExactError(bool nullComponent)
    {
        var component = nullComponent ? null : Component(null);

        AssertError(Invoke(component));
    }

    private static ViewViewComponentResult Invoke(
        ComponentViewModel<HtmlTextWidgetProperties>? component)
    {
        var result = new HtmlTextWidget().Invoke(component!);

        Assert.That(result, Is.TypeOf<ViewViewComponentResult>());

        return (ViewViewComponentResult)result;
    }

    private static void AssertError(ViewViewComponentResult view)
    {
        Assert.That(view.ViewName, Is.EqualTo(ErrorView));
        Assert.That(
            view.ViewData.Model,
            Is.TypeOf<ComponentErrorViewModel>());

        var model = (ComponentErrorViewModel)view.ViewData.Model!;

        Assert.Multiple(() =>
        {
            Assert.That(
                model.ComponentLabel,
                Is.EqualTo("Html Text Widget"));

            Assert.That(model.Errors, Is.EqualTo(new[] { NoText }));
        });
    }

    private static ComponentViewModel<HtmlTextWidgetProperties> Component(
        HtmlTextWidgetProperties? props)
    {
        var type = typeof(ComponentViewModel<HtmlTextWidgetProperties>);

        var component =
            (ComponentViewModel<HtmlTextWidgetProperties>)
            RuntimeHelpers.GetUninitializedObject(type);

        // Test-only: bypasses Kentico constructor initialization.
        for (Type? current = type;
             current != null;
             current = current.BaseType)
        {
            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly;

            var property = current.GetProperty("Properties", flags);

            if (property?.GetSetMethod(true) is MethodInfo setter)
            {
                setter.Invoke(component, new object?[] { props });
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
            "Unsupported Kentico Properties storage. Use your version's " +
            "supported ComponentViewModel constructor or factory.");

        void Verify()
        {
            if (!ReferenceEquals(component.Properties, props))
            {
                throw new InvalidOperationException(
                    "Component.Properties was not initialized correctly.");
            }
        }
    }
}