using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CMS.Websites.Routing;
using ETG;
using ETG.Core.Content;
using ETG.Web.Components.ViewComponents.PageCustomMeta;
using ETG.Web.Infrastructure;
using ETG.Web.Tests;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using NUnit.Framework;

namespace ETG.Web.Tests.Components.ViewComponents;

[TestFixture]
public class PageCustomMetaViewComponentTests : BaseWebTest
{
    private Mock<IMediator> _mediator = null!;
    private Mock<IWebsiteChannelContext> _channel = null!;
    private DefaultHttpContext _context = null!;
    private WebPageMetaService _metaService = null!;
    private PageCustomMetaViewComponent _component = null!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();

        _mediator = new Mock<IMediator>();
        _channel = new Mock<IWebsiteChannelContext>();

        _context = new DefaultHttpContext();
        _context.Request.Scheme = "https";
        _context.Request.Host = new HostString("example.com");

        _metaService = new WebPageMetaService(
            _mediator.Object,
            new HttpContextAccessor { HttpContext = _context });

        SetSettings("verification-code");

        _channel.SetupGet(x => x.WebsiteChannelName).Returns("ETG");

        _component = new PageCustomMetaViewComponent(
            _metaService, _mediator.Object, _channel.Object)
        {
            ViewComponentContext = new ViewComponentContext
            {
                ViewContext = new ViewContext
                {
                    HttpContext = _context,
                    ViewData = new ViewDataDictionary<object>(
                        new EmptyModelMetadataProvider(),
                        new ModelStateDictionary())
                }
            }
        };
    }

    [TestCaseSource(typeof(MetaValueCases))]
    public async Task InvokeAsync_MapsMetadataAndSettings(string? value)
    {
        var image = "https://example.com/share.jpg";

        _metaService.SetMeta(new WebPageMeta(value!, value!, value!)
        {
            ShareImage = image
        });

        SetSettings(value);

        _channel.SetupGet(x => x.WebsiteChannelName).Returns(value!);

        var model = await InvokeModel();

        Assert.Multiple(() =>
        {
            Assert.That(model.Title, Is.EqualTo(value));
            Assert.That(model.Description, Is.EqualTo(value));
            Assert.That(model.Keywords, Is.EqualTo(value));
            Assert.That(model.ShareImage, Is.EqualTo(image));
            Assert.That(model.GoogleVerificationCode, Is.EqualTo(value ?? ""));
            Assert.That(model.site_name, Is.EqualTo(value ?? ""));
        });

        _mediator.Verify(x => x.Send(
                It.IsAny<WebsiteSettingsQuery>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestCaseSource(typeof(MetaUrlCases))]
    public async Task InvokeAsync_BuildsCanonicalWithoutQuery(
        string scheme, string host, string pathBase,
        string path, string expected)
    {
        _context.Request.Scheme = scheme;
        _context.Request.Host = new HostString(host);
        _context.Request.PathBase = new PathString(pathBase);
        _context.Request.Path = new PathString(path);
        _context.Request.QueryString = new QueryString("?tracking=test");

        var model = await InvokeModel();

        Assert.That(model.CanonicalUrl, Is.EqualTo(expected));
    }

    [TestCaseSource(typeof(MetaErrorCases))]
    public void InvokeAsync_PropagatesFailures(string scenario)
    {
        switch (scenario)
        {
            case "MediatorException":
                _mediator.Setup(x => x.Send(
                        It.IsAny<WebsiteSettingsQuery>(),
                        It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new InvalidOperationException());

                Assert.ThrowsAsync<InvalidOperationException>(
                    async () => { await _component.InvokeAsync(); });
                break;

            case "MissingGlobalContent":
                _mediator.Setup(x => x.Send(
                        It.IsAny<WebsiteSettingsQuery>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(
                        new WebsiteSettingsQueryResponse(null!, null!));

                Assert.ThrowsAsync<NullReferenceException>(
                    async () => { await _component.InvokeAsync(); });
                break;

            case "MissingHttpContext":
                _component.ViewContext.HttpContext = null!;

                Assert.ThrowsAsync<NullReferenceException>(
                    async () => { await _component.InvokeAsync(); });
                break;

            default:
                Assert.Fail($"Unknown scenario: {scenario}");
                break;
        }
    }

    private void SetSettings(string? verificationCode)
    {
        _mediator.Setup(x => x.Send(
                It.IsAny<WebsiteSettingsQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WebsiteSettingsQueryResponse(
                null!,
                new WebsiteGlobalContent
                {
                    GoogleVerificationCode = verificationCode!
                }));
    }

    private async Task<PageCustomMetaViewModel> InvokeModel()
    {
        var result = await _component.InvokeAsync();

        Assert.That(result, Is.TypeOf<ViewViewComponentResult>());

        var view = (ViewViewComponentResult)result;

        Assert.That(view.ViewName, Is.EqualTo(
            "~/Components/ViewComponents/PageCustomMeta/PageCustomMeta.cshtml"));

        Assert.That(view.ViewData.Model,
            Is.TypeOf<PageCustomMetaViewModel>());

        return (PageCustomMetaViewModel)view.ViewData.Model!;
    }
}

public abstract class MetaCaseSource : IEnumerable<TestCaseData>
{
    public abstract IEnumerator<TestCaseData> GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public sealed class MetaValueCases : MetaCaseSource
{
    public override IEnumerator<TestCaseData> GetEnumerator()
    {
        foreach (var value in new string?[]
        {
            $"Metadata-{Guid.NewGuid():N}",
            null,
            "",
            " \t",
            "<unexpected>"
        })
        {
            yield return new TestCaseData(new object?[] { value });
        }
    }
}

public sealed class MetaUrlCases : MetaCaseSource
{
    public override IEnumerator<TestCaseData> GetEnumerator()
    {
        foreach (var scheme in new[] { "https", "http" })
            foreach (var pathBase in new[] { "", "/app" })
            {
                const string host = "example.com";
                const string path = "/test-page";

                yield return new TestCaseData(
                    scheme,
                    host,
                    pathBase,
                    path,
                    $"{scheme}://{host}{pathBase}{path}")
                    .SetName(
                        $"Canonical_{scheme}_{(pathBase == "" ? "Root" : "App")}");
            }
    }
}

public sealed class MetaErrorCases : MetaCaseSource
{
    public override IEnumerator<TestCaseData> GetEnumerator()
    {
        foreach (var scenario in new[]
        {
            "MediatorException",
            "MissingGlobalContent",
            "MissingHttpContext"
        })
        {
            yield return new TestCaseData(scenario);
        }
    }
}