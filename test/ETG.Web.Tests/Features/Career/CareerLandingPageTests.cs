using System.Reflection;
using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG;
using ETG.Web.Features.Career;
using ETG.Web.Features.Career.Operations;
using ETG.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

using CmsPage = ETG.Core.Content.Page;
using MediaFile = ETG.Core.Content.MediaFile;

namespace ETG.Web.Tests.Features.Career;

[TestFixture]
public class CareerLandingPageTests : BaseWebTest
{
    private const string Pass = "ExpectedToPass";
    private const string Gap = "ExpectedToExposeLogicOrValidationGap";
    private const string ViewPath =
        "~/Features/Careers/CareerLandingPage_Default.cshtml";

    private Mock<IMediator> mediator = null!;
    private Mock<IWebPageUrlRetriever> urls = null!;
    private Mock<IWebsiteChannelContext> channel = null!;
    private Mock<IWebPageDataContextRetriever> context = null!;

    private CareerLandingPageTemplateController controller = null!;
    private WebPageMetaService metadata = null!;
    private RoutedWebPage routedPage = null!;
    private WebPageDataContext pageContext = null!;
    private CmsPage cmsPage = null!;
    private IReadOnlyList<CareerRole>? items;
    private string rolePath = null!;
    private Random random = null!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();

        mediator = new(MockBehavior.Strict);
        urls = new(MockBehavior.Strict);
        channel = new(MockBehavior.Strict);
        context = new(MockBehavior.Strict);
        random = new Random(23);

        routedPage = new RoutedWebPage
        {
            WebPageItemID = random.Next(1, 1_000_000),
            LanguageName = "en",
            WebsiteChannelName = Text()
        };

        pageContext = CreatePageContext(routedPage);
        rolePath = $"/{Text()}/{Text()}";

        cmsPage = new CmsPage
        {
            SystemFields = new WebPageFields
            {
                WebPageItemID = routedPage.WebPageItemID
            },
            DocumentName = Text(),
            PageHeading = Text(),
            PageContent = Text(),
            PageJsonSchema = Text(),
            PageHeroImageAltTag = Text(),
            PageHeroCaption = Text(),
            PageHideShareButton = true,
            MetaDataSchema__PageTitle = Text(),
            MetaDataSchema__PageDescription = Text(),
            MetaDataSchema__PageKeywords = Text()
        };

        items = Array.Empty<CareerRole>();

        context.Setup(x => x.TryRetrieve(out pageContext))
            .Returns(true);

        channel.SetupGet(x => x.WebsiteChannelName)
            .Returns(routedPage.WebsiteChannelName);

        mediator.Setup(x => x.Send(
                It.IsAny<CareerLandingPageQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
                new CareerLandingPageQueryResponse(cmsPage, items!));

        urls.Setup(x => x.Retrieve(
                It.IsAny<IWebPageFieldsSource>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WebPageUrl(
                rolePath,
                $"https://example.com{rolePath}"));

        var http = new DefaultHttpContext();

        metadata = new WebPageMetaService(
            mediator.Object,
            new HttpContextAccessor { HttpContext = http });

        controller = new CareerLandingPageTemplateController(
            mediator.Object,
            urls.Object,
            channel.Object,
            context.Object,
            metadata)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = http
            }
        };
    }

    [TearDown]
    public override void TearDown()
    {
        try
        {
            controller?.Dispose();
        }
        finally
        {
            base.TearDown();
        }
    }

    [Test]
    [Category(Pass)]
    public async Task ValidPage_MapsGeneratedContentAndMetadata()
    {
        // Arrange: generated valid content from Setup.

        // Act
        var model = await Model();
        var meta = await metadata.GetMeta();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(model.Name, Is.EqualTo(cmsPage.DocumentName));
            Assert.That(model.Content, Is.EqualTo(cmsPage.PageContent));
            Assert.That(model.JsonSchema, Is.EqualTo(cmsPage.PageJsonSchema));
            Assert.That(model.HideShareButton, Is.True);
            Assert.That(model.Hero.Heading, Is.EqualTo(cmsPage.PageHeading));
            Assert.That(model.Hero.HeroImageAltText,
                Is.EqualTo(cmsPage.PageHeroImageAltTag));
            Assert.That(model.Hero.HeroCaption,
                Is.EqualTo(cmsPage.PageHeroCaption));
            Assert.That(model.CareerRoles, Is.Empty);
            Assert.That(meta.Title,
                Is.EqualTo(cmsPage.MetaDataSchema__PageTitle));
            Assert.That(meta.ShareImage, Is.Empty);
        });

        urls.VerifyNoOtherCalls();
    }

    public static IEnumerable<TestCaseData> TextCases()
    {
        string?[] values =
        {
            null, "", " \t", "Valid text",
            "<script>", "../../admin", "%ZZ", "工程師"
        };

        return values.Select(value =>
            new TestCaseData(value, value ?? ""));
    }

    [TestCaseSource(nameof(TextCases))]
    [Category(Pass)]
    public async Task CmsText_MapsNullEmptyWhitespaceAndUnexpectedValues(
        string? input,
        string expected)
    {
        // Arrange
        cmsPage.DocumentName = input!;
        cmsPage.PageContent = input!;
        cmsPage.PageJsonSchema = input!;
        cmsPage.PageHeroImageAltTag = input!;
        cmsPage.PageHeroCaption = input!;

        // Act
        var model = await Model();

        // Assert: null becomes empty; no trimming or sanitizing is assumed.
        Assert.That(new[]
        {
            model.Name,
            model.Content,
            model.JsonSchema,
            model.Hero.HeroImageAltText,
            model.Hero.HeroCaption
        }, Is.EqualTo(Enumerable.Repeat(expected, 5)));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" \t")]
    [Category(Pass)]
    public async Task BlankHeading_FallsBackToGeneratedDocumentName(
        string? heading)
    {
        cmsPage.PageHeading = heading!;

        var model = await Model();

        Assert.That(model.Hero.Heading,
            Is.EqualTo(cmsPage.DocumentName));
    }

    [Test]
    [Category(Pass)]
    public async Task NullHeadingAndDocument_UsesCareersFallback()
    {
        cmsPage.PageHeading = null!;
        cmsPage.DocumentName = null!;

        var model = await Model();

        Assert.That(model.Hero.Heading, Is.EqualTo("Careers"));
        Assert.That(model.Name, Is.Empty);
    }

    [TestCase("")]
    [TestCase(" \t")]
    [Category(Gap)]
    public async Task BlankDocument_BlocksCareersHeadingFallback(
        string document)
    {
        // Current fallback uses ?? rather than a whitespace check.
        cmsPage.PageHeading = null!;
        cmsPage.DocumentName = document;

        var model = await Model();

        Assert.That(model.Hero.Heading, Is.EqualTo(document));
    }

    [TestCase("null")]
    [TestCase("empty")]
    [TestCase("null-entry")]
    [TestCase("missing-asset")]
    [Category(Pass)]
    public async Task MissingHeroImage_UsesEmptyImageUrl(string scenario)
    {
        cmsPage.PageHeroImage = scenario switch
        {
            "null" => null!,
            "empty" => Array.Empty<IContentItemFieldsSource>(),
            "null-entry" => new IContentItemFieldsSource[] { null! },
            _ => new IContentItemFieldsSource[] { new MediaFile() }
        };

        var model = await Model();

        Assert.That(model.Hero.HeroImageUrl, Is.Empty);
        Assert.That((await metadata.GetMeta()).ShareImage, Is.Empty);
    }

    [TestCaseSource(nameof(TextCases))]
    [Category(Pass)]
    public async Task RoleText_MapsNullEmptyWhitespaceAndUnexpectedValues(
        string? input,
        string expected)
    {
        var role = NewRole();
        role.CareerRoleLocation = input!;
        role.CareerRolePositionType = input!;
        items = new[] { role };

        var model = await Model();
        var mapped = model.CareerRoles.Single();

        Assert.Multiple(() =>
        {
            Assert.That(mapped.Title, Is.EqualTo(role.CareerRoleTitle));
            Assert.That(mapped.Location, Is.EqualTo(expected));
            Assert.That(mapped.PositionType, Is.EqualTo(expected));
            Assert.That(mapped.Path, Is.EqualTo(rolePath));
        });

        VerifyUrl(role);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" \t")]
    [Category(Pass)]
    public async Task RoleTitle_UsesNullOnlyFallback(string? title)
    {
        var role = NewRole();
        role.CareerRoleTitle = title!;
        items = new[] { role };

        var model = await Model();

        // Unlike Detail, Landing does not fallback for blank titles.
        Assert.That(model.CareerRoles.Single().Title,
            Is.EqualTo(title ?? role.DocumentName));

        VerifyUrl(role);
    }

    [TestCase(false)]
    [TestCase(true)]
    [Category(Pass)]
    public async Task NullRoleTitleAndDocument_UsesNodeOrEmpty(bool nullNode)
    {
        var role = NewRole();
        role.CareerRoleTitle = role.DocumentName = null!;

        if (nullNode)
            role.SystemFields.WebPageItemName = null!;

        items = new[] { role };

        var model = await Model();

        Assert.That(model.CareerRoles.Single().Title,
            Is.EqualTo(nullNode ? "" : role.SystemFields.WebPageItemName));

        VerifyUrl(role);
    }

    [Test]
    [Category(Pass)]
    public async Task Roles_PreserveInputOrderDespiteReverseUrlCompletion()
    {
        // Arrange: controller must preserve the handler's returned order.
        var roles = new[] { NewRole(), NewRole(), NewRole() };
        items = roles;

        var completions = roles.ToDictionary(
            role => role,
            _ => new TaskCompletionSource<WebPageUrl>(
                TaskCreationOptions.RunContinuationsAsynchronously));

        urls.Setup(x => x.Retrieve(
                It.IsAny<IWebPageFieldsSource>(),
                It.IsAny<CancellationToken>()))
            .Returns((IWebPageFieldsSource source, CancellationToken _) =>
                completions[(CareerRole)source].Task);

        // Act
        var pending = controller.Index();

        foreach (var role in roles.Reverse())
        {
            completions[role].SetResult(new WebPageUrl(
                rolePath,
                $"https://example.com{rolePath}"));
        }

        var model = AssertView(await pending);

        // Assert: this tests controller order, not the database OrderBy.
        Assert.That(
            model.CareerRoles.Select(role => role.Title),
            Is.EqualTo(roles.Select(role => role.CareerRoleTitle)));

        VerifyQuery();

        foreach (var role in roles)
            VerifyUrl(role);
    }

    [Test]
    [Category(Pass)]
    public async Task EmptyRoleCollection_ReturnsViewWithoutUrlCalls()
    {
        items = Array.Empty<CareerRole>();

        var model = await Model();

        Assert.That(model.CareerRoles, Is.Empty);
        urls.VerifyNoOtherCalls();
    }

    [Test]
    [Category(Gap)]
    public void NullRoleCollection_DocumentsMissingGuard()
    {
        items = null;

        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await controller.Index());

        VerifyQuery();
        urls.VerifyNoOtherCalls();
    }

    [Test]
    [Category(Gap)]
    public void NullRoleEntry_DocumentsMissingGuard()
    {
        items = new CareerRole[] { null! };

        Assert.ThrowsAsync<NullReferenceException>(
            async () => await controller.Index());

        VerifyQuery();
    }

    [Test]
    [Category(Gap)]
    public void NullRetrievedUrl_DocumentsMissingGuard()
    {
        items = new[] { NewRole() };

        urls.Setup(x => x.Retrieve(
                It.IsAny<IWebPageFieldsSource>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((WebPageUrl)null!);

        Assert.ThrowsAsync<NullReferenceException>(
            async () => await controller.Index());

        VerifyQuery();
        VerifyUrl(items[0]);
    }

    [Test]
    [Category(Gap)]
    public void RoleFallbackWithNullSystemFields_DocumentsMissingGuard()
    {
        var role = NewRole();
        role.CareerRoleTitle = role.DocumentName = null!;
        role.SystemFields = null!;
        items = new[] { role };

        Assert.ThrowsAsync<NullReferenceException>(
            async () => await controller.Index());

        VerifyQuery();
        VerifyUrl(role);
    }

    [TestCase(false)]
    [TestCase(true)]
    [Category(Pass)]
    public async Task NullResponseOrPage_Returns404(bool nullResponse)
    {
        mediator.Setup(x => x.Send(
                It.IsAny<CareerLandingPageQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(nullResponse
                ? null!
                : new CareerLandingPageQueryResponse(null!, items!));

        Assert.That(await controller.Index(),
            Is.TypeOf<NotFoundResult>());

        VerifyQuery();
        urls.VerifyNoOtherCalls();
    }

    [Test]
    [Category(Pass)]
    public async Task MissingContext_Returns404()
    {
        WebPageDataContext absent = null!;
        context.Setup(x => x.TryRetrieve(out absent)).Returns(false);

        Assert.That(await controller.Index(),
            Is.TypeOf<NotFoundResult>());

        mediator.VerifyNoOtherCalls();
        channel.VerifyNoOtherCalls();
        urls.VerifyNoOtherCalls();
    }

    [Test]
    [Category(Gap)]
    public void RetrievedNullContext_DocumentsMissingGuard()
    {
        WebPageDataContext absent = null!;
        context.Setup(x => x.TryRetrieve(out absent)).Returns(true);

        Assert.ThrowsAsync<NullReferenceException>(
            async () => await controller.Index());

        mediator.VerifyNoOtherCalls();
        urls.VerifyNoOtherCalls();
    }

    [TestCase("context")]
    [TestCase("query")]
    [TestCase("url")]
    [Category(Pass)]
    public void DependencyFailure_PropagatesOriginal(string dependency)
    {
        var expected = new InvalidOperationException(
            $"Failure: {dependency}");

        if (dependency == "context")
        {
            context.Setup(x => x.TryRetrieve(out pageContext))
                .Throws(expected);
        }
        else if (dependency == "query")
        {
            mediator.Setup(x => x.Send(
                    It.IsAny<CareerLandingPageQuery>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(expected);
        }
        else
        {
            items = new[] { NewRole() };

            urls.Setup(x => x.Retrieve(
                    It.IsAny<IWebPageFieldsSource>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(expected);
        }

        var actual = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await controller.Index());

        Assert.That(actual, Is.SameAs(expected));

        if (dependency == "context")
        {
            mediator.VerifyNoOtherCalls();
            urls.VerifyNoOtherCalls();
        }
        else
        {
            VerifyQuery();
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    [Category(Pass)]
    public void QueryCancellation_Propagates(bool requestAborted)
    {
        using var cancellation = new CancellationTokenSource();

        if (requestAborted)
            cancellation.Cancel();

        controller.HttpContext.RequestAborted = cancellation.Token;

        var expected = new OperationCanceledException("Cancelled");

        mediator.Setup(x => x.Send(
                It.IsAny<CareerLandingPageQuery>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(expected);

        var actual = Assert.ThrowsAsync<OperationCanceledException>(
            async () => await controller.Index());

        Assert.That(actual, Is.SameAs(expected));
        VerifyQuery();
        urls.VerifyNoOtherCalls();
    }

    [Test]
    [Category(Gap)]
    public async Task RequestAborted_IsNotForwardedToMediator()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        controller.HttpContext.RequestAborted = cancellation.Token;

        await Model();

        // The supplied controller calls Send without a token.
        mediator.Verify(x => x.Send(
            It.IsAny<CareerLandingPageQuery>(),
            CancellationToken.None), Times.Once);
    }

    private string Text() => new(
        Enumerable.Range(0, 12)
            .Select(_ => (char)('a' + random.Next(26)))
            .ToArray());

    private CareerRole NewRole() => new()
    {
        SystemFields = new WebPageFields
        {
            WebPageItemName = Text()
        },
        CareerRoleTitle = Text(),
        DocumentName = Text(),
        CareerRoleLocation = Text(),
        CareerRolePositionType = Text()
    };

    private async Task<CareerLandingPageViewModel> Model()
    {
        var model = AssertView(await controller.Index());
        VerifyQuery();
        return model;
    }

    private static CareerLandingPageViewModel AssertView(ActionResult result)
    {
        Assert.That(result, Is.TypeOf<ViewResult>());

        var view = (ViewResult)result;

        Assert.That(view.ViewName, Is.EqualTo(ViewPath));
        Assert.That(view.Model,
            Is.TypeOf<CareerLandingPageViewModel>());

        var model = (CareerLandingPageViewModel)view.Model!;
        Assert.That(model.Hero, Is.Not.Null);
        return model;
    }

    private void VerifyQuery()
    {
        context.Verify(x => x.TryRetrieve(out pageContext), Times.Once);

        channel.VerifyGet(
            x => x.WebsiteChannelName, Times.Once);

        mediator.Verify(x => x.Send(
            It.Is<CareerLandingPageQuery>(query =>
                ReferenceEquals(query.Page, routedPage) &&
                query.ChannelName == routedPage.WebsiteChannelName),
            It.IsAny<CancellationToken>()), Times.Once);

        mediator.VerifyNoOtherCalls();
    }

    private void VerifyUrl(CareerRole role)
    {
        urls.Verify(x => x.Retrieve(
            It.Is<IWebPageFieldsSource>(source =>
                ReferenceEquals(source, role)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // Fixture construction only; no private controller methods are tested.
    private static WebPageDataContext CreatePageContext(RoutedWebPage page)
    {
        var constructor = typeof(WebPageDataContext).GetConstructor(
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(RoutedWebPage) },
            modifiers: null)
            ?? throw new InvalidOperationException(
                "The installed Kentico version needs a different context " +
                "fixture: WebPageDataContext(RoutedWebPage) was not found.");

        return (WebPageDataContext)constructor.Invoke(
            new object[] { page });
    }
}