using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core;
using ETG.Core.Operations;
using ETG.Web.Features.Article;
using ETG.Web.Services.Tour;
using ETG.Web.Tests;
using Kentico.Content.Web.Mvc;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Article = ETG.Core.Content.Article;
using ContentPage = ETG.Core.Content.Page;
using Destination = ETG.Core.Content.Destination;
using MediaFile = ETG.Core.Content.MediaFile;
using Tour = ETG.Core.Content.Tour;

namespace ETG.Web.Tests.Features.ArticlePage
{
    [TestFixture]
    public class ArticlePostPageTests : BaseWebTest
    {
        private static readonly Guid RelatedId =
            Guid.Parse("10000000-0000-0000-0000-000000000001");

        private Mock<IContentQueryExecutor> _executor = null!;
        private Mock<IMediator> _mediator = null!;
        private ArticlePageQueryHandler _page = null!;
        private ArticlePostPagesLatestQueryHandler _latest = null!;
        private ArticlePostPageToursQueryHandler _tours = null!;

        [SetUp]
        public override void Setup()
        {
            base.Setup();

            _executor = new Mock<IContentQueryExecutor>();
            _executor.As<IContentQueryModelTypeMapper>();

            _mediator = new Mock<IMediator>(MockBehavior.Strict);

            var optionsCreator =
                new Mock<IContentQueryExecutionOptionsCreator>();

            optionsCreator
                .Setup(x => x.Create())
                .Returns(() => new ContentQueryExecutionOptions());

            var tools = new WebPageQueryTools(
                _executor.Object,
                null!,
                optionsCreator.Object,
                null!);

            _page = new ArticlePageQueryHandler(tools);

            _latest = new ArticlePostPagesLatestQueryHandler(
                tools,
                null!);

            _tours = new ArticlePostPageToursQueryHandler(tools);
        }

        // Missing context: 404 without downstream calls.
        [Test]
        public async Task Controller_MissingContext_Returns404()
        {
            var controller = Controller(
                new Mock<IWebPageDataContextRetriever>().Object,
                new Mock<ITourPricingService>().Object);

            Assert.That(
                await controller.Index(), Is.TypeOf<NotFoundResult>());

            _mediator.VerifyNoOtherCalls();
            _executor.VerifyNoOtherCalls();
        }

        // Empty results currently throw; otherwise return the first item.
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(3)]
        public async Task Page_ReturnsFirstOrThrows(int count)
        {
            var items = Enumerable.Range(1, count)
                .Select(i => ArticleItem(i)).ToArray();

            Results(items);

            if (count == 0)
                Assert.ThrowsAsync<InvalidOperationException>(
                    async () => { await _page.Handle(PageRequest()); });
            else
                Assert.That(
                    await _page.Handle(PageRequest()),
                    Is.SameAs(items[0]));

            Verify<Article>(CancellationToken.None);
        }

        // Checks matching, duplicates, current-page exclusion,
        // three-item limit, and preservation of executor order.
        [TestCase("category", false)]
        [TestCase("category", true)]
        [TestCase("destination", false)]
        [TestCase("destination", true)]
        [TestCase("unrelated", false)]
        public async Task Latest_ReturnsOnlyThreeRelatedArticles(
            string relation, bool duplicates)
        {
            var current = ArticleItem(1, "both", duplicates);
            var items = new[]
            {
                current,
                ArticleItem(2),
                ArticleItem(9, relation, duplicates),
                ArticleItem(4, relation, duplicates),
                ArticleItem(7, relation, duplicates),
                ArticleItem(3, relation, duplicates)
            };

            Results(items);

            var result = await _latest.Handle(
                new ArticlePostPagesLatestQuery(current, "ETG"));

            Assert.That(result.Items, Is.EqualTo(
                relation == "unrelated"
                    ? Array.Empty<Article>()
                    : new[] { items[2], items[3], items[4] }));

            Verify<Article>(CancellationToken.None);
        }

        [Test]
        public async Task Latest_NoResults_ReturnsEmpty()
        {
            Results(Array.Empty<Article>());

            var result = await _latest.Handle(
                new ArticlePostPagesLatestQuery(ArticleItem(1), "ETG"));

            Assert.That(result.Items, Is.Empty);
            Verify<Article>(CancellationToken.None);
        }

        // Null/empty destinations return before checking cancellation.
        [TestCase(true)]
        [TestCase(false)]
        public async Task Tours_NoDestinations_SkipsQuery(bool useNull)
        {
            using var source = new CancellationTokenSource();
            source.Cancel();

            var result = await _tours.Handle(
                new ArticlePostPageToursQuery(
                    useNull ? null! : new List<string>()),
                source.Token);

            Assert.That(result.Items, Is.Empty);
            _executor.VerifyNoOtherCalls();
        }

        // Both unmatched destinations and missing featured tours
        // should stop before executing a tour query.
        [TestCase(false)]
        [TestCase(true)]
        public async Task Tours_NoLinkedTours_ReturnsEmpty(bool found)
        {
            Results(found
                ? new[] { new Destination() }
                : Array.Empty<Destination>());

            var result = await _tours.Handle(ToursRequest());

            Assert.That(result.Items, Is.Empty);
            Verify<Destination>(CancellationToken.None);
            Assert.That(_executor.Invocations.Count, Is.EqualTo(1));
        }

        [TestCase("page", false)]
        [TestCase("page", true)]
        [TestCase("latest", false)]
        [TestCase("latest", true)]
        [TestCase("tours", false)]
        [TestCase("tours", true)]
        public void Handlers_ForwardTokenAndPropagateErrors(
            string handler, bool cancelled)
        {
            using var source = new CancellationTokenSource();
            if (cancelled) source.Cancel();

            Exception expected = cancelled
                ? new OperationCanceledException(source.Token)
                : new InvalidOperationException("Query failed");

            if (handler == "tours")
                Failure<Destination>(expected);
            else
                Failure<Article>(expected);

            var actual = Assert.ThrowsAsync(
                expected.GetType(), async () =>
                {
                    if (handler == "page")
                        await _page.Handle(PageRequest(), source.Token);
                    else if (handler == "latest")
                        await _latest.Handle(
                            new ArticlePostPagesLatestQuery(
                                ArticleItem(1), "ETG"), source.Token);
                    else
                        await _tours.Handle(ToursRequest(), source.Token);
                });

            Assert.That(actual, Is.SameAs(expected));

            if (handler == "tours")
                Verify<Destination>(source.Token);
            else
                Verify<Article>(source.Token);
        }

        // COMPONENT TESTS: Index performs static Kentico database access.
        // Prices are mocked; this tests controller formatting/fallbacks,
        // not the pricing service's database calculations.
        [TestCase(null, false, 0, 0, "", "")]
        [TestCase("", false, 0, 0, "", "")]
        [TestCase(" ", false, 0, 0, "", "")]
        [TestCase("T1", false, 0, 0, "", "")]
        [TestCase("T1", true, 0, 0, "", "")]
        [TestCase("T1", true, -10, -20, "", "")]
        [TestCase("T1", true, 1234, 2000, "$1,234", "2,000")]
        [TestCase("T1", true, 2500, 2000, "$2,500", "2,000")]
        [Category("KenticoComponent")]
        public async Task Controller_MapsPricingAndFallbacks(
            string? code, bool hasPrice, int current, int original,
            string expectedCurrent, string expectedOriginal)
        {
            var tour = new Tour { TourCode = code!, TourName = "Tour" };
            var prices = new Dictionary<string, TourPricingResult>();

            if (hasPrice && !string.IsNullOrWhiteSpace(code))
                prices[code] = new TourPricingResult(current, original);

            var fixture = ArrangeController(tour, prices);
            var result = (ViewResult)await fixture.Controller.Index();
            var model = (ArticlePostPageViewModel)result.Model!;
            var mapped = model.TourListing!.Tours.Single();

            Assert.Multiple(() =>
            {
                Assert.That(result.ViewName, Is.EqualTo(
                    "~/Features/Article/ArticlePostPage_Default.cshtml"));

                Assert.That(model.Title, Is.Empty);
                Assert.That(model.Summary, Is.Empty);
                Assert.That(model.AuthorName, Is.Empty);
                Assert.That(model.PublishDateIso, Is.Empty);
                Assert.That(model.RelatedArticles, Is.Empty);

                Assert.That(mapped.CurrentPrice, Is.EqualTo(expectedCurrent));
                Assert.That(mapped.OriginalPrice, Is.EqualTo(expectedOriginal));
                Assert.That(mapped.Currency, Is.EqualTo("AUD"));
                Assert.That(mapped.Path, Is.EqualTo("#"));
                Assert.That(mapped.HeroImageAlt, Is.EqualTo("Tour"));
                Assert.That(mapped.Description, Is.Empty);
                Assert.That(mapped.Badge, Is.Empty);
                Assert.That(mapped.DaysFromLabel, Is.Empty);
                Assert.That(mapped.PriceTypeLabel,
                    Is.EqualTo("Per person | twin share"));
            });

            fixture.Pricing.Verify(p => p.GetPricesBulkAsync(
                It.Is<IEnumerable<Tour>>(
                    items => items.SequenceEqual(new[] { tour }))),
                Times.Once);

            fixture.Pricing.VerifyNoOtherCalls();
        }

        [TestCase("null")]
        [TestCase("empty")]
        [TestCase("wrong-type")]
        [TestCase("missing-asset")]
        [Category("KenticoComponent")]
        public async Task Controller_MissingImages_ReturnsEmpty(string kind)
        {
            IEnumerable<IContentItemFieldsSource>? images = kind switch
            {
                "null" => null,
                "empty" => Array.Empty<IContentItemFieldsSource>(),
                "wrong-type" =>
                    new[] { new Mock<IContentItemFieldsSource>().Object },
                _ => new IContentItemFieldsSource[] { new MediaFile() }
            };

            var fixture = ArrangeController(
                new Tour { TourHeroImage = images! },
                new Dictionary<string, TourPricingResult>());

            fixture.Article.ArticleHeroImage = images!;

            var result = (ViewResult)await fixture.Controller.Index();
            var model = (ArticlePostPageViewModel)result.Model!;

            Assert.Multiple(() =>
            {
                Assert.That(model.HeroImageUrl, Is.Empty);
                Assert.That(model.Hero!.HeroImageUrl, Is.Empty);
                Assert.That(
                    model.TourListing!.Tours.Single().HeroImageUrl,
                    Is.Empty);
            });
        }

        // Documents unguarded null pricing responses.
        [TestCase(true)]
        [TestCase(false)]
        [Category("KenticoComponent")]
        [Category("ValidationGap")]
        public void Controller_NullPrices_Throws(bool nullDictionary)
        {
            var prices = nullDictionary
                ? null
                : new Dictionary<string, TourPricingResult>
                {
                    ["T1"] = null!
                };

            var fixture = ArrangeController(
                new Tour { TourCode = "T1" }, prices!);

            Assert.ThrowsAsync<NullReferenceException>(
                async () => { await fixture.Controller.Index(); });
        }

        [Test]
        public void Registration_HasCorrectIdentifierViewAndRoute()
        {
            var attributes = typeof(ArticlePostPageTemplateController)
                .Assembly.GetCustomAttributesData();

            var templates = attributes.Where(a =>
                a.AttributeType.Name == "RegisterPageTemplateAttribute" &&
                Values(a).Contains("ETG.ArticlePostPage_Default")).ToArray();

            Assert.That(templates, Has.Length.EqualTo(1));
            Assert.That(Values(templates[0]), Does.Contain(
                "~/Features/Article/ArticlePostPage_Default.cshtml"));
            Assert.That(Values(templates[0]), Does.Contain(
                typeof(ArticlePostPageTemplateProperties)));

            var routes = attributes.Where(a =>
                a.AttributeType.Name == "RegisterWebPageRouteAttribute" &&
                Values(a).Contains(
                    typeof(ArticlePostPageTemplateController))).ToArray();

            Assert.That(routes, Has.Length.EqualTo(1));
            Assert.That(
                Values(routes[0]), Does.Contain(Article.CONTENT_TYPE_NAME));
        }

        // ---------------- Shared helpers ----------------

        private (
            ArticlePostPageTemplateController Controller,
            Article Article,
            Mock<ITourPricingService> Pricing)
            ArrangeController(
                Tour tour, Dictionary<string, TourPricingResult> prices)
        {
            _executor.DefaultValueProvider = new EmptySequenceTasks();
            Results(Array.Empty<ContentPage>());

            var article = ArticleItem(123);

            _mediator.Setup(m => m.Send(
                    It.IsAny<ArticlePostPageQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(article);

            _mediator.Setup(m => m.Send(
                    It.IsAny<ArticlePostPagesLatestQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ArticlePostPagesLatestResult(
                    Array.Empty<Article>()));

            _mediator.Setup(m => m.Send(
                    It.IsAny<ArticlePostPageToursQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ArticlePostPageToursResult(new[] { tour }));

            var pricing =
                new Mock<ITourPricingService>(MockBehavior.Strict);

            pricing.Setup(p => p.GetPricesBulkAsync(
                    It.IsAny<IEnumerable<Tour>>()))
                .ReturnsAsync(prices);

            var contexts = new Mock<IWebPageDataContextRetriever>();
            var context = TestContext(RoutedPage());

            contexts.Setup(c => c.TryRetrieve(out context)).Returns(true);

            return (Controller(contexts.Object, pricing.Object),
                article, pricing);
        }

        private ArticlePostPageTemplateController Controller(
            IWebPageDataContextRetriever contexts, ITourPricingService pricing)
        {
            var channel = new Mock<IWebsiteChannelContext>();
            channel.SetupGet(c => c.WebsiteChannelName).Returns("ETG");

            // Supplied Article declaration does not implement IMetaDataSchema.
            return new ArticlePostPageTemplateController(
                _mediator.Object, contexts, null!, null!,
                _executor.Object, channel.Object, pricing, null!)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        // SDK-specific: prefer your project's context factory if available.
        // Requires RuntimeHelpers.GetUninitializedObject support.
        private static WebPageDataContext TestContext(RoutedWebPage page)
        {
            var fields = typeof(WebPageDataContext)
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(f => f.FieldType == typeof(RoutedWebPage)).ToArray();

            Assert.That(fields, Has.Length.EqualTo(1),
                "SDK layout changed; use your project's context factory.");

            var context = (WebPageDataContext)
                RuntimeHelpers.GetUninitializedObject(
                    typeof(WebPageDataContext));

            fields[0].SetValue(context, page);
            Assert.That(context.WebPage, Is.SameAs(page));
            return context;
        }

        private static Article ArticleItem(
            int id, string relation = "none", bool duplicate = false)
        {
            var copies = duplicate ? 2 : 1;

            return new Article
            {
                SystemFields = new WebPageFields { WebPageItemID = id },
                ArticleCategories =
                    relation == "category" || relation == "both"
                        ? Enumerable.Repeat(RelatedId, copies).ToArray()
                        : Array.Empty<Guid>(),
                ArticleDestinations =
                    relation == "destination" || relation == "both"
                        ? Enumerable.Range(0, copies)
                            .Select(_ => new WebPageRelatedItem
                            {
                                WebPageGuid = RelatedId
                            }).ToArray()
                        : Array.Empty<WebPageRelatedItem>()
            };
        }

        private static RoutedWebPage RoutedPage() => new RoutedWebPage
        {
            WebPageItemID = 123,
            WebPageItemGUID = RelatedId,
            LanguageID = 1,
            LanguageName = "en",
            ContentTypeName = Article.CONTENT_TYPE_NAME,
            WebsiteChannelID = 1,
            WebsiteChannelName = "ETG"
        };

        private static ArticlePostPageQuery PageRequest() =>
            new ArticlePostPageQuery(RoutedPage());

        private static ArticlePostPageToursQuery ToursRequest() =>
            new ArticlePostPageToursQuery(
                new List<string> { RelatedId.ToString() });

        private void Results<T>(IEnumerable<T> items) =>
            _executor.Setup(x => x.GetResult<T>(
                    It.IsAny<ContentItemQueryBuilder>(),
                    It.IsAny<Func<IContentQueryDataContainer, T>>(),
                    It.IsAny<ContentQueryExecutionOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(items);

        private void Failure<T>(Exception error) =>
            _executor.Setup(x => x.GetResult<T>(
                    It.IsAny<ContentItemQueryBuilder>(),
                    It.IsAny<Func<IContentQueryDataContainer, T>>(),
                    It.IsAny<ContentQueryExecutionOptions>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(error);

        private void Verify<T>(CancellationToken token) =>
            _executor.Verify(x => x.GetResult<T>(
                    It.IsAny<ContentItemQueryBuilder>(),
                    It.IsAny<Func<IContentQueryDataContainer, T>>(),
                    It.IsAny<ContentQueryExecutionOptions>(),
                    It.Is<CancellationToken>(t => t == token)),
                Times.Once);

        private static object?[] Values(CustomAttributeData attribute) =>
            attribute.ConstructorArguments.Select(a => a.Value)
                .Concat(attribute.NamedArguments
                    .Select(a => a.TypedValue.Value)).ToArray();

        // Supplies empty raw content results for the controller's author query.
        private sealed class EmptySequenceTasks : DefaultValueProvider
        {
            protected override object GetDefaultValue(Type type, Mock mock)
            {
                if (type.IsGenericType &&
                    type.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    var result = type.GetGenericArguments()[0];

                    if (result.IsGenericType &&
                        result.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    {
                        return typeof(Task).GetMethod(nameof(Task.FromResult))!
                            .MakeGenericMethod(result)
                            .Invoke(null, new object[]
                            {
                                Array.CreateInstance(
                                    result.GetGenericArguments()[0], 0)
                            })!;
                    }
                }

                return type.IsValueType
                    ? Activator.CreateInstance(type)!
                    : null!;
            }
        }
    }
}