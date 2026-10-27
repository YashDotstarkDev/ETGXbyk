using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CMS.ContentEngine;
using ETG;
using ETG.Web.Components.ViewComponents.GenericEnquireSidebar;
using ETG.Web.Tests;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Moq;
using NUnit.Framework;

namespace ETG.Web.Tests.Components.ViewComponents;

[TestFixture]
public class GenericEnquireSidebarViewComponentTests : BaseWebTest
{
    private Mock<IContentQueryExecutor> _executor = null!;
    private GenericEnquireSidebarViewComponent _component = null!;

    private const string ViewPath =
        "~/Components/ViewComponents/GenericEnquireSidebar/GenericEnquireSidebar.cshtml";

    [SetUp]
    public override void Setup()
    {
        base.Setup();

        _executor = new Mock<IContentQueryExecutor>();

        // GetMappedResult requires this interface on the executor.
        _executor.As<IContentQueryModelTypeMapper>();

        _component = new GenericEnquireSidebarViewComponent(
            _executor.Object);
    }

    [TestCaseSource(typeof(SidebarContentCases))]
    public async Task InvokeAsync_FiltersNullItemsAndReturnsExpectedResult(
        EnquiryContact[] contacts,
        int expectedCount)
    {
        _executor.Setup(x => x.GetResult<EnquiryContact>(
                It.IsAny<ContentItemQueryBuilder>(),
                It.IsAny<Func<IContentQueryDataContainer, EnquiryContact>>(),
                It.IsAny<ContentQueryExecutionOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<EnquiryContact>)contacts);

        var result = await _component.InvokeAsync();

        if (expectedCount == 0)
        {
            Assert.That(result, Is.TypeOf<ContentViewComponentResult>());

            Assert.That(
                ((ContentViewComponentResult)result).Content,
                Is.EqualTo(string.Empty));
        }
        else
        {
            Assert.That(result, Is.TypeOf<ViewViewComponentResult>());

            var view = (ViewViewComponentResult)result;

            Assert.That(view.ViewName, Is.EqualTo(ViewPath));
            Assert.That(
                view.ViewData.Model,
                Is.TypeOf<GenericEnquireSidebarViewModel>());

            var model =
                (GenericEnquireSidebarViewModel)view.ViewData.Model!;

            Assert.That(model.EnquiryContacts.Count,
                Is.EqualTo(expectedCount));

            Assert.That(model.EnquiryContacts,
                Has.None.Null);

            var expected = contacts.Where(x => x != null).ToArray();

            // Original objects and order must be preserved.
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.That(
                    model.EnquiryContacts[i],
                    Is.SameAs(expected[i]));
            }
        }

        _executor.Verify(x => x.GetResult<EnquiryContact>(
                It.IsAny<ContentItemQueryBuilder>(),
                It.IsAny<Func<IContentQueryDataContainer, EnquiryContact>>(),
                It.IsAny<ContentQueryExecutionOptions>(),
                It.Is<CancellationToken>(t => t == CancellationToken.None)),
            Times.Once);
    }

    [TestCaseSource(typeof(SidebarFieldCases))]
    public async Task InvokeAsync_PreservesFieldsWithoutValidation(
        string? value)
    {
        var contact = new EnquiryContact
        {
            Name = value!,
            Phone = value!,
            Address = value!,
            Email = value!,
            DocumentName = value!
        };

        _executor.Setup(x => x.GetResult<EnquiryContact>(
                It.IsAny<ContentItemQueryBuilder>(),
                It.IsAny<Func<IContentQueryDataContainer, EnquiryContact>>(),
                It.IsAny<ContentQueryExecutionOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                (IEnumerable<EnquiryContact>)new[] { contact });

        var result = await _component.InvokeAsync();

        Assert.That(result, Is.TypeOf<ViewViewComponentResult>());

        var view = (ViewViewComponentResult)result;

        Assert.That(view.ViewName, Is.EqualTo(ViewPath));
        Assert.That(view.ViewData.Model,
            Is.TypeOf<GenericEnquireSidebarViewModel>());

        var model =
            (GenericEnquireSidebarViewModel)view.ViewData.Model!;

        var actual = model.EnquiryContacts.Single();

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.SameAs(contact));
            Assert.That(actual.Name, Is.EqualTo(value));
            Assert.That(actual.Phone, Is.EqualTo(value));
            Assert.That(actual.Address, Is.EqualTo(value));
            Assert.That(actual.Email, Is.EqualTo(value));
            Assert.That(actual.DocumentName, Is.EqualTo(value));
        });
    }

    [TestCaseSource(typeof(SidebarFailureCases))]
    public void InvokeAsync_PropagatesFailures(string scenario)
    {
        var setup = _executor.Setup(x => x.GetResult<EnquiryContact>(
            It.IsAny<ContentItemQueryBuilder>(),
            It.IsAny<Func<IContentQueryDataContainer, EnquiryContact>>(),
            It.IsAny<ContentQueryExecutionOptions>(),
            It.IsAny<CancellationToken>()));

        switch (scenario)
        {
            case "NullCollection":
                setup.ReturnsAsync(
                    (IEnumerable<EnquiryContact>)null!);

                Assert.ThrowsAsync<ArgumentNullException>(
                    async () => { await _component.InvokeAsync(); });
                break;

            case "QueryException":
                setup.ThrowsAsync(
                    new InvalidOperationException("Query failed"));

                Assert.ThrowsAsync<InvalidOperationException>(
                    async () => { await _component.InvokeAsync(); });
                break;

            case "Cancellation":
                setup.ThrowsAsync(new OperationCanceledException());

                Assert.ThrowsAsync<OperationCanceledException>(
                    async () => { await _component.InvokeAsync(); });
                break;

            default:
                Assert.Fail($"Unknown scenario: {scenario}");
                break;
        }
    }
}

// Non-static data providers.
public abstract class SidebarCaseSource : IEnumerable<TestCaseData>
{
    public abstract IEnumerator<TestCaseData> GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public sealed class SidebarContentCases : SidebarCaseSource
{
    public override IEnumerator<TestCaseData> GetEnumerator()
    {
        yield return new TestCaseData(
            Array.Empty<EnquiryContact>(), 0);

        yield return new TestCaseData(
            new EnquiryContact[] { null!, null! }, 0);

        // Generate collection-size boundary cases.
        foreach (var count in new[] { 1, 2 })
        {
            var contacts = Enumerable.Range(1, count)
                .Select(i => new EnquiryContact
                {
                    Name = $"Contact-{i}",
                    Email = $"contact{i}@example.com",
                    Phone = $"12345{i}",
                    Address = $"Address-{i}",
                    DocumentName = $"Contact-{i}"
                })
                .ToArray();

            yield return new TestCaseData(contacts, count);

            yield return new TestCaseData(
                new EnquiryContact[] { null! }
                    .Concat(contacts)
                    .Concat(new EnquiryContact[] { null! })
                    .ToArray(),
                count);
        }
    }
}

public sealed class SidebarFieldCases : SidebarCaseSource
{
    public override IEnumerator<TestCaseData> GetEnumerator()
    {
        foreach (var value in new string?[]
        {
            null,
            "",
            " \t",
            "not-an-email",
            "<script>alert(1)</script>",
            "日本語-é-مرحبا"
        })
        {
            yield return new TestCaseData(new object?[] { value });
        }
    }
}

public sealed class SidebarFailureCases : SidebarCaseSource
{
    public override IEnumerator<TestCaseData> GetEnumerator()
    {
        foreach (var scenario in new[]
        {
            "NullCollection", "QueryException", "Cancellation"
        })
        {
            yield return new TestCaseData(scenario);
        }
    }
}