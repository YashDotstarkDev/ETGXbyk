using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using ETG.Web.Components.ViewComponents.MainNavigation;
using ETG.Web.Tests;
using MediatR;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Moq;
using NUnit.Framework;

namespace ETG.Web.Tests.Components.ViewComponents
{
    [TestFixture]
    public class TopNavigationViewComponentTests : BaseWebTest
    {
        private const string ViewPath =
            "~/Components/ViewComponents/TopNavigation/TopNavigation.cshtml";

        private Mock<IMediator> _mediator = null!;
        private TopNavigationViewComponent _component = null!;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            _mediator = new Mock<IMediator>();
            _component = new TopNavigationViewComponent(_mediator.Object);
        }

        [TestCaseSource(typeof(TopNavigationModelCases))]
        public async Task InvokeAsync_MapsReturnedModelUnchanged(
            NavigationViewModel model)
        {
            Returns(model);

            var actual = AssertView(await _component.InvokeAsync());

            Assert.That(actual, Is.SameAs(model));
            VerifyQuery(3);
        }

        [Test]
        public async Task InvokeAsync_MissingResult_ReturnsEmptyModel()
        {
            Returns(Maybe<NavigationViewModel>.None);

            AssertEmpty(AssertView(await _component.InvokeAsync()));
            VerifyQuery(3);
        }

        [TestCaseSource(typeof(TopNavigationDepthCases))]
        public async Task InvokeAsync_ForwardsDepthWithoutValidation(int depth)
        {
            Returns(new NavigationViewModel());

            AssertView(await _component.InvokeAsync(depth));

            // Includes invalid/boundary values: no component validation.
            VerifyQuery(depth);
        }

        [TestCaseSource(typeof(TopNavigationFailureCases))]
        public async Task InvokeAsync_MediatorFailure_ReturnsEmptyModel(
            Exception exception)
        {
            _mediator.Setup(x => x.Send(
                    It.IsAny<GetNavigationHierarchyQuery>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            AssertEmpty(AssertView(await _component.InvokeAsync()));
            VerifyQuery(3);
        }

        [Test]
        public async Task InvokeAsync_NullMediator_ReturnsEmptyModel()
        {
            // Constructor accepts null; InvokeAsync catches the resulting error.
            var component = new TopNavigationViewComponent(null!);

            AssertEmpty(AssertView(await component.InvokeAsync()));
        }

        private void Returns(Maybe<NavigationViewModel> result)
        {
            _mediator.Setup(x => x.Send(
                    It.IsAny<GetNavigationHierarchyQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);
        }

        private void VerifyQuery(int depth)
        {
            _mediator.Verify(x => x.Send(
                    It.Is<GetNavigationHierarchyQuery>(q =>
                        q.RootPath == "/Shared/Menus/Top-Menu" &&
                        q.MaxDepth == depth),
                    It.Is<CancellationToken>(
                        t => t == CancellationToken.None)),
                Times.Once);

            _mediator.VerifyNoOtherCalls();
        }

        private static NavigationViewModel AssertView(object result)
        {
            Assert.That(result, Is.TypeOf<ViewViewComponentResult>());
            var view = (ViewViewComponentResult)result;

            Assert.Multiple(() =>
            {
                Assert.That(view.ViewName, Is.EqualTo(ViewPath));
                Assert.That(view.ViewData.Model,
                    Is.TypeOf<NavigationViewModel>());
            });

            return (NavigationViewModel)view.ViewData.Model!;
        }

        private static void AssertEmpty(NavigationViewModel model)
        {
            Assert.Multiple(() =>
            {
                Assert.That(model.RootItems, Is.Empty);
                Assert.That(model.Layout, Is.Null);
            });
        }
    }

    public abstract class TopNavigationCases : IEnumerable<TestCaseData>
    {
        public abstract IEnumerator<TestCaseData> GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public sealed class TopNavigationModelCases : TopNavigationCases
    {
        public override IEnumerator<TestCaseData> GetEnumerator()
        {
            foreach (var count in new[] { 0, 1, 5 })
            {
                var model = new NavigationViewModel();

                for (var i = 0; i < count; i++)
                {
                    model.RootItems.Add(new NavigationItemViewModel
                    {
                        Id = i.ToString(),
                        Name = $"Item-{i}",
                        Children = new List<NavigationItemViewModel>
                        {
                            new NavigationItemViewModel
                            {
                                Id = $"{i}-child",
                                Name = $"Child-{i}"
                            }
                        }
                    });
                }

                yield return new TestCaseData(model)
                    .SetName($"ReturnedModel_{count}_Roots");
            }

            // Unexpected model contents are forwarded, not validated.
            yield return new TestCaseData(new NavigationViewModel
            {
                RootItems = null!
            }).SetName("ReturnedModel_NullRootCollection");

            yield return new TestCaseData(new NavigationViewModel
            {
                RootItems = new List<NavigationItemViewModel> { null! }
            }).SetName("ReturnedModel_NullRootItem");
        }
    }

    public sealed class TopNavigationDepthCases : TopNavigationCases
    {
        public override IEnumerator<TestCaseData> GetEnumerator()
        {
            foreach (var depth in new[]
            {
                int.MinValue, -1, 0, 1, 3, 10, int.MaxValue
            })
            {
                yield return new TestCaseData(depth);
            }
        }
    }

    public sealed class TopNavigationFailureCases : TopNavigationCases
    {
        public override IEnumerator<TestCaseData> GetEnumerator()
        {
            foreach (var exception in new Exception[]
            {
                new InvalidOperationException("MediatR failed"),
                new ArgumentException("Unexpected query"),
                new OperationCanceledException()
            })
            {
                yield return new TestCaseData(exception)
                    .SetName($"MediatorThrows_{exception.GetType().Name}");
            }
        }
    }
}