using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CMS.ContentEngine;
using CMS.Websites;
using ETG.Core.Content;
using ETG.Core.Operations;
using ETG.Web.Features.AgentPortal;
using ETG.Web.Infrastructure;
using ETG.Web.Tests;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ETG.Web.Tests.Features.AgentPortal
{
    [TestFixture]
    public class AgentPortalTests : BaseWebTest
    {
        private const string Identifier =
            "Website.AgentPortalPage_Default";

        private const string ViewPath =
            "~/Features/AgentPortal/AgentPortalPage_Default.cshtml";

        private Mock<IMediator> _mediator = null!;
        private Mock<IContentQueryExecutor> _executor = null!;
        private Mock<IContentRetriever> _content = null!;
        private Mock<ILogger<AgentPortalPageTemplateController>> _logger
            = null!;

        private AgentPortalPageTemplateController _controller = null!;
        private AgentPortalPageQueryHandler _handler = null!;

        [SetUp]
        public override void Setup()
        {
            base.Setup();

            _mediator = new Mock<IMediator>(MockBehavior.Strict);
            _executor = new Mock<IContentQueryExecutor>();

            // Required by the mapped-result extension.
            _executor.As<IContentQueryModelTypeMapper>();

            _content = new Mock<IContentRetriever>(MockBehavior.Strict);
            _logger =
                new Mock<ILogger<AgentPortalPageTemplateController>>();

            // Unconfigured TryRetrieve returns false.
            var contexts = new Mock<IWebPageDataContextRetriever>();
            var http = new DefaultHttpContext();

            var meta = new WebPageMetaService(
                _mediator.Object,
                new HttpContextAccessor { HttpContext = http });

            _controller = new AgentPortalPageTemplateController(
                _mediator.Object,
                contexts.Object,
                _content.Object,
                _logger.Object,
                meta,
                _executor.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = http
                }
            };

            // These other tools are not accessed by Handle().
            _handler = new AgentPortalPageQueryHandler(
                new WebPageQueryTools(
                    _executor.Object,
                    null!,
                    null!,
                    null!));
        }

        [TearDown]
        public override void TearDown()
        {
            _controller?.Dispose();
            base.TearDown();
        }


        [TestCaseSource(typeof(PortalBooleanCases))]
        public async Task Index_MissingContext_Returns404(
            bool cancelled)
        {
            using var source = new CancellationTokenSource();

            if (cancelled)
                source.Cancel();

            _controller.HttpContext.RequestAborted = source.Token;

            var result = await _controller.Index();

            Assert.That(result, Is.TypeOf<NotFoundResult>());
            Assert.That(
                ((NotFoundResult)result).StatusCode,
                Is.EqualTo(StatusCodes.Status404NotFound));

            _mediator.VerifyNoOtherCalls();
            _executor.VerifyNoOtherCalls();
            _content.VerifyNoOtherCalls();
        }

        [Test]
        public void Index_NullContextDependency_PropagatesException()
        {
            var controller = new AgentPortalPageTemplateController(
                _mediator.Object,
                null!,
                _content.Object,
                _logger.Object,
                null!,
                _executor.Object);

            Assert.ThrowsAsync<NullReferenceException>(
                async () => { await controller.Index(); });
        }

        [TestCaseSource(typeof(PortalResultCases))]
        public async Task Handle_ReturnsFirstPage_OrThrowsWhenEmpty(
            int count)
        {
            var pages = Enumerable.Range(1, count)
                .Select(index => new AgentPortalMainPage
                {
                    AgentPortalHeading = $"Portal-{index}"
                })
                .ToArray();

            SetResults(pages);

            using var source = new CancellationTokenSource();

            if (count == 0)
            {
                Assert.ThrowsAsync<InvalidOperationException>(
                    async () =>
                    {
                        await _handler.Handle(
                            CreateRequest(), source.Token);
                    });
            }
            else
            {
                var actual = await _handler.Handle(
                    CreateRequest(), source.Token);

                Assert.That(actual, Is.SameAs(pages[0]));
            }

            VerifyExecution(source.Token);
        }

        [TestCaseSource(typeof(PortalTextCases))]
        public async Task Handle_PreservesReturnedPageFields(
            string? value)
        {
            var page = new AgentPortalMainPage
            {
                AgentPortalHeading = value!,
                AgentPortalMainDescription = value!
            };

            SetResults(new[] { page });

            var actual = await _handler.Handle(
                CreateRequest(),
                CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(actual, Is.SameAs(page));
                Assert.That(
                    actual.AgentPortalHeading,
                    Is.EqualTo(value));
                Assert.That(
                    actual.AgentPortalMainDescription,
                    Is.EqualTo(value));
            });

            VerifyExecution(CancellationToken.None);
        }

        [TestCaseSource(typeof(PortalFailureCases))]
        public void Handle_PropagatesExecutorFailures(
            Exception expected)
        {
            // Mock the interface method, not GetWebPageResult.
            _executor.Setup(x =>
                x.GetResult<AgentPortalMainPage>(
                    It.IsAny<ContentItemQueryBuilder>(),
                    It.IsAny<Func<IContentQueryDataContainer,
                        AgentPortalMainPage>>(),
                    It.IsAny<ContentQueryExecutionOptions>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(expected);

            using var source = new CancellationTokenSource();

            if (expected is OperationCanceledException)
                source.Cancel();

            var actual = Assert.ThrowsAsync(
                expected.GetType(),
                async () =>
                {
                    await _handler.Handle(
                        CreateRequest(), source.Token);
                });

            Assert.That(actual, Is.SameAs(expected));
            VerifyExecution(source.Token);
        }

        [TestCaseSource(typeof(PortalCampaignCases))]
        public void CampaignMonth_ParsesOrFallsBack(
            string name,
            DateTime expected)
        {
            Assert.That(
                InvokePrivate("GetCampaignMonth", null, name),
                Is.EqualTo(expected));
        }

        [Test]
        public void CampaignMonth_Null_ThrowsArgumentNullException()
        {
            var exception = Assert.Throws<TargetInvocationException>(
                () => InvokePrivate(
                    "GetCampaignMonth",
                    null,
                    new object?[] { null }));

            Assert.That(
                exception!.InnerException,
                Is.TypeOf<ArgumentNullException>());
        }

        [TestCaseSource(typeof(PortalImageCases))]
        public void Image_MissingOrWrongType_ReturnsNullAndWarns(
            IEnumerable<IContentItemFieldsSource>? images)
        {
            var result = InvokePrivate(
                "GetImageUrl",
                _controller,
                images,
                "Example",
                "toolkit");

            Assert.That(result, Is.Null);

            Assert.That(
                _logger.Invocations.Count(call =>
                    call.Method.Name == nameof(ILogger.Log) &&
                    Equals(call.Arguments[0], LogLevel.Warning)),
                Is.EqualTo(1));
        }

        [Test]
        [Category("ValidationGap")]
        public void Image_NullLinkedItem_ThrowsInsteadOfFallingBack()
        {
            var images = new IContentItemFieldsSource[] { null! };

            var exception = Assert.Throws<TargetInvocationException>(
                () => InvokePrivate(
                    "GetImageUrl",
                    _controller,
                    images,
                    "Example",
                    "toolkit"));

            Assert.That(
                exception!.InnerException,
                Is.TypeOf<NullReferenceException>());
        }

        [Test]
        public void Registration_HasExactIdentifierAndView()
        {
            var registration =
                typeof(AgentPortalPageTemplateController)
                    .Assembly.GetCustomAttributesData()
                    .SingleOrDefault(attribute =>
                        attribute.AttributeType.Name ==
                            "RegisterPageTemplateAttribute" &&
                        attribute.ConstructorArguments.Any(argument =>
                            Equals(argument.Value, Identifier)));

            Assert.That(registration, Is.Not.Null);

            var values = registration!.ConstructorArguments
                .Select(argument => argument.Value)
                .Concat(registration.NamedArguments.Select(
                    argument => argument.TypedValue.Value))
                .ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(values, Does.Contain(Identifier));
                Assert.That(values, Does.Contain(ViewPath));
                Assert.That(values, Does.Contain(
                    typeof(AgentPortalPageTemplateProperties)));
            });
        }

        private void SetResults(
            IEnumerable<AgentPortalMainPage> pages)
        {
            _executor.Setup(x =>
                x.GetResult<AgentPortalMainPage>(
                    It.IsAny<ContentItemQueryBuilder>(),
                    It.IsAny<Func<IContentQueryDataContainer,
                        AgentPortalMainPage>>(),
                    It.IsAny<ContentQueryExecutionOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(pages);
        }

        private void VerifyExecution(CancellationToken token)
        {
            // Verification must also target the interface method.
            _executor.Verify(x =>
                x.GetResult<AgentPortalMainPage>(
                    It.IsAny<ContentItemQueryBuilder>(),
                    It.IsAny<Func<IContentQueryDataContainer,
                        AgentPortalMainPage>>(),
                    It.Is<ContentQueryExecutionOptions>(
                        options => options.ForPreview),
                    It.Is<CancellationToken>(
                        actual => actual == token)),
                Times.Once);
        }

        private static AgentPortalPageQuery CreateRequest()
        {
            return new AgentPortalPageQuery(new RoutedWebPage
            {
                WebPageItemID = 123,
                WebPageItemGUID =
                    Guid.Parse("1d907759-779a-4cf0-91f0-ef500de6e364"),
                LanguageID = 1,
                LanguageName = "en",
                ContentTypeName = AgentPortalMainPage.CONTENT_TYPE_NAME,
                WebsiteChannelID = 1,
                WebsiteChannelName = "ETG"
            });
        }

        private static object? InvokePrivate(
            string name,
            object? target,
            params object?[] arguments)
        {
            var method = typeof(AgentPortalPageTemplateController)
                .GetMethod(
                    name,
                    BindingFlags.NonPublic |
                    (target == null
                        ? BindingFlags.Static
                        : BindingFlags.Instance));

            Assert.That(method, Is.Not.Null);

            return method!.Invoke(target, arguments);
        }
    }

    public abstract class PortalCaseSource : IEnumerable<TestCaseData>
    {
        public abstract IEnumerator<TestCaseData> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public sealed class PortalBooleanCases : PortalCaseSource
    {
        public override IEnumerator<TestCaseData> GetEnumerator()
        {
            foreach (var value in new[] { false, true })
                yield return new TestCaseData(value);
        }
    }

    public sealed class PortalResultCases : PortalCaseSource
    {
        public override IEnumerator<TestCaseData> GetEnumerator()
        {
            foreach (var count in new[] { 0, 1, 3 })
                yield return new TestCaseData(count);
        }
    }

    public sealed class PortalTextCases : PortalCaseSource
    {
        public override IEnumerator<TestCaseData> GetEnumerator()
        {
            foreach (var value in new string?[]
            {
                null,
                "",
                " ",
                "\t\r\n",
                "Agent portal",
                "日本語-é-مرحبا"
            })
            {
                yield return new TestCaseData(
                    new object?[] { value });
            }
        }
    }

    public sealed class PortalFailureCases : PortalCaseSource
    {
        public override IEnumerator<TestCaseData> GetEnumerator()
        {
            foreach (var exception in new Exception[]
            {
                new InvalidOperationException("Query failed"),
                new ArgumentException("Invalid query"),
                new OperationCanceledException()
            })
            {
                yield return new TestCaseData(exception);
            }
        }
    }

    public sealed class PortalCampaignCases : PortalCaseSource
    {
        public override IEnumerator<TestCaseData> GetEnumerator()
        {
            foreach (var month in new[] { 1, 6, 12 })
            {
                var date = new DateTime(2026, month, 1);
                var label = date.ToString(
                    "MMMM yyyy",
                    CultureInfo.InvariantCulture);

                yield return new TestCaseData(
                    $"Campaign ({label})", date);
            }

            foreach (var name in new[]
            {
                "",
                " ",
                "Campaign",
                "Campaign (Unknown 2026)",
                "Campaign (January 0000)",
                "Campaign (January 2026) suffix",
                "<script>alert(1)</script>"
            })
            {
                yield return new TestCaseData(
                    name, DateTime.MaxValue);
            }
        }
    }

    public sealed class PortalImageCases : PortalCaseSource
    {
        public override IEnumerator<TestCaseData> GetEnumerator()
        {
            yield return new TestCaseData(new object?[] { null })
                .SetName("Image_NullCollection");

            yield return new TestCaseData(new object[]
            {
                Array.Empty<IContentItemFieldsSource>()
            }).SetName("Image_EmptyCollection");

            yield return new TestCaseData(new object[]
            {
                new[]
                {
                    new Mock<IContentItemFieldsSource>().Object
                }
            }).SetName("Image_InvalidLinkedType");
        }
    }
}