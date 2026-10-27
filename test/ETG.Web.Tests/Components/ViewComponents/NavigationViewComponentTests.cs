using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using ETG.Web.Components.ViewComponents.MainNavigation;
using ETG.Web.Tests;
using MediatR;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Moq;
using NUnit.Framework;

namespace ETG.Web.Tests.Components.ViewComponents;

[TestFixture]
public class NavigationViewComponentTests : BaseWebTest
{
    private Mock<IMediator> _mediator = null!;
    private NavigationViewComponent _component = null!;

    private const string ViewPath =
        "~/Components/ViewComponents/MainNavigation/Navigation.cshtml";

    [SetUp]
    public override void Setup()
    {
        base.Setup();

        _mediator = new Mock<IMediator>();
        _component = new NavigationViewComponent(_mediator.Object);
    }

    [TestCaseSource(typeof(NavigationResponseCases))]
    public async Task InvokeAsync_ReturnsModelOrFallback(
        string scenario, int maxDepth)
    {
        var supplied = new NavigationViewModel();

        supplied.RootItems.Add(new NavigationItemViewModel
        {
            Id = "1",
            Name = "Home",
            LinkTo = "/"
        });

        var setup = _mediator.Setup(x => x.Send(
            It.IsAny<GetNavigationHierarchyQuery>(),
            It.IsAny<CancellationToken>()));

        switch (scenario)
        {
            case "Value":
                setup.ReturnsAsync(Maybe.From(supplied));
                break;

            case "None":
                setup.ReturnsAsync(Maybe<NavigationViewModel>.None);
                break;

            case "Exception":
                setup.ThrowsAsync(new InvalidOperationException());
                break;

            case "Cancellation":
                setup.ThrowsAsync(new OperationCanceledException());
                break;

            default:
                Assert.Fail($"Unknown scenario: {scenario}");
                return;
        }

        var result = await _component.InvokeAsync(maxDepth);

        Assert.That(result, Is.TypeOf<ViewViewComponentResult>());

        var view = (ViewViewComponentResult)result;

        Assert.That(view.ViewName, Is.EqualTo(ViewPath));
        Assert.That(view.ViewData.Model,
            Is.TypeOf<NavigationViewModel>());

        var model = (NavigationViewModel)view.ViewData.Model!;

        if (scenario == "Value")
        {
            Assert.That(model, Is.SameAs(supplied));
            Assert.That(model.RootItems.Single().LinkTo, Is.EqualTo("/"));
        }
        else
        {
            Assert.That(model.RootItems, Is.Empty);
            Assert.That(model.Layout, Is.Null);
        }

        // Current component forwards depth without validating it.
        _mediator.Verify(x => x.Send(
                It.Is<GetNavigationHierarchyQuery>(q =>
                    q.RootPath == "/Shared/Menus/Main-Menu" &&
                    q.MaxDepth == maxDepth),
                It.Is<CancellationToken>(t => t == CancellationToken.None)),
            Times.Once);
    }

    [TestCaseSource(typeof(NavigationQueryCases))]
    public void Query_PreservesInputsAndBuildsCacheKey(
        string? rootPath, int depth)
    {
        var query = new GetNavigationHierarchyQuery(rootPath!, depth);

        Assert.Multiple(() =>
        {
            Assert.That(query.RootPath, Is.EqualTo(rootPath));
            Assert.That(query.MaxDepth, Is.EqualTo(depth));

            Assert.That(query.CacheValueKey,
                Is.EqualTo($"Navigation_{rootPath}_{depth}"));

            Assert.That(
                new GetNavigationHierarchyQuery(rootPath!).MaxDepth,
                Is.EqualTo(10));
        });
    }

    [TestCaseSource(typeof(NavigationTreeCases))]
    public void Model_FlattensGeneratedTreeInDepthFirstOrder(int depth)
    {
        var model = new NavigationViewModel();

        if (depth > 0)
        {
            var root = new NavigationItemViewModel
            {
                Id = "0",
                Level = 0
            };

            model.RootItems.Add(root);
            var current = root;

            for (var level = 1; level < depth; level++)
            {
                var child = new NavigationItemViewModel
                {
                    Id = level.ToString(),
                    Level = level
                };

                current.Children.Add(child);
                current = child;
            }

            // A sibling root must appear after the entire first subtree.
            model.RootItems.Add(new NavigationItemViewModel
            {
                Id = "second",
                Level = 0
            });

            AssertGeneratedBranch(root, 0, depth);
        }

        var flattened = model.GetAllItemsFlattened();

        var expectedIds = depth == 0
            ? Array.Empty<string>()
            : Enumerable.Range(0, depth)
                .Select(x => x.ToString())
                .Append("second")
                .ToArray();

        Assert.That(
            flattened.Select(x => x.Id),
            Is.EqualTo(expectedIds));

        // Flattening must return a new list, not replace the roots.
        flattened.Clear();

        Assert.That(model.RootItems.Count,
            Is.EqualTo(depth == 0 ? 0 : 2));
    }

    [TestCaseSource(typeof(NavigationVisibilityCases))]
    public void Item_UsesVisibilityFlags(
        bool hideMobile, bool hideDesktop, bool isMobile)
    {
        var item = new NavigationItemViewModel
        {
            HideOnMobile = hideMobile,
            HideOnDesktop = hideDesktop
        };

        Assert.That(
            item.IsVisible(isMobile),
            Is.EqualTo(isMobile ? !hideMobile : !hideDesktop));
    }

    [TestCaseSource(typeof(NavigationFieldCases))]
    public async Task InvokeAsync_PreservesUnvalidatedFields(string? value)
    {
        var item = new NavigationItemViewModel
        {
            Name = value!,
            LinkTo = value,
            ViewAllUrl = value
        };

        var supplied = new NavigationViewModel
        {
            RootItems = new List<NavigationItemViewModel> { item }
        };

        _mediator.Setup(x => x.Send(
                It.IsAny<GetNavigationHierarchyQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(supplied));

        var result = await _component.InvokeAsync();

        Assert.That(result, Is.TypeOf<ViewViewComponentResult>());

        var view = (ViewViewComponentResult)result;
        Assert.That(view.ViewName, Is.EqualTo(ViewPath));

        var model = view.ViewData.Model as NavigationViewModel;

        Assert.That(model, Is.SameAs(supplied));

        Assert.Multiple(() =>
        {
            Assert.That(model!.RootItems.Single().Name, Is.EqualTo(value));
            Assert.That(model.RootItems.Single().LinkTo, Is.EqualTo(value));
            Assert.That(model.RootItems.Single().ViewAllUrl, Is.EqualTo(value));
            Assert.That(item.HasChildren, Is.False);
            Assert.That(item.Target, Is.EqualTo("_self"));
        });

        _mediator.Verify(x => x.Send(
                It.Is<GetNavigationHierarchyQuery>(q => q.MaxDepth == 10),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestCaseSource(typeof(NavigationNullTreeCases))]
    public void Model_InvalidNullTree_CurrentlyThrows(string scenario)
    {
        var model = new NavigationViewModel();

        switch (scenario)
        {
            case "NullRoots":
                model.RootItems = null!;
                break;

            case "NullItem":
                model.RootItems.Add(null!);
                break;

            case "NullChildren":
                model.RootItems.Add(new NavigationItemViewModel
                {
                    Children = null!
                });
                break;
        }

        Assert.Throws<NullReferenceException>(
            () => model.GetAllItemsFlattened());
    }

    private void AssertGeneratedBranch(
        NavigationItemViewModel item, int level, int depth)
    {
        Assert.That(item.Id, Is.EqualTo(level.ToString()));
        Assert.That(item.Level, Is.EqualTo(level));
        Assert.That(item.HasChildren, Is.EqualTo(level < depth - 1));

        if (level < depth - 1)
        {
            Assert.That(item.Children.Count, Is.EqualTo(1));

            AssertGeneratedBranch(
                item.Children.Single(), level + 1, depth);
        }
    }
}

// Non-static case providers.
public abstract class NavigationCaseSource : IEnumerable<TestCaseData>
{
    public abstract IEnumerator<TestCaseData> GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public sealed class NavigationResponseCases : NavigationCaseSource
{
    public override IEnumerator<TestCaseData> GetEnumerator()
    {
        foreach (var scenario in new[]
        {
            "Value", "None", "Exception", "Cancellation"
        })
            foreach (var depth in new[] { -1, 0, 1, 10 })
            {
                yield return new TestCaseData(scenario, depth);
            }
    }
}

public sealed class NavigationQueryCases : NavigationCaseSource
{
    public override IEnumerator<TestCaseData> GetEnumerator()
    {
        foreach (var path in new string?[]
        {
            null, "", " ", "../unexpected", "/Shared/Menus/Main-Menu"
        })
            foreach (var depth in new[] { int.MinValue, 0, 1, 10, int.MaxValue })
            {
                yield return new TestCaseData(path, depth);
            }
    }
}

public sealed class NavigationTreeCases : NavigationCaseSource
{
    public override IEnumerator<TestCaseData> GetEnumerator()
    {
        foreach (var depth in new[] { 0, 1, 5, 32 })
            yield return new TestCaseData(depth);
    }
}

public sealed class NavigationVisibilityCases : NavigationCaseSource
{
    public override IEnumerator<TestCaseData> GetEnumerator()
    {
        foreach (var mobile in new[] { false, true })
            foreach (var desktop in new[] { false, true })
                foreach (var device in new[] { false, true })
                    yield return new TestCaseData(mobile, desktop, device);
    }
}

public sealed class NavigationFieldCases : NavigationCaseSource
{
    public override IEnumerator<TestCaseData> GetEnumerator()
    {
        foreach (var value in new string?[]
        {
            null, "", " ", "/about", "not-a-url", "javascript:alert(1)"
        })
        {
            yield return new TestCaseData(new object?[] { value });
        }
    }
}

public sealed class NavigationNullTreeCases : NavigationCaseSource
{
    public override IEnumerator<TestCaseData> GetEnumerator()
    {
        foreach (var scenario in new[]
        {
            "NullRoots", "NullItem", "NullChildren"
        })
        {
            yield return new TestCaseData(scenario);
        }
    }
}