using System.Collections;
using CSharpFunctionalExtensions;
using ETG.Web.Components.ViewComponents.Footer;
using MediatR;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Moq;

namespace ETG.Web.Tests.Components.ViewComponents;

[TestFixture]
public class FooterViewComponentTests : BaseWebTest
{
    private Mock<IMediator> _mediator = null!;
    private FooterViewComponent _component = null!;

    private const string ViewPath =
        "~/Components/ViewComponents/Footer/Footer.cshtml";

    [SetUp]
    public override void Setup()
    {
        base.Setup();

        _mediator = new Mock<IMediator>();
        _component = new FooterViewComponent(_mediator.Object);
    }

    [TestCaseSource(typeof(FooterResponseCases))]
    public async Task InvokeAsync_ReturnsModelOrFallback(string scenario)
    {
        var supplied = new FooterViewModel
        {
            SiteName = "Test Site",
            Tagline = "Explore",
            Contact = new FooterContactViewModel
            {
                EmailAddress = "hello@example.com"
            },
            Columns = new[]
            {
                new FooterColumnViewModel
                {
                    Heading = "Company",
                    Links = new[]
                    {
                        new FooterLinkViewModel
                        {
                            Label = "About",
                            Url = "/about"
                        },
                        new FooterLinkViewModel
                        {
                            Label = "Contact",
                            Url = "/contact"
                        }
                    }
                }
            }
        };

        var setup = _mediator.Setup(x => x.Send(
            It.IsAny<GetFooterQuery>(),
            It.IsAny<CancellationToken>()));

        switch (scenario)
        {
            case "Value":
                setup.ReturnsAsync(Maybe.From(supplied));
                break;

            case "EmptyModel":
                setup.ReturnsAsync(Maybe.From(new FooterViewModel()));
                break;

            case "None":
                // Represents an absent/null footer value.
                setup.ReturnsAsync(Maybe<FooterViewModel>.None);
                break;

            case "Exception":
                setup.ThrowsAsync(
                    new InvalidOperationException("Query failed"));
                break;

            case "Cancellation":
                setup.ThrowsAsync(new OperationCanceledException());
                break;

            default:
                Assert.Fail($"Unknown scenario: {scenario}");
                return;
        }

        var model = await InvokeModel();

        if (scenario == "Value")
        {
            // The component passes the model through, without remapping.
            Assert.That(model, Is.SameAs(supplied));

            Assert.Multiple(() =>
            {
                Assert.That(model.SiteName, Is.EqualTo("Test Site"));
                Assert.That(model.HasTagline, Is.True);
                Assert.That(model.Contact!.EmailAddress,
                    Is.EqualTo("hello@example.com"));

                Assert.That(
                    model.Columns.Single().Links.Select(x => x.Label),
                    Is.EqualTo(new[] { "About", "Contact" }));

                Assert.That(
                    model.Columns.Single().Links.Select(x => x.Url),
                    Is.EqualTo(new[] { "/about", "/contact" }));
            });
        }
        else
        {
            // Defaults are checked as observable empty/fallback output.
            Assert.Multiple(() =>
            {
                Assert.That(model.Columns, Is.Empty);
                Assert.That(model.BottomLinks, Is.Empty);
                Assert.That(model.SocialLinks, Is.Empty);
                Assert.That(model.Contact, Is.Null);
                Assert.That(model.Layout, Is.Null);
                Assert.That(model.Logo, Is.Null);
                Assert.That(model.SiteName, Is.Empty);
                Assert.That(model.Tagline, Is.Empty);
                Assert.That(model.HasTagline, Is.False);
                Assert.That(model.CopyrightText, Is.Empty);
            });
        }

        _mediator.Verify(
            x => x.Send(
                It.IsAny<GetFooterQuery>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestCaseSource(typeof(FooterFieldCases))]
    public async Task InvokeAsync_PreservesFieldsWithoutValidation(
        string? value, bool hasTagline)
    {
        var supplied = new FooterViewModel
        {
            Tagline = value!,
            Contact = new FooterContactViewModel
            {
                EmailAddress = value,
                PhoneNumber = value
            },
            BottomLinks = new[]
            {
                new FooterBottomLinkViewModel
                {
                    Label = "Link",
                    Url = value!
                }
            }
        };

        _mediator.Setup(x => x.Send(
                It.IsAny<GetFooterQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(supplied));

        var model = await InvokeModel();

        Assert.Multiple(() =>
        {
            Assert.That(model, Is.SameAs(supplied));
            Assert.That(model.HasTagline, Is.EqualTo(hasTagline));
            Assert.That(model.Contact!.EmailAddress, Is.EqualTo(value));
            Assert.That(model.Contact.PhoneNumber, Is.EqualTo(value));
            Assert.That(model.BottomLinks.Single().Url, Is.EqualTo(value));
        });
    }

    [TestCaseSource(typeof(FooterUnsafeUrlCases))]
    public async Task InvokeAsync_PreservesSuppliedUrl(string url)
    {
        var supplied = new FooterViewModel
        {
            BottomLinks = new[]
            {
            new FooterBottomLinkViewModel { Url = url }
        }
        };

        _mediator.Setup(x => x.Send(
                It.IsAny<GetFooterQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(supplied));

        var model = await InvokeModel();

        Assert.Multiple(() =>
        {
            Assert.That(model, Is.SameAs(supplied));
            Assert.That(model.BottomLinks.Single().Url, Is.EqualTo(url));
        });
    }

    private async Task<FooterViewModel> InvokeModel()
    {
        var result = await _component.InvokeAsync();

        Assert.That(result, Is.TypeOf<ViewViewComponentResult>());

        var view = (ViewViewComponentResult)result;

        Assert.That(view.ViewName, Is.EqualTo(ViewPath));
        Assert.That(view.ViewData.Model, Is.TypeOf<FooterViewModel>());

        return (FooterViewModel)view.ViewData.Model!;
    }
}

public abstract class FooterCaseSource : IEnumerable<TestCaseData>
{
    public abstract IEnumerator<TestCaseData> GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public sealed class FooterResponseCases : FooterCaseSource
{
    public override IEnumerator<TestCaseData> GetEnumerator()
    {
        foreach (var scenario in new[]
        {
            "Value", "EmptyModel", "None", "Exception", "Cancellation"
        })
        {
            yield return new TestCaseData(scenario);
        }
    }
}

public sealed class FooterFieldCases : FooterCaseSource
{
    public override IEnumerator<TestCaseData> GetEnumerator()
    {
        yield return new TestCaseData(null, false);
        yield return new TestCaseData("", false);
        yield return new TestCaseData(" \t", false);
        yield return new TestCaseData("/about", true);
        yield return new TestCaseData("not a valid URL", true);
        yield return new TestCaseData("javascript:alert(1)", true);
    }
}

public sealed class FooterUnsafeUrlCases : FooterCaseSource
{
    public override IEnumerator<TestCaseData> GetEnumerator()
    {
        yield return new TestCaseData("javascript:alert(1)");
        yield return new TestCaseData("data:text/html,test");
    }
}