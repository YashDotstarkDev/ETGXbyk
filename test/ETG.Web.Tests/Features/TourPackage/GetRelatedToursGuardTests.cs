using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Content;
using ETG.Web.Features.TourPackage.Operations;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ETG.Web.Tests.Features.TourPackage;

[TestFixture]
public class GetRelatedToursGuardTests : BaseWebTest
{
    private Mock<IContentQueryExecutor> executor = null!;
    private Mock<IWebsiteChannelContext> channel = null!;
    private GetRelatedToursQueryHandler handler = null!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();

        executor = new(MockBehavior.Strict);
        channel = new(MockBehavior.Strict);

        handler = new GetRelatedToursQueryHandler(
            executor.Object,
            channel.Object);
    }

    private static IEnumerable<TestCaseData> MissingContentCases()
    {
        yield return new TestCaseData(null, false)
            .SetName("RelatedTours_NullTour_ReturnsEmpty");

        yield return new TestCaseData(null, true)
            .SetName("RelatedTours_NullTour_CancelledToken_ReturnsEmpty");

        var codes = new (string Name, string? Value)[]
        {
            ("NullCode", null),
            ("EmptyCode", ""),
            ("WhitespaceCode", " \t"),
            ("ValidCode", "TOUR-001"),
            ("UnexpectedCode", "<script>alert(1)</script>")
        };

        foreach (var (name, value) in codes)
        {
            foreach (var cancelled in new[] { false, true })
            {
                var tour = new Tour
                {
                    SystemFields = null!,
                    TourCode = value!
                };

                yield return new TestCaseData(tour, cancelled)
                    .SetName(
                        $"RelatedTours_MissingSystemFields_{name}_" +
                        $"{(cancelled ? "Cancelled" : "Active")}Token");
            }
        }
    }

    [TestCaseSource(nameof(MissingContentCases))]
    public async Task Handle_MissingTourOrSystemFields_ReturnsEmpty(
        Tour? tour,
        bool cancelled)
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();

        if (cancelled)
        {
            cancellation.Cancel();
        }

        var request = new GetRelatedToursQuery(tour!);

        // Act
        var result = await handler.Handle(
            request,
            cancellation.Token);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        });

        // The guard must return before reading channel configuration
        // or querying CMS content.
        channel.VerifyNoOtherCalls();
        executor.VerifyNoOtherCalls();
    }

    [Test]
    [Category("CurrentBehavior")]
    public void Handle_NullRequest_ThrowsBeforeCallingDependencies()
    {
        // Arrange / Act
        var actual = Assert.ThrowsAsync<NullReferenceException>(
            async () => await handler.Handle(
                null!,
                CancellationToken.None));

        // Assert
        Assert.That(actual, Is.Not.Null);

        channel.VerifyNoOtherCalls();
        executor.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Handle_MissingContent_ReturnsIndependentLists()
    {
        // Arrange
        var request = new GetRelatedToursQuery(null!);

        // Act
        var first = await handler.Handle(
            request,
            CancellationToken.None);

        first.Add(new Tour());

        var second = await handler.Handle(
            request,
            CancellationToken.None);

        // Assert: mutating one response cannot contaminate another.
        Assert.Multiple(() =>
        {
            Assert.That(first, Has.Count.EqualTo(1));
            Assert.That(second, Is.Empty);
            Assert.That(second, Is.Not.SameAs(first));
        });

        channel.VerifyNoOtherCalls();
        executor.VerifyNoOtherCalls();
    }
}