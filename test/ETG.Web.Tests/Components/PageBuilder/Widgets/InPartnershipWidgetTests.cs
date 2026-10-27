#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CMS.ContentEngine;
using ETG.Web.Components;
using ETG.Web.Components.PageBuilder.Widgets.InPartnership;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Moq;
using NUnit.Framework;

namespace ETG.Web.Tests.Components.PageBuilder.Widgets;

[TestFixture]
public class InPartnershipWidgetTests : BaseWebTest
{
    private Mock<IContentRetriever> retriever = null!;
    private InPartnershipWidget widget = null!;

    public override void Setup()
    {
        base.Setup();

        retriever = new Mock<IContentRetriever>(MockBehavior.Strict);
        widget = new InPartnershipWidget(retriever.Object);
    }

    private static IEnumerable<TestCaseData> MissingSelectionCases()
    {
        foreach (var logo1 in new[] { "null", "empty", "empty-guid" })
            foreach (var logo2 in new[] { "null", "empty", "empty-guid" })
                foreach (var logo3 in new[] { "null", "empty", "empty-guid" })
                {
                    yield return new TestCaseData(logo1, logo2, logo3)
                        .SetName(
                            $"MissingLogo1_{logo1}_Logo2_{logo2}_Logo3_{logo3}");
                }
    }

    [TestCaseSource(nameof(MissingSelectionCases))]
    public async Task MissingLogo1_ReturnsExactErrorWithoutRetrieval(
        string logo1,
        string logo2,
        string logo3)
    {
        var props = new InPartnershipWidgetProperties
        {
            Logo1Images = Selection(logo1)!,
            Logo2Images = Selection(logo2)!,
            Logo3Images = Selection(logo3)!
        };

        // Any unhandled exception fails this test.
        var result = await widget.InvokeAsync(Model(props));

        Assert.That(result, Is.TypeOf<ViewViewComponentResult>());

        var view = (ViewViewComponentResult)result;

        Assert.That(
            view.ViewName,
            Is.EqualTo("~/Components/ComponentError.cshtml"));

        Assert.That(
            view.ViewData.Model,
            Is.TypeOf<ComponentErrorViewModel>());

        var error = (ComponentErrorViewModel)view.ViewData.Model!;

        Assert.Multiple(() =>
        {
            Assert.That(
                error.ComponentLabel,
                Is.EqualTo("In Partnership Widget"));

            Assert.That(
                error.Errors,
                Is.EqualTo(new[]
                {
                    "Logo 1 is required and must contain a valid image asset."
                }));
        });

        retriever.VerifyNoOtherCalls();
    }

    private static IEnumerable<ContentItemReference>? Selection(
        string scenario)
    {
        return scenario switch
        {
            "null" => null,
            "empty" => Array.Empty<ContentItemReference>(),
            "empty-guid" => new[]
            {
                new ContentItemReference
                {
                    Identifier = Guid.Empty
                }
            },
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };
    }

    // Uses the same construction approach as the previous file.
    // Replace with your existing working SDK factory if necessary.
    private static ComponentViewModel<InPartnershipWidgetProperties> Model(
        InPartnershipWidgetProperties props)
    {
        var type =
            typeof(ComponentViewModel<InPartnershipWidgetProperties>);

        foreach (var constructor in type.GetConstructors(
                     BindingFlags.Public |
                     BindingFlags.NonPublic |
                     BindingFlags.Instance))
        {
            var parameters = constructor.GetParameters();

            var propertiesIndex = Array.FindIndex(
                parameters,
                parameter => parameter.ParameterType ==
                    typeof(InPartnershipWidgetProperties));

            if (propertiesIndex < 0 ||
                parameters
                    .Where((_, index) => index != propertiesIndex)
                    .Any(parameter => !parameter.HasDefaultValue))
            {
                continue;
            }

            var arguments = parameters
                .Select(parameter =>
                    parameter.HasDefaultValue
                        ? parameter.DefaultValue
                        : null)
                .ToArray();

            arguments[propertiesIndex] = props;

            return
                (ComponentViewModel<InPartnershipWidgetProperties>)
                constructor.Invoke(arguments);
        }

        var emptyConstructor = type.GetConstructor(
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.Instance,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);

        var setter = type.GetProperty("Properties")
            ?.GetSetMethod(nonPublic: true);

        if (emptyConstructor is not null && setter is not null)
        {
            var model =
                (ComponentViewModel<InPartnershipWidgetProperties>)
                emptyConstructor.Invoke(Array.Empty<object>());

            setter.Invoke(model, new object?[] { props });

            return model;
        }

        throw new InvalidOperationException(
            "Use your project's working ComponentViewModel factory " +
            "in the Model() helper.");
    }
}