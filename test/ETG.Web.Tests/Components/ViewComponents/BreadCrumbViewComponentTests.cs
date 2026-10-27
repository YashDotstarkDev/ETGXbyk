using System.Collections.Generic;
using System.Linq;
using ETG.Web.Components.ViewComponents.BreadCrumb;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using NUnit.Framework;

namespace ETG.Web.Tests.Components.ViewComponents;

[TestFixture]
public class BreadCrumbViewComponentTests : BaseWebTest
{
    private BreadCrumbViewComponent _component = null!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();

        _component = new BreadCrumbViewComponent
        {
            ViewComponentContext = new ViewComponentContext
            {
                ViewContext = new ViewContext
                {
                    HttpContext = new DefaultHttpContext(),
                    ViewData = new ViewDataDictionary<object>(
                        new EmptyModelMetadataProvider(),
                        new ModelStateDictionary())
                }
            }
        };
    }

    public static IEnumerable<TestCaseData> PathCases()
    {
        foreach (var path in new string?[] { null, "", "/" })
        {
            yield return new TestCaseData(
                path,
                new string[0],
                new string[0]);
        }

        yield return new TestCaseData(
            "//about-us///OUR_team/",
            new[] { "About Us", "Our Team" },
            new[] { "/about-us", "" });

        yield return new TestCaseData(
            "/hello%20world",
            new[] { "Hello%20world" },
            new[] { "" });
    }

    [TestCaseSource(nameof(PathCases))]
    public void Get_ReturnsExpectedBreadcrumbs(
        string? path, string[] labels, string[] urls)
    {
        var model = GetBreadCrumbQuery.Get(path!);

        AssertItems(model, labels, urls);
    }

    [TestCaseSource(nameof(PathCases))]
    public void Invoke_ReturnsExpectedViewAndBreadcrumbs(
        string? path, string[] labels, string[] urls)
    {
        _component.HttpContext.Request.Path =
            path == null ? default(PathString) : new PathString(path);

        var model = InvokeModel();

        AssertItems(model, labels, urls);
    }

    [Test]
    public void Get_WhitespacePath_ReturnsEmpty()
    {
        var model = GetBreadCrumbQuery.Get(" \t\r\n");

        Assert.That(model.Items, Is.Empty);
    }

    [TestCase(true, false, null)]
    [TestCase(false, true, null)]
    [TestCase(false, true, "My Story")]
    [TestCase(true, true, "Stories")]
    public void Get_ReturnsArticleBreadcrumbs(
        bool landing, bool post, string? title)
    {
        var model = GetBreadCrumbQuery.Get(
            "/articles/story", title!, landing, post);

        AssertArticle(model, landing, title);
    }

    [TestCase("Landing", null)]
    [TestCase("Post", "My Story")]
    [TestCase("Overlap", "Stories")]
    [TestCase("Lowercase", null)]
    public void Invoke_DetectsArticleModels(
        string kind, string? title)
    {
        _component.HttpContext.Request.Path = "/articles/story";
        _component.ViewContext.ViewData["Title"] = title;

        _component.ViewContext.ViewData.Model = kind switch
        {
            "Landing" => new ArticleLandingPage(),
            "Post" => new ArticlePostPage(),
            "Overlap" => new ArticleLandingArticlePostPage(),
            _ => new articlepostpage()
        };

        var model = InvokeModel();

        if (kind == "Lowercase")
        {
            // Current detection is case-sensitive.
            AssertItems(
                model,
                new[] { "Articles", "Story" },
                new[] { "/articles", "" });
        }
        else
        {
            AssertArticle(model, kind != "Post", title);
        }
    }

    private BreadCrumbViewModel InvokeModel()
    {
        var result = _component.Invoke();

        Assert.That(result, Is.TypeOf<ViewViewComponentResult>());

        var view = (ViewViewComponentResult)result;

        Assert.That(view.ViewName, Is.EqualTo(
            "~/Components/ViewComponents/BreadCrumb/BreadCrumb.cshtml"));

        Assert.That(view.ViewData.Model,
            Is.TypeOf<BreadCrumbViewModel>());

        return (BreadCrumbViewModel)view.ViewData.Model!;
    }

    private static void AssertArticle(
        BreadCrumbViewModel model, bool landing, string? title)
    {
        var label = string.IsNullOrEmpty(title)
            ? landing ? "The Explorer's Log" : "Article"
            : title;

        AssertItems(
            model,
            landing
                ? new[] { label }
                : new[] { "The Explorer's Log", label },
            landing
                ? new[] { "" }
                : new[] { "/articles", "" });
    }

    private static void AssertItems(
        BreadCrumbViewModel model, string[] labels, string[] urls)
    {
        var items = model.Items.ToArray();

        Assert.That(items.Length, Is.EqualTo(labels.Length));

        Assert.Multiple(() =>
        {
            for (var i = 0; i < items.Length; i++)
            {
                Assert.That(items[i].Label, Is.EqualTo(labels[i]));
                Assert.That(items[i].Url, Is.EqualTo(urls[i]));
                Assert.That(items[i].IsCurrentPage,
                    Is.EqualTo(i == items.Length - 1));
            }
        });
    }

    public static IEnumerable<TestCaseData> RepeatedRootCases()
    {
        yield return new TestCaseData("//", null);
        yield return new TestCaseData("////", "Story");
    }

    [TestCaseSource(nameof(RepeatedRootCases))]
    public void Get_RepeatedRootSlashes_ReturnsArticleBreadcrumbs(
        string path, string? title)
    {
        var model = GetBreadCrumbQuery.Get(
            path, title!, false, true);

        AssertItems(
            model,
            new[]
            {
            "The Explorer's Log",
            string.IsNullOrEmpty(title) ? "Article" : title!
            },
            new[] { "/articles", "" });
    }
    public sealed class ArticleLandingPage { }
    public sealed class ArticlePostPage { }
    public sealed class ArticleLandingArticlePostPage { }
    public sealed class articlepostpage { }
}