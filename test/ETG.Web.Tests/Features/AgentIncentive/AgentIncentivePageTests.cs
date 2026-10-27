using CMS.Websites.Routing;
using ETG.Core.Content;
using ETG.Web.Features.AgentIncentive;
using ETG.Web.Features.AgentIncentive.Operations;
using ETG.Web.Infrastructure;
using ETG.Web.Tests;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ETG.Web.Tests.Features.AgentIncentive
{
    [TestFixture]
    public class AgentIncentivePageTests : BaseWebTest
    {
        private const string TemplateIdentifier =
            "ETG.AgentIncentiveTemplate";

        private const string TemplateView =
            "~/Features/AgentIncentive/AgentIncentivePage_Default.cshtml";

        private Mock<IMediator> _mediator = null!;
        private Mock<IWebPageDataContextRetriever> _contexts = null!;
        private Mock<IWebsiteChannelContext> _channel = null!;
        private DefaultHttpContext _http = null!;
        private WebPageMetaService _meta = null!;

        [SetUp]
        public override void Setup()
        {
            base.Setup();

            _mediator = new Mock<IMediator>(MockBehavior.Strict);
            _contexts = new Mock<IWebPageDataContextRetriever>();
            _channel = new Mock<IWebsiteChannelContext>(
                MockBehavior.Strict);

            // Unconfigured TryRetrieve returns false.
            _http = new DefaultHttpContext();
            _http.Request.Scheme = "https";
            _http.Request.Host = new HostString("example.test");

            _meta = new WebPageMetaService(
                _mediator.Object,
                new HttpContextAccessor
                {
                    HttpContext = _http
                });
        }

        [TestCaseSource(typeof(AgentIncentiveCancellationCases))]
        public async Task LoadViewModelAsync_MissingContext_ReturnsNull(
            bool cancelled)
        {
            using var source = new CancellationTokenSource();

            if (cancelled)
                source.Cancel();

            var result =
                await AgentIncentivePageTemplateController
                    .LoadViewModelAsync(
                        _mediator.Object,
                        _contexts.Object,
                        _channel.Object,
                        source.Token);

            Assert.That(result, Is.Null);

            _mediator.VerifyNoOtherCalls();
            _channel.VerifyNoOtherCalls();
        }

        [TestCaseSource(typeof(AgentIncentiveCancellationCases))]
        public async Task Index_MissingContext_Returns404(
            bool cancelled)
        {
            using var source = new CancellationTokenSource();

            if (cancelled)
                source.Cancel();

            _http.RequestAborted = source.Token;

            var controller = new AgentIncentivePageTemplateController(
                _mediator.Object,
                _contexts.Object,
                _channel.Object,
                _meta)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = _http
                }
            };

            var result = await controller.Index();

            Assert.That(result, Is.TypeOf<NotFoundResult>());

            Assert.That(
                ((NotFoundResult)result).StatusCode,
                Is.EqualTo(StatusCodes.Status404NotFound));

            _mediator.VerifyNoOtherCalls();
            _channel.VerifyNoOtherCalls();
        }

        [Test]
        public void Registration_ContainsExactIdentifierAndView()
        {
            var registration =
                typeof(AgentIncentivePageTemplateController)
                    .Assembly
                    .GetCustomAttributesData()
                    .SingleOrDefault(attribute =>
                        attribute.AttributeType.Name ==
                            "RegisterPageTemplateAttribute" &&
                        attribute.ConstructorArguments.Any(argument =>
                            Equals(
                                argument.Value,
                                TemplateIdentifier)));

            Assert.That(
                registration,
                Is.Not.Null,
                $"Missing registration: {TemplateIdentifier}");

            var values = registration!.ConstructorArguments
                .Select(argument => argument.Value)
                .Concat(
                    registration.NamedArguments.Select(
                        argument => argument.TypedValue.Value))
                .ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(
                    values,
                    Does.Contain(TemplateIdentifier));

                Assert.That(
                    values,
                    Does.Contain(TemplateView));

                Assert.That(
                    values,
                    Does.Contain(
                        typeof(AgentIncentivePageTemplateProperties)));
            });
        }

        [Test]
        public void ViewModel_Defaults_AreExpected()
        {
            var model = new AgentIncentivePageViewModel();

            Assert.Multiple(() =>
            {
                Assert.That(model.Page, Is.Null);
                Assert.That(model.Heading, Is.Empty);
                Assert.That(model.HeroImageUrl, Is.Null);
                Assert.That(model.HeroImageAlt, Is.Empty);
            });
        }

        [TestCaseSource(typeof(AgentIncentiveTextCases))]
        public async Task MetadataService_MapsPageMetadata(
            string? value,
            string expectedTitleAndDescription)
        {
            var page = new Incentive
            {
                MetaDataSchema__PageTitle = value!,
                MetaDataSchema__PageDescription = value!,
                MetaDataSchema__PageKeywords = value!
            };

            _meta.SetMeta(page, "/images/hero.jpg");

            var actual = await _meta.GetMeta();

            Assert.Multiple(() =>
            {
                // Current behavior: null/blank title and description
                // become empty strings.
                Assert.That(
                    actual.Title,
                    Is.EqualTo(expectedTitleAndDescription));

                Assert.That(
                    actual.Description,
                    Is.EqualTo(expectedTitleAndDescription));

                // Keywords preserve the supplied value.
                Assert.That(
                    actual.Keywords,
                    Is.EqualTo(value));

                Assert.That(
                    actual.ShareImage,
                    Is.EqualTo(
                        "https://example.test/images/hero.jpg"));
            });
        }

        [TestCaseSource(typeof(AgentIncentiveImageUrlCases))]
        public async Task MetadataService_NormalizesShareImage(
            string? input,
            string expected)
        {
            var page = new Incentive
            {
                MetaDataSchema__PageTitle = "Incentive",
                MetaDataSchema__PageDescription = "",
                MetaDataSchema__PageKeywords = ""
            };

            _meta.SetMeta(page, input);

            var actual = await _meta.GetMeta();

            Assert.That(
                actual.ShareImage,
                Is.EqualTo(expected));
        }

        [TestCaseSource(typeof(AgentIncentivePreviewCases))]
        public void Query_NullPage_CacheKeyThrows(bool preview)
        {
            var query = new AgentIncentivePageQuery(
                null!,
                preview);

            Assert.That(
                query.ForPreview,
                Is.EqualTo(preview));

            Assert.Throws<NullReferenceException>(() =>
            {
                _ = query.CacheValueKey;
            });
        }
    }

    public abstract class AgentIncentiveCases
        : IEnumerable<TestCaseData>
    {
        public abstract IEnumerator<TestCaseData> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public sealed class AgentIncentiveCancellationCases
        : AgentIncentiveCases
    {
        public override IEnumerator<TestCaseData> GetEnumerator()
        {
            foreach (var cancelled in new[] { false, true })
            {
                yield return new TestCaseData(cancelled);
            }
        }
    }

    public sealed class AgentIncentivePreviewCases
        : AgentIncentiveCases
    {
        public override IEnumerator<TestCaseData> GetEnumerator()
        {
            foreach (var preview in new[] { false, true })
            {
                yield return new TestCaseData(preview);
            }
        }
    }

    public sealed class AgentIncentiveTextCases
        : AgentIncentiveCases
    {
        public override IEnumerator<TestCaseData> GetEnumerator()
        {
            foreach (var value in new string?[]
            {
                null,
                "",
                " "
            })
            {
                yield return new TestCaseData(
                    new object?[] { value, string.Empty });
            }

            foreach (var value in new[]
            {
                "Agent incentive",
                "日本語-é-مرحبا",
                "<script>alert(1)</script>"
            })
            {
                yield return new TestCaseData(value, value);
            }
        }
    }

    public sealed class AgentIncentiveImageUrlCases
        : AgentIncentiveCases
    {
        public override IEnumerator<TestCaseData> GetEnumerator()
        {
            foreach (var value in new string?[]
            {
                null,
                "",
                " "
            })
            {
                yield return new TestCaseData(
                    new object?[] { value, string.Empty });
            }

            foreach (var prefix in new[] { "/", "~/", "" })
            {
                yield return new TestCaseData(
                    prefix + "images/hero.jpg",
                    "https://example.test/images/hero.jpg");
            }

            yield return new TestCaseData(
                "//cdn.example.test/hero.jpg",
                "https://cdn.example.test/hero.jpg");

            foreach (var value in new[]
            {
                "javascript:alert(1)",
                "data:text/html,test"
            })
            {
                yield return new TestCaseData(
                    value,
                    string.Empty);
            }
        }
    }
}