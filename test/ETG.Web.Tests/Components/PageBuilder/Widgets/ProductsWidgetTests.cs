#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core;
using ETG.Core.Models.Tour;
using ETG.Web.Components;
using ETG.Web.Components.PageBuilder.Widgets.Products;
using ETG.Web.Services.Tour;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Moq;
using NUnit.Framework;

namespace ETG.Web.Tests.Components.PageBuilder.Widgets;

[TestFixture]
public class ProductsWidgetTests : BaseWebTest
{
    private Mock<IContentQueryExecutor> executor = null!;
    private Mock<IWebPageUrlRetriever> urls = null!;
    private Mock<IWebsiteChannelContext> channel = null!;
    private Mock<ITourPricingService> pricing = null!;
    private Mock<IContentQueryExecutionOptionsCreator> options = null!;
    private Mock<IContentRetriever> content = null!;
    private ProductsWidget widget = null!;

    public override void Setup()
    {
        base.Setup();

        executor = new(MockBehavior.Strict);
        urls = new(MockBehavior.Strict);
        channel = new(MockBehavior.Strict);
        pricing = new(MockBehavior.Strict);
        options = new(MockBehavior.Strict);
        content = new(MockBehavior.Strict);

        widget = new ProductsWidget(
            executor.Object,
            urls.Object,
            channel.Object,
            pricing.Object,
            options.Object,
            content.Object);
    }

    [Test]
    public void IdentifierAndName_AreCorrect()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                ProductsWidget.IDENTIFIER,
                Is.EqualTo("ETG.Web.Widget.ProductsWidget"));

            Assert.That(
                ProductsWidget.NAME,
                Is.EqualTo("Products Widget"));
        });
    }

    [TestCase(true)]
    [TestCase(false)]
    [Category("CurrentBehavior")]
    public async Task MissingModelOrProperties_ReturnsExactError(
        bool nullModel)
    {
        var result = await widget.InvokeAsync(
            nullModel ? null! : Model(null));

        AssertError(
            result,
            "No properties have been set for this widget.");

        VerifyNoCalls();
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase("\t\r\n")]
    [TestCase(",")]
    [TestCase(",,,")]
    [TestCase(" , \t, \r\n, ")]
    [Category("CurrentBehavior")]
    public async Task EmptyCodes_ReturnExactErrorWithoutServiceCalls(
        string codes)
    {
        var result = await widget.InvokeAsync(Model(new()
        {
            TourCodes = codes
        }));

        AssertError(
            result,
            "No package codes have been entered. Add comma-separated " +
            "TourCodes in the widget properties.");

        VerifyNoCalls();
    }

    [Test]
    [Category("CurrentBehavior")]
    public void NullTourCodes_CurrentlyThrows()
    {
        var props = new ProductsWidgetProperties
        {
            TourCodes = null!
        };

        Assert.ThrowsAsync<NullReferenceException>(
            async () => { await widget.InvokeAsync(Model(props)); });

        VerifyNoCalls();
    }

    // Property tests, not successful InvokeAsync retrieval tests.

    [TestCase(" A , B , C ", new string[] { "A", "B", "C" })]
    [TestCase("A,, ,B,", new string[] { "A", "B" })]
    [TestCase("A,A", new string[] { "A", "A" })]
    [TestCase("abc,ABC", new string[] { "abc", "ABC" })]
    [TestCase("UNKNOWN", new string[] { "UNKNOWN" })]
    [TestCase("???", new string[] { "???" })]
    [TestCase("<script>", new string[] { "<script>" })]
    [Category("CurrentBehavior")]
    public void PackageCodes_PreservesValuesAndOrder(
        string raw,
        string[] expected)
    {
        var props = new ProductsWidgetProperties
        {
            TourCodes = raw
        };

        Assert.That(props.PackageCodes, Is.EqualTo(expected));
        VerifyNoCalls();
    }

    private static IEnumerable<TestCaseData> ModelCases()
    {
        foreach (string? text in new string?[]
                 {
                     null, "", " \t ", "Tours"
                 })
        {
            foreach (var hide in new[] { false, true })
            {
                yield return new TestCaseData(text, hide);
            }
        }
    }

    [TestCaseSource(nameof(ModelCases))]
    [Category("CurrentBehavior")]
    public void ViewModel_DefaultListingCopiesProperties(
        string? text,
        bool hide)
    {
        var props = new ProductsWidgetProperties
        {
            Heading = text!,
            SectionId = text!,
            ViewAllUrl = "/tours",
            HideViewAll = hide,
            TourCodes = " B , A , B "
        };

        var model = new ProductsWidgetViewModel(props);

        Assert.Multiple(() =>
        {
            Assert.That(model.Heading, Is.EqualTo(text));
            Assert.That(model.SectionId, Is.EqualTo(text));
            Assert.That(model.ViewAllUrl, Is.EqualTo("/tours"));
            Assert.That(model.HideViewAll, Is.EqualTo(hide));
            Assert.That(model.TourCodes, Is.EqualTo(props.TourCodes));
            Assert.That(
                model.PackageCodes,
                Is.EqualTo(new[] { "B", "A", "B" }));

            Assert.That(model.Tour.Heading, Is.EqualTo(text));
            Assert.That(model.Tour.SectionId, Is.EqualTo(text));
            Assert.That(model.Tour.ViewAllUrl, Is.EqualTo("/tours"));
            Assert.That(
                model.Tour.HideViewAllButton,
                Is.EqualTo(hide));
            Assert.That(model.Tour.Tours, Is.Empty);
        });

        VerifyNoCalls();
    }

    [Test]
    [Category("CurrentBehavior")]
    public void ViewModel_PreservesSuppliedListing()
    {
        var props = new ProductsWidgetProperties
        {
            TourCodes = "A"
        };

        var listing = new TourListingViewModel
        {
            Tours = new[]
            {
                new TourSummaryViewModel
                {
                    TourCode = "A",
                    Name = "Tour A"
                }
            }
        };

        var model = new ProductsWidgetViewModel(props, listing);

        Assert.That(model.Tour, Is.SameAs(listing));
        VerifyNoCalls();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("not a URL")]
    [TestCase("javascript:alert(1)")]
    [Category("CurrentBehavior")]
    public void ViewModel_CurrentlyCopiesUrlWithoutValidation(string? url)
    {
        var props = new ProductsWidgetProperties
        {
            TourCodes = "A",
            ViewAllUrl = url!
        };

        var model = new ProductsWidgetViewModel(props);

        Assert.Multiple(() =>
        {
            Assert.That(model.ViewAllUrl, Is.EqualTo(url));
            Assert.That(model.Tour.ViewAllUrl, Is.EqualTo(url));
        });

        VerifyNoCalls();
    }

    //[Test]
    //[Category("ValidationGap")]
    //[Ignore(
    //    "Known defect: null TourCodes throws in PackageCodes.Split().")]
    //public async Task NullTourCodes_ShouldReturnSafeError()
    //{
    //    var result = await widget.InvokeAsync(Model(new()
    //    {
    //        TourCodes = null!
    //    }));

    //    Assert.That(result, Is.TypeOf<ViewViewComponentResult>());
    //    var view = (ViewViewComponentResult)result;

    //    Assert.That(
    //        view.ViewName,
    //        Is.EqualTo("~/Components/ComponentError.cshtml"));

    //    Assert.That(
    //        view.ViewData.Model,
    //        Is.TypeOf<ComponentErrorViewModel>());

    //    var error = (ComponentErrorViewModel)view.ViewData.Model!;
    //    Assert.That(error.Errors, Is.Not.Empty);

    //    VerifyNoCalls();
    //}

    private static void AssertError(
        IViewComponentResult result,
        string message)
    {
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
                Is.EqualTo("Products Widget Widget"));

            Assert.That(
                error.Errors,
                Is.EqualTo(new[] { message }));
        });
    }

    private void VerifyNoCalls()
    {
        executor.VerifyNoOtherCalls();
        urls.VerifyNoOtherCalls();
        channel.VerifyNoOtherCalls();
        pricing.VerifyNoOtherCalls();
        options.VerifyNoOtherCalls();
        content.VerifyNoOtherCalls();
    }

    private static ComponentViewModel<ProductsWidgetProperties> Model(
        ProductsWidgetProperties? props)
    {
        var type = typeof(ComponentViewModel<ProductsWidgetProperties>);

        const BindingFlags flags =
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.Instance;

        foreach (var constructor in type.GetConstructors(flags))
        {
            var parameters = constructor.GetParameters();

            var index = Array.FindIndex(
                parameters,
                p => p.ParameterType == typeof(ProductsWidgetProperties));

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

            return (ComponentViewModel<ProductsWidgetProperties>)
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
            var model = (ComponentViewModel<ProductsWidgetProperties>)
                empty.Invoke(Array.Empty<object>());

            setter.Invoke(model, new object?[] { props });
            return model;
        }

        throw new InvalidOperationException(
            "Use the project's existing ComponentViewModel factory.");
    }
}