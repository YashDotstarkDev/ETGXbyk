using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CMS.ContentEngine;
using ETG.Core.Content;
using ETG.Core.Models.GoogleReviews;
using ETG.Web.Components.ViewComponents.StickyFooter;
using ETG.Web.Services.GoogleReviews;
using ETG.Web.Tests;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Moq;
using NUnit.Framework;

namespace ETG.Web.Tests.Components.ViewComponents;

[TestFixture]
public class StickyBarViewComponentTests : BaseWebTest
{
    private Mock<IContentQueryExecutor> _executor = null!;
    private Mock<IGoogleReviewsService> _reviews = null!;
    private StickyBarViewComponent _component = null!;

    private const string ViewPath =
        "~/Components/ViewComponents/StickyFooterBar/StickyBar.cshtml";

    [SetUp]
    public override void Setup()
    {
        base.Setup();

        _executor = new Mock<IContentQueryExecutor>();

        // Required by the real GetMappedResult extension method.
        _executor.As<IContentQueryModelTypeMapper>();

        _reviews = new Mock<IGoogleReviewsService>();

        _component = new StickyBarViewComponent(
            _executor.Object,
            _reviews.Object);
    }

    [TestCaseSource(typeof(StickyBarContentCases))]
    public async Task InvokeAsync_SelectsFirstItemAndMapsReviews(
        StickyFooterBar[] items,
        ReviewListingResponse? reviews)
    {
        _executor.Setup(x => x.GetResult<StickyFooterBar>(
                It.IsAny<ContentItemQueryBuilder>(),
                It.IsAny<Func<IContentQueryDataContainer, StickyFooterBar>>(),
                It.IsAny<ContentQueryExecutionOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<StickyFooterBar>)items);

        _reviews.Setup(x => x.GetReviewsAsync())
            .ReturnsAsync(reviews);

        var result = await _component.InvokeAsync();
        var first = items.FirstOrDefault();

        if (first == null)
        {
            Assert.That(result, Is.TypeOf<ContentViewComponentResult>());

            Assert.That(
                ((ContentViewComponentResult)result).Content,
                Is.EqualTo(string.Empty));

            _reviews.Verify(
                x => x.GetReviewsAsync(),
                Times.Never);
        }
        else
        {
            Assert.That(result, Is.TypeOf<ViewViewComponentResult>());

            var view = (ViewViewComponentResult)result;

            Assert.That(view.ViewName, Is.EqualTo(ViewPath));
            Assert.That(
                view.ViewData.Model,
                Is.TypeOf<StickyBarViewComponent.StickyBarViewModel>());

            var model =
                (StickyBarViewComponent.StickyBarViewModel)
                view.ViewData.Model!;

            Assert.Multiple(() =>
            {
                Assert.That(model.StickyFooterBar, Is.SameAs(first));

                Assert.That(
                    model.StickyFooterBar!.StickyBarContent,
                    Is.EqualTo(first.StickyBarContent));

                if (reviews == null)
                    Assert.That(model.Reviews, Is.Null);
                else
                    Assert.That(model.Reviews, Is.SameAs(reviews));
            });

            _reviews.Verify(
                x => x.GetReviewsAsync(),
                Times.Once);
        }

        _executor.Verify(x => x.GetResult<StickyFooterBar>(
                It.Is<ContentItemQueryBuilder>(q => q != null),
                It.IsAny<Func<IContentQueryDataContainer, StickyFooterBar>>(),
                It.IsAny<ContentQueryExecutionOptions>(),
                It.Is<CancellationToken>(
                    t => t == CancellationToken.None)),
            Times.Once);

        // Rendering must not trigger a Google API refresh.
        _reviews.Verify(
            x => x.RefreshFromApiAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestCaseSource(typeof(StickyBarFailureCases))]
    public void InvokeAsync_PropagatesFailures(string scenario)
    {
        var querySetup = _executor.Setup(x =>
            x.GetResult<StickyFooterBar>(
                It.IsAny<ContentItemQueryBuilder>(),
                It.IsAny<Func<IContentQueryDataContainer, StickyFooterBar>>(),
                It.IsAny<ContentQueryExecutionOptions>(),
                It.IsAny<CancellationToken>()));

        var queryFails =
            scenario == "NullCollection" ||
            scenario == "QueryException" ||
            scenario == "QueryCancellation";

        switch (scenario)
        {
            case "NullCollection":
                querySetup.ReturnsAsync(
                    (IEnumerable<StickyFooterBar>)null!);

                Assert.ThrowsAsync<ArgumentNullException>(
                    async () => { await _component.InvokeAsync(); });
                break;

            case "QueryException":
                querySetup.ThrowsAsync(
                    new InvalidOperationException("Query failed"));

                Assert.ThrowsAsync<InvalidOperationException>(
                    async () => { await _component.InvokeAsync(); });
                break;

            case "QueryCancellation":
                querySetup.ThrowsAsync(new OperationCanceledException());

                Assert.ThrowsAsync<OperationCanceledException>(
                    async () => { await _component.InvokeAsync(); });
                break;

            case "ReviewsException":
            case "ReviewsCancellation":
                querySetup.ReturnsAsync(
                    (IEnumerable<StickyFooterBar>)new[]
                    {
                        new StickyFooterBar
                        {
                            StickyBarContent = "Contact us"
                        }
                    });

                if (scenario == "ReviewsException")
                {
                    _reviews.Setup(x => x.GetReviewsAsync())
                        .ThrowsAsync(
                            new InvalidOperationException("Reviews failed"));

                    Assert.ThrowsAsync<InvalidOperationException>(
                        async () => { await _component.InvokeAsync(); });
                }
                else
                {
                    _reviews.Setup(x => x.GetReviewsAsync())
                        .ThrowsAsync(new OperationCanceledException());

                    Assert.ThrowsAsync<OperationCanceledException>(
                        async () => { await _component.InvokeAsync(); });
                }

                break;

            default:
                Assert.Fail($"Unknown scenario: {scenario}");
                return;
        }

        _reviews.Verify(
            x => x.GetReviewsAsync(),
            queryFails ? Times.Never() : Times.Once());
    }
}

// Non-static data providers.
public abstract class StickyBarCaseSource : IEnumerable<TestCaseData>
{
    public abstract IEnumerator<TestCaseData> GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public sealed class StickyBarContentCases : StickyBarCaseSource
{
    public override IEnumerator<TestCaseData> GetEnumerator()
    {
        var reviews = new ReviewListingResponse
        {
            Totals = new ReviewTotals
            {
                AverageRating = 4.5,
                ReviewCount = 1
            },
            Reviews = new List<ReviewItem>
            {
                new ReviewItem
                {
                    Reviewer = "Test Reviewer",
                    Text = "Helpful service",
                    Rating = 5
                }
            }
        };

        yield return new TestCaseData(
            Array.Empty<StickyFooterBar>(), reviews)
            .SetName("EmptyCollection_ReturnsEmptyContent");

        yield return new TestCaseData(
            new StickyFooterBar[] { null! }, reviews)
            .SetName("NullFirstItem_ReturnsEmptyContent");

        yield return new TestCaseData(
            new StickyFooterBar[]
            {
                null!,
                new StickyFooterBar { StickyBarContent = "Later item" }
            },
            reviews)
            .SetName("NullFirstItem_DoesNotSearchLaterItems");

        // Generate content-field cases without unstable random IDs.
        var index = 0;

        foreach (var value in new string?[]
        {
            "Contact us",
            null,
            "",
            " \t",
            "<script>alert(1)</script>",
            "日本語-é-مرحبا"
        })
        {
            yield return new TestCaseData(
                new[]
                {
                    new StickyFooterBar { StickyBarContent = value! }
                },
                reviews)
                .SetName($"ContentField_{index++}_IsPreserved");
        }

        // Multiple items: only the first is used.
        yield return new TestCaseData(
            Enumerable.Range(1, 2)
                .Select(i => new StickyFooterBar
                {
                    StickyBarContent = $"Content-{i}"
                })
                .ToArray(),
            reviews)
            .SetName("MultipleItems_SelectsFirst");

        yield return new TestCaseData(
            new StickyFooterBar[]
            {
                new StickyFooterBar { StickyBarContent = "First" },
                null!
            },
            reviews)
            .SetName("ValidFirstItem_IgnoresLaterNull");

        yield return new TestCaseData(
            new[]
            {
                new StickyFooterBar { StickyBarContent = "Contact us" }
            },
            null)
            .SetName("NullReviews_ReturnsView");

        yield return new TestCaseData(
            new[]
            {
                new StickyFooterBar { StickyBarContent = "Contact us" }
            },
            new ReviewListingResponse())
            .SetName("EmptyReviews_ReturnsView");
    }
}

public sealed class StickyBarFailureCases : StickyBarCaseSource
{
    public override IEnumerator<TestCaseData> GetEnumerator()
    {
        foreach (var scenario in new[]
        {
            "NullCollection",
            "QueryException",
            "QueryCancellation",
            "ReviewsException",
            "ReviewsCancellation"
        })
        {
            yield return new TestCaseData(scenario);
        }
    }
}