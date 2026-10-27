using ETG.Core.Models.GoogleReviews;
using ETG.Web.Components;
using ETG.Web.Components.PageBuilder.Widgets.GoogleReviews;
using ETG.Web.Services.GoogleReviews;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using NUnit.Framework;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ETG.Web.Tests.Components.PageBuilder.Widgets;

[TestFixture]
public class GoogleReviewsWidgetTests : BaseWebTest
{
    private const string ReviewsView =
        "~/Components/PageBuilder/Widgets/GoogleReviews/GoogleReviews.cshtml";
    private const string ErrorView = "~/Components/ComponentError.cshtml";
    private const string NoData =
        "No Google Reviews data is available yet. Please run the scheduled task to fetch reviews.";

    [Test]
    public void MetadataAndDefaults_Match()
    {
        var props = new GoogleReviewsWidgetProperties();

        Assert.Multiple(() =>
        {
            Assert.That(GoogleReviewsWidget.IDENTIFIER,
                Is.EqualTo("ETG.Web.Widget.GoogleReviewsWidget"));
            Assert.That(GoogleReviewsWidget.NAME, Is.EqualTo("Google Reviews"));
            Assert.That(props.Heading, Is.EqualTo("What our customers say"));
            Assert.That(props.ViewAllUrl, Is.Empty);
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Response_ReturnsExpectedViewAndModel(bool empty)
    {
        var response = Response(empty);
        var props = Properties();
        var service = new FakeService { Response = response };

        var view = await Invoke(service, props);
        var model = SuccessModel(view);

        Assert.Multiple(() =>
        {
            Assert.That(model.Reviews, Is.SameAs(response));
            Assert.That(model.Heading, Is.EqualTo(props.Heading));
            Assert.That(model.ViewAllUrl, Is.EqualTo(props.ViewAllUrl));
            Assert.That(service.Calls, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task NullResponse_ReturnsExactError()
    {
        var service = new FakeService();
        var view = await Invoke(service, Properties());

        Assert.That(view.ViewName, Is.EqualTo(ErrorView));
        Assert.That(view.ViewData.Model,
            Is.TypeOf<ComponentErrorViewModel>());

        var error = (ComponentErrorViewModel)view.ViewData.Model!;
        Assert.Multiple(() =>
        {
            Assert.That(error.ComponentLabel,
                Is.EqualTo("Google Reviews Widget"));
            Assert.That(error.Errors, Is.EqualTo(new[] { NoData }));
            Assert.That(service.Calls, Is.EqualTo(1));
        });
    }

    [TestCase(null, null, null)]
    [TestCase("", "", null)]
    [TestCase(" \t\r\n ", " \t\r\n ", null)]
    [TestCase("Heading", "/reviews", "/reviews")]
    [TestCase("Heading", " https://example.invalid ", " https://example.invalid ")]
    public async Task TextInputs_UseCurrentNormalization(
        string? heading, string? url, string? expectedUrl)
    {
        var props = Properties();
        props.Heading = heading!;
        props.ViewAllUrl = url!;

        var service = new FakeService { Response = Response(false) };
        var model = SuccessModel(await Invoke(service, props));

        Assert.Multiple(() =>
        {
            Assert.That(model.Heading, Is.EqualTo(heading));
            Assert.That(model.ViewAllUrl, Is.EqualTo(expectedUrl));
            Assert.That(service.Calls, Is.EqualTo(1));
        });
    }

    [TestCase("javascript:alert(1)")]
    [TestCase("data:text/html,<script>alert(1)</script>")]
    [TestCase("not a url")]
    [Category("KnownValidationGap")]
    public async Task TamperedUrl_CurrentlyAccepted(string url)
    {
        var service = new FakeService { Response = Response(false) };
        var props = Properties();
        props.ViewAllUrl = url;

        var model = SuccessModel(await Invoke(service, props));

        Assert.Multiple(() =>
        {
            Assert.That(model.ViewAllUrl, Is.EqualTo(url));
            Assert.That(service.Calls, Is.EqualTo(1));
        });
    }

    [Test]
    [Category("KnownValidationGap")]
    public void NullProperties_WithData_CurrentlyThrows()
    {
        var service = new FakeService { Response = Response(false) };
        var widget = new GoogleReviewsWidget(service);

        Assert.ThrowsAsync<NullReferenceException>(
            async () => await widget.InvokeAsync(Component(null)));

        Assert.That(service.Calls, Is.EqualTo(1));
    }

    [Test]
    public void NullComponentViewModel_CurrentlyThrows()
    {
        var service = new FakeService { Response = Response(false) };
        var widget = new GoogleReviewsWidget(service);

        Assert.ThrowsAsync<NullReferenceException>(
            async () => await widget.InvokeAsync(null!));

        Assert.That(service.Calls, Is.Zero);
    }

    [Test]
    public void ServiceException_CurrentlyPropagates()
    {
        var failure = new InvalidOperationException("Simulated failure");
        var service = new FakeService { Failure = failure };
        var widget = new GoogleReviewsWidget(service);

        var actual = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await widget.InvokeAsync(Component(Properties())));

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.SameAs(failure));
            Assert.That(service.Calls, Is.EqualTo(1));
        });
    }

    private static async Task<ViewViewComponentResult> Invoke(
        FakeService service, GoogleReviewsWidgetProperties props)
    {
        var result = await new GoogleReviewsWidget(service)
            .InvokeAsync(Component(props));

        Assert.That(result, Is.TypeOf<ViewViewComponentResult>());
        return (ViewViewComponentResult)result;
    }

    private static GoogleReviewsWidgetViewModel SuccessModel(
        ViewViewComponentResult view)
    {
        Assert.That(view.ViewName, Is.EqualTo(ReviewsView));
        Assert.That(view.ViewData.Model,
            Is.TypeOf<GoogleReviewsWidgetViewModel>());

        return (GoogleReviewsWidgetViewModel)view.ViewData.Model!;
    }

    private static ComponentViewModel<GoogleReviewsWidgetProperties> Component(
        GoogleReviewsWidgetProperties? props)
    {
        var type = typeof(ComponentViewModel<GoogleReviewsWidgetProperties>);
        var component =
            (ComponentViewModel<GoogleReviewsWidgetProperties>)
            RuntimeHelpers.GetUninitializedObject(type);

        for (Type? current = type;
             current != null;
             current = current.BaseType)
        {
            const BindingFlags flags = BindingFlags.Instance |
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly;

            var property = current.GetProperty("Properties", flags);

            if (property?.GetSetMethod(true) is MethodInfo setter)
            {
                setter.Invoke(component, new object?[] { props });
                Verify();
                return component;
            }

            var field = current.GetField("<Properties>k__BackingField", flags);

            if (field != null)
            {
                field.SetValue(component, props);
                Verify();
                return component;
            }
        }

        throw new InvalidOperationException(
            "Cannot initialize Properties for this Kentico version.");

        void Verify()
        {
            if (!ReferenceEquals(component.Properties, props))
                throw new InvalidOperationException(
                    "Component.Properties was not initialized correctly.");
        }
    }

    private static GoogleReviewsWidgetProperties Properties() => new()
    {
        Heading = $"Reviews-{Guid.NewGuid():N}",
        ViewAllUrl = $"https://example.invalid/{Guid.NewGuid():N}"
    };

    private static ReviewListingResponse Response(bool empty)
    {
        var response = new ReviewListingResponse();

        if (!empty)
        {
            response.Totals.AverageRating = 5;
            response.Totals.ReviewCount = 1;
            response.Reviews.Add(new ReviewItem
            {
                Reviewer = $"Reviewer-{Guid.NewGuid():N}",
                Text = $"Review-{Guid.NewGuid():N}",
                Rating = 5,
                DateTime = DateTime.UtcNow
            });
        }

        return response;
    }

    private sealed class FakeService : IGoogleReviewsService
    {
        public ReviewListingResponse? Response { get; set; }
        public Exception? Failure { get; set; }
        public int Calls { get; private set; }

        public Task<ReviewListingResponse?> GetReviewsAsync()
        {
            Calls++;

            return Failure is Exception failure
                ? Task.FromException<ReviewListingResponse?>(failure)
                : Task.FromResult(Response);
        }

        public Task<bool> RefreshFromApiAsync(
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException(
                "The widget must not refresh reviews.");
    }
}