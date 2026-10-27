#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ETG.Web.Components;
using ETG.Web.Components.PageBuilder.Widgets.Spacer;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using NUnit.Framework;

namespace ETG.Web.Tests.Components.PageBuilder.Widgets;

[TestFixture]
public class SpacerWidgetTests : BaseWebTest
{
    private const string SuccessView =
        "~/Components/PageBuilder/Widgets/Spacer/Spacer.cshtml";

    private const string ErrorView =
        "~/Components/ComponentError.cshtml";

    private const string MissingProperties =
        "The Spacer widget properties could not be loaded.";

    private const string InvalidPadding =
        "Invalid padding value. Only 0, 20, 40, or 60 can be used.";

    private SpacerWidget widget = null!;

    public override void Setup()
    {
        base.Setup();
        widget = new SpacerWidget();
    }

    [Test]
    public void IdentifierAndName_MatchExpectedValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                SpacerWidget.IDENTIFIER,
                Is.EqualTo("ETG.Web.Widget.SpacerWidget"));

            Assert.That(
                SpacerWidget.NAME,
                Is.EqualTo("Spacer"));
        });
    }

    [TestCase(0)]
    [TestCase(20)]
    [TestCase(40)]
    [TestCase(60)]
    [Category("CurrentBehavior")]
    public void AllowedPadding_ReturnsExactSuccess(int padding)
    {
        var result = widget.Invoke(Model(new() { Padding = padding }));
        AssertSuccess(result, padding);
    }

    [TestCase(-1)]
    [TestCase(-20)]
    [TestCase(1)]
    [TestCase(19)]
    [TestCase(21)]
    [TestCase(39)]
    [TestCase(41)]
    [TestCase(59)]
    [TestCase(61)]
    [TestCase(80)]
    [TestCase(int.MinValue)]
    [TestCase(int.MaxValue)]
    [Category("CurrentBehavior")]
    public void UnsupportedPadding_ReturnsExactError(int padding)
    {
        AssertError(
            widget.Invoke(Model(new() { Padding = padding })),
            InvalidPadding);
    }

    [Test]
    [Category("CurrentBehavior")]
    public void NullProperties_ReturnsExactError()
    {
        AssertError(widget.Invoke(Model(null)), MissingProperties);
    }

    [Test]
    [Category("CurrentBehavior")]
    public void NullComponentModel_CurrentlyThrows()
    {
        Assert.Throws<NullReferenceException>(
            () => widget.Invoke(null!));
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase("\t\r\n")]
    [TestCase("abc")]
    [TestCase("20px")]
    [TestCase("20.5")]
    [TestCase("20.0")]
    [TestCase("2147483648")]
    [TestCase("-2147483649")]
    [TestCase("<script>alert(1)</script>")]
    [Category("BindingBoundary")]
    public async Task MalformedInteger_IsRejected(string submitted)
    {
        var context = await BindPadding(submitted);

        Assert.Multiple(() =>
        {
            Assert.That(context.Result.IsModelSet, Is.False);
            Assert.That(context.ModelState.IsValid, Is.False);
            Assert.That(context.ModelState.ErrorCount, Is.GreaterThan(0));
        });

        Assert.That(context.ModelState.ContainsKey("Padding"), Is.True);
        Assert.That(
            context.ModelState["Padding"]!.Errors.Count,
            Is.GreaterThan(0));
    }

    [TestCase("0", 0, true)]
    [TestCase("20", 20, true)]
    [TestCase("40", 40, true)]
    [TestCase("60", 60, true)]
    [TestCase("-1", -1, false)]
    [TestCase("21", 21, false)]
    [TestCase("80", 80, false)]
    [TestCase("2147483647", int.MaxValue, false)]
    [Category("BindingBoundary")]
    public async Task BoundInteger_IsValidatedByWidget(
        string submitted,
        int expected,
        bool allowed)
    {
        var context = await BindPadding(submitted);

        // Binding does not run the complete MVC validation pipeline.
        Assert.That(context.ModelState.ErrorCount, Is.Zero);
        Assert.That(context.Result.IsModelSet, Is.True);
        Assert.That(context.Result.Model, Is.TypeOf<int>());
        Assert.That(context.Result.Model, Is.EqualTo(expected));

        var result = widget.Invoke(Model(new()
        {
            Padding = (int)context.Result.Model!
        }));

        if (allowed)
        {
            AssertSuccess(result, expected);
        }
        else
        {
            AssertError(result, InvalidPadding);
        }
    }

    [Test]
    [Category("CurrentBehavior")]
    public void DefaultProperties_UseAllowedZeroPadding()
    {
        var props = new SpacerWidgetProperties();

        Assert.That(props.Padding, Is.Zero);
        AssertSuccess(widget.Invoke(Model(props)), 0);
    }

    private static async Task<ModelBindingContext> BindPadding(
        string submitted)
    {
        var action = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor(),
            new ModelStateDictionary());

        var values = new QueryCollection(
            new Dictionary<string, StringValues>
            {
                ["Padding"] = new StringValues(submitted)
            });

        var provider = new QueryStringValueProvider(
            BindingSource.Query,
            values,
            CultureInfo.InvariantCulture);

        var metadata = new EmptyModelMetadataProvider()
            .GetMetadataForType(typeof(int));

        ModelBindingContext context =
            DefaultModelBindingContext.CreateBindingContext(
                action,
                provider,
                metadata,
                bindingInfo: null,
                modelName: "Padding");

        var binder = new SimpleTypeModelBinder(
            typeof(int),
            NullLoggerFactory.Instance);

        await binder.BindModelAsync(context);
        return context;
    }

    private static ViewViewComponentResult AssertView(
        IViewComponentResult result,
        string expectedPath)
    {
        Assert.That(result, Is.TypeOf<ViewViewComponentResult>());

        var view = (ViewViewComponentResult)result;
        Assert.That(view.ViewName, Is.EqualTo(expectedPath));

        return view;
    }

    private static void AssertSuccess(
        IViewComponentResult result,
        int expectedPadding)
    {
        var view = AssertView(result, SuccessView);

        Assert.That(
            view.ViewData.Model,
            Is.TypeOf<SpacerWidgetViewModel>());

        Assert.That(
            ((SpacerWidgetViewModel)view.ViewData.Model!).Padding,
            Is.EqualTo(expectedPadding));
    }

    private static void AssertError(
        IViewComponentResult result,
        string expectedMessage)
    {
        var view = AssertView(result, ErrorView);

        Assert.That(
            view.ViewData.Model,
            Is.TypeOf<ComponentErrorViewModel>());

        var error = (ComponentErrorViewModel)view.ViewData.Model!;

        Assert.Multiple(() =>
        {
            Assert.That(
                error.ComponentLabel,
                Is.EqualTo("Spacer Widget"));

            Assert.That(
                error.Errors,
                Is.EqualTo(new[] { expectedMessage }));
        });
    }

    private static ComponentViewModel<SpacerWidgetProperties> Model(
        SpacerWidgetProperties? props)
    {
        var type = typeof(ComponentViewModel<SpacerWidgetProperties>);

        const BindingFlags flags =
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.Instance;

        foreach (var constructor in type.GetConstructors(flags))
        {
            var parameters = constructor.GetParameters();

            var index = Array.FindIndex(
                parameters,
                p => p.ParameterType == typeof(SpacerWidgetProperties));

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

            return (ComponentViewModel<SpacerWidgetProperties>)
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
            var model = (ComponentViewModel<SpacerWidgetProperties>)
                empty.Invoke(Array.Empty<object>());

            setter.Invoke(model, new object?[] { props });
            return model;
        }

        throw new InvalidOperationException(
            "Replace Model() with your project's ComponentViewModel factory.");
    }
}