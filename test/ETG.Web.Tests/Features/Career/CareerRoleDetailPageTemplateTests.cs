using System.Reflection;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG;
using ETG.Core.Content;
using ETG.Web.Features.Career;
using ETG.Web.Features.Career.Operations;
using ETG.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using Moq;
using NUnit.Framework;

namespace ETG.Web.Tests.Features.Career;

[TestFixture(17)]
[TestFixture(42)]
public class CareerRoleDetailPageTemplateControllerTests(int seed) : BaseWebTest
{
    private const string Pass = "ExpectedToPass";
    private const string Gap = "ExpectedToExposeLogicOrValidationGap";

    private Mock<IMediator> mediator = null!;
    private Mock<IWebPageUrlRetriever> urls = null!;
    private Mock<IWebsiteChannelContext> channel = null!;
    private Mock<IWebPageDataContextRetriever> context = null!;

    private CareerRoleDetailPageTemplateController controller = null!;
    private CareerRole role = null!;
    private RoutedWebPage page = null!;
    private WebPageDataContext pageContext = null!;
    private WebPageMetaService metadata = null!;

    private string path = null!;
    private string alias = null!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();

        mediator = new(MockBehavior.Strict);
        urls = new(MockBehavior.Strict);
        channel = new(MockBehavior.Strict);
        context = new(MockBehavior.Strict);

        var random = new Random(seed);

        string Text(int length = 12) => new(
            Enumerable.Range(0, length)
                .Select(_ => (char)('a' + random.Next(26)))
                .ToArray());

        alias = Text();
        path = $"/{Text()}/{alias}";

        page = new RoutedWebPage
        {
            WebPageItemID = random.Next(1, 1_000_000),
            LanguageName = Text(2),
            WebsiteChannelName = Text()
        };

        pageContext = CreatePageContext(page);

        role = new CareerRole
        {
            SystemFields = new WebPageFields
            {
                WebPageItemID = page.WebPageItemID,
                WebPageItemName = alias,
                WebPageItemTreePath = path
            },
            CareerRoleTitle = Text(),
            DocumentName = Text(),
            CareerRoleLocation = Text(),
            CareerRolePositionType = Text(),
            CareerRoleDescription = Text(),
            CareerRoleSummaryOfPosition = Text(),
            CareerRoleDutiesAndResponsibilities = Text(),
            CareerRoleAddress = Text(),
            CareerRoleState = Text()
        };

        context.Setup(x => x.TryRetrieve(out pageContext))
            .Returns(true);

        channel.SetupGet(x => x.WebsiteChannelName)
            .Returns(page.WebsiteChannelName);

        mediator.Setup(x => x.Send(
                It.IsAny<CareerRoleDetailPageQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
                new CareerRoleDetailPageQueryResponse(role));

        urls.Setup(x => x.Retrieve(
                It.IsAny<IWebPageFieldsSource>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WebPageUrl(
                path,
                $"https://example.com{path}"));

        var http = new DefaultHttpContext();

        metadata = new WebPageMetaService(
            mediator.Object,
            new HttpContextAccessor { HttpContext = http });

        controller = new CareerRoleDetailPageTemplateController(
            mediator.Object,
            urls.Object,
            channel.Object,
            context.Object,
            metadata)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = http,
                RouteData = new RouteData()
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
    public async Task ValidRole_MapsGeneratedContent()
    {
        // Arrange: Setup creates reproducible generated CMS data.

        // Act
        var model = await Model();

        // Assert
        AssertTitle(model, role.CareerRoleTitle);

        Assert.That(TextValues(model), Is.EqualTo(new[]
        {
            role.CareerRoleLocation,
            role.CareerRolePositionType,
            role.CareerRoleDescription,
            role.CareerRoleSummaryOfPosition,
            role.CareerRoleDutiesAndResponsibilities,
            role.CareerRoleAddress,
            role.CareerRoleState
        }));

        Assert.That(model.PageAlias, Is.EqualTo(alias));
        Assert.That(model.ApplyUrl,
            Is.EqualTo($"/careers/apply?role={role.CareerRoleTitle}"));
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
        role.CareerRoleLocation =
            role.CareerRolePositionType =
            role.CareerRoleDescription =
            role.CareerRoleSummaryOfPosition =
            role.CareerRoleDutiesAndResponsibilities =
            role.CareerRoleAddress =
            role.CareerRoleState = input!;

        // Act
        var model = await Model();

        // Assert: null becomes empty; whitespace and other text are retained.
        Assert.That(
            TextValues(model),
            Is.EqualTo(Enumerable.Repeat(expected, 7)));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" \t")]
    [Category(Pass)]
    public async Task BlankTitle_UsesGeneratedDocumentName(string? title)
    {
        role.CareerRoleTitle = title!;

        var model = await Model();

        AssertTitle(model, role.DocumentName);
    }

    [TestCase(false)]
    [TestCase(true)]
    [Category(Pass)]
    public async Task MissingTitleAndDocument_UsesNodeOrEmpty(bool nullNode)
    {
        role.CareerRoleTitle = role.DocumentName = null!;

        if (nullNode)
            role.SystemFields.WebPageItemName = null!;

        var model = await Model();

        AssertTitle(model, nullNode ? "" : alias);
        Assert.That(model.ApplyUrl, Is.EqualTo(nullNode
            ? "/careers/apply"
            : $"/careers/apply?role={alias}"));
    }

    [TestCase("")]
    [TestCase(" \t")]
    [Category(Gap)]
    public async Task BlankDocument_BlocksNodeFallback(string documentName)
    {
        // Passing documents current behavior, not a validation fix.
        role.CareerRoleTitle = null!;
        role.DocumentName = documentName;

        var model = await Model();

        AssertTitle(model, documentName);
        Assert.That(model.ApplyUrl, Is.EqualTo("/careers/apply"));
    }

    [TestCase("Sales & Marketing", "Sales%20%26%20Marketing")]
    [TestCase("C# / .NET?", "C%23%20%2F%20.NET%3F")]
    [TestCase("<script>", "%3Cscript%3E")]
    [TestCase("A+B", "A%2BB")]
    [TestCase("100%", "100%25")]
    [TestCase(" Role ", "%20Role%20")]
    [TestCase("role=Admin&approved=true",
        "role%3DAdmin%26approved%3Dtrue")]
    [Category(Pass)]
    public async Task ApplyUrl_EncodesTitle(string title, string encoded)
    {
        role.CareerRoleTitle = title;

        var model = await Model();

        AssertTitle(model, title);
        Assert.That(model.ApplyUrl,
            Is.EqualTo($"/careers/apply?role={encoded}"));
    }

    [TestCase("", null)]
    [TestCase("/", null)]
    [TestCase("///", null)]
    [TestCase("/careers/developer", "developer")]
    [TestCase("//careers///developer//", "developer")]
    [TestCase("developer", "developer")]
    [TestCase("/careers/ ", " ")]
    [TestCase("/careers/%ZZ", "%ZZ")]
    [TestCase("/careers/<script>", "<script>")]
    [Category(Pass)]
    public async Task Alias_UsesLastSegmentOrGeneratedNode(
        string treePath,
        string? expected)
    {
        role.SystemFields.WebPageItemTreePath = treePath;

        var model = await Model();

        Assert.That(model.PageAlias, Is.EqualTo(expected ?? alias));
    }

    [Test]
    [Category(Pass)]
    public async Task MissingContext_Returns404()
    {
        WebPageDataContext missing = null!;
        context.Setup(x => x.TryRetrieve(out missing)).Returns(false);

        var result = await controller.Index();

        Assert.That(result, Is.TypeOf<NotFoundResult>());
        context.Verify(x => x.TryRetrieve(out missing), Times.Once);
        NoDownstreamCalls();
    }

    [TestCase(false)]
    [TestCase(true)]
    [Category(Pass)]
    public async Task NullResponseOrRole_Returns404(bool nullResponse)
    {
        mediator.Setup(x => x.Send(
                It.IsAny<CareerRoleDetailPageQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(nullResponse
                ? null!
                : new CareerRoleDetailPageQueryResponse(null!));

        var result = await controller.Index();

        Assert.That(result, Is.TypeOf<NotFoundResult>());
        VerifyQuery();
        urls.VerifyNoOtherCalls();
    }

    [TestCase("context")]
    [TestCase("system-fields")]
    [TestCase("tree-path")]
    [TestCase("url")]
    [Category(Gap)]
    public void UnexpectedNull_DocumentsMissingGuard(string missing)
    {
        if (missing == "context")
        {
            WebPageDataContext absent = null!;
            context.Setup(x => x.TryRetrieve(out absent)).Returns(true);
        }
        else if (missing == "system-fields")
        {
            role.SystemFields = null!;
        }
        else if (missing == "tree-path")
        {
            role.SystemFields.WebPageItemTreePath = null!;
        }
        else
        {
            urls.Setup(x => x.Retrieve(
                    It.IsAny<IWebPageFieldsSource>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((WebPageUrl)null!);
        }

        // These exceptions expose missing production guards.
        Assert.ThrowsAsync<NullReferenceException>(
            async () => await controller.Index());

        if (missing == "context")
            NoDownstreamCalls();
        else
            VerifySuccess();
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
                    It.IsAny<CareerRoleDetailPageQuery>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(expected);
        }
        else
        {
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
            NoDownstreamCalls();
        }
        else if (dependency == "query")
        {
            VerifyQuery();
            urls.VerifyNoOtherCalls();
        }
        else
        {
            VerifySuccess();
        }
    }

    public static IEnumerable<TestCaseData> ClientCases()
    {
        string?[] values =
        {
            null, "", " \t", "Unexpected role",
            "<script>", "../../admin", "%ZZ",
            "role=Admin&approved=true"
        };

        foreach (var source in new[] { "query", "form", "route" })
            foreach (var value in values)
                yield return new TestCaseData(source, value);
    }

    [TestCaseSource(nameof(ClientCases))]
    [Category(Pass)]
    public async Task ClientInput_DoesNotOverrideCmsRole(
        string source,
        string? value)
    {
        if (source == "query")
        {
            controller.Request.QueryString =
                QueryString.Create("role", value ?? "");
        }
        else if (source == "route")
        {
            controller.RouteData.Values["role"] = value;
        }
        else
        {
            controller.Request.Method = HttpMethods.Post;
            controller.Request.ContentType =
                "application/x-www-form-urlencoded";

            controller.Request.Form = new FormCollection(
                new Dictionary<string, StringValues>
                {
                    ["role"] = value ?? ""
                });
        }

        var model = await Model();

        AssertTitle(model, role.CareerRoleTitle);
        Assert.That(model.ApplyUrl,
            Is.EqualTo($"/careers/apply?role={role.CareerRoleTitle}"));
    }

    [Test]
    [Category(Pass)]
    public async Task NonMetadataRole_LeavesMetadataUnchanged()
    {
        Assert.That((object)role, Is.Not.InstanceOf<IMetaDataSchema>());
        var before = await metadata.GetMeta();

        await Model();

        Assert.That(await metadata.GetMeta(), Is.EqualTo(before));
    }

    private async Task<CareerRoleDetailPageViewModel> Model()
    {
        var result = await controller.Index();

        Assert.That(result, Is.TypeOf<ViewResult>());
        var view = (ViewResult)result;

        Assert.That(view.ViewName, Is.EqualTo(
            "~/Features/Careers/CareerRoleDetailPage_Default.cshtml"));

        Assert.That(view.Model,
            Is.TypeOf<CareerRoleDetailPageViewModel>());

        var model = (CareerRoleDetailPageViewModel)view.Model!;

        Assert.That(model.BasicInfo, Is.Not.Null);
        Assert.That(model.BasicInfo.Path, Is.EqualTo(path));

        VerifySuccess();
        return model;
    }

    private static void AssertTitle(
        CareerRoleDetailPageViewModel model,
        string expected)
    {
        Assert.That(model.Title, Is.EqualTo(expected));
        Assert.That(model.BasicInfo.Title, Is.EqualTo(expected));
    }

    private static string[] TextValues(
        CareerRoleDetailPageViewModel model) =>
        new[]
        {
            model.BasicInfo.Location,
            model.BasicInfo.PositionType,
            model.Description,
            model.SummaryOfPosition,
            model.DutiesAndResponsibilities,
            model.Address,
            model.State
        };

    private void NoDownstreamCalls()
    {
        mediator.VerifyNoOtherCalls();
        urls.VerifyNoOtherCalls();
        channel.VerifyNoOtherCalls();
    }

    private void VerifyQuery()
    {
        context.Verify(x => x.TryRetrieve(out pageContext), Times.Once);
        channel.VerifyGet(x => x.WebsiteChannelName, Times.Once);

        mediator.Verify(x => x.Send(
            It.Is<CareerRoleDetailPageQuery>(query =>
                ReferenceEquals(query.Page, page) &&
                query.ChannelName == page.WebsiteChannelName),
            It.IsAny<CancellationToken>()), Times.Once);

        mediator.VerifyNoOtherCalls();
    }

    private void VerifySuccess()
    {
        VerifyQuery();

        urls.Verify(x => x.Retrieve(
            It.Is<IWebPageFieldsSource>(source =>
                ReferenceEquals(source, role)),
            It.IsAny<CancellationToken>()), Times.Once);

        urls.VerifyNoOtherCalls();
    }

    // SDK fixture construction only.
    // No private controller methods are tested.
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
                "This Kentico version requires a different page-context " +
                "fixture: WebPageDataContext(RoutedWebPage) was not found.");

        return (WebPageDataContext)constructor.Invoke(
            new object[] { page });
    }
}