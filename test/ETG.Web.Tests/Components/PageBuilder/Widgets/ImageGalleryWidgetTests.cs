using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CMS.ContentEngine;
using ETG.Web.Components;
using ETG.Web.Components.PageBuilder.Widgets.ImageGallery;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Moq;
using NUnit.Framework;

namespace ETG.Web.Tests.Components.PageBuilder.Widgets;

[TestFixture]
public class ImageGalleryWidgetTests : BaseWebTest
{
    private Mock<IContentRetriever> retriever = null!;

    [SetUp]
    public void SetUpGallery()
    {
        // No extension methods are configured or verified.
        retriever = new Mock<IContentRetriever>(MockBehavior.Strict);
    }

    [Test, Order(0)]
    public void IdentifierAndName_Match()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ImageGalleryWidget.IDENTIFIER,
                Is.EqualTo("ETG.Web.Widget.ImageGallery"));
            Assert.That(ImageGalleryWidget.NAME,
                Is.EqualTo("Image Gallery"));
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task EmptyOrEmptyGuidSelection_ReturnsExactError(
        bool emptyGuid)
    {
        var props = new ImageGalleryProperties
        {
            ImagePaths = emptyGuid
                ? new[]
                {
                    new ContentItemReference { Identifier = Guid.Empty }
                }
                : Array.Empty<ContentItemReference>()
        };

        var result = await Widget().InvokeAsync(Component(props));

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
                Is.EqualTo("Image Gallery Widget"));
            Assert.That(error.Errors, Is.EqualTo(new[]
            {
                "No valid images have been selected."
            }));
        });

        retriever.VerifyNoOtherCalls();
    }

    [Test]
    [Category("KnownValidationGap")]
    public void NullCollection_CurrentlyThrowsArgumentNullException()
    {
        var component = Component(new ImageGalleryProperties
        {
            ImagePaths = null!
        });

        var error = Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await Widget().InvokeAsync(component);
        });

        Assert.That(error!.ParamName, Is.EqualTo("source"));
        retriever.VerifyNoOtherCalls();
    }

    [Test]
    [Category("KnownValidationGap")]
    public void NullEntry_CurrentlyThrowsNullReferenceException()
    {
        var component = Component(new ImageGalleryProperties
        {
            ImagePaths = new ContentItemReference[] { null! }
        });

        Assert.ThrowsAsync<NullReferenceException>(async () =>
        {
            await Widget().InvokeAsync(component);
        });

        retriever.VerifyNoOtherCalls();
    }

    [TestCase(false)]
    [TestCase(true)]
    [Category("KnownValidationGap")]
    public void NullComponentOrProperties_CurrentlyThrows(bool nullComponent)
    {
        var component = nullComponent ? null : Component(null);

        Assert.ThrowsAsync<NullReferenceException>(async () =>
        {
            await Widget().InvokeAsync(component!);
        });

        retriever.VerifyNoOtherCalls();
    }

    private ImageGalleryWidget Widget() => new(retriever.Object);

    private static ComponentViewModel<ImageGalleryProperties> Component(
        ImageGalleryProperties? props)
    {
        var type = typeof(ComponentViewModel<ImageGalleryProperties>);
        var component = (ComponentViewModel<ImageGalleryProperties>)
            RuntimeHelpers.GetUninitializedObject(type);

        // Test-only: bypasses Kentico component initialization.
        for (Type? current = type;
             current != null;
             current = current.BaseType)
        {
            const BindingFlags flags =
                BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

            var property = current.GetProperty("Properties", flags);
            var field = current.GetField(
                "<Properties>k__BackingField", flags);

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
            "Unsupported Kentico Properties storage. " +
            "Use your version's supported component factory.");
    }
}