using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Content;
using ETG.Web.Features.Blog;
using ETG.Web.Features.Blog.Operations;
using ETG.Web.Infrastructure;
using ETG.Web.Rendering;
using ETG.Web.Features.Shared;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using ETG.Web.Components.ViewComponents.Pagination;
using Microsoft.AspNetCore.WebUtilities;

[assembly: RegisterPageTemplate(
    identifier: "Website.BlogLandingPage_Default",
    name: "Blog Page - Default",
    propertiesType: typeof(BlogLandingPageTemplateProperties),
    customViewName: "~/Features/Blog/BlogLandingPage_Default.cshtml",
    ContentTypeNames = [BlogLandingPage.CONTENT_TYPE_NAME],
    Description = "",
    IconClass = ""
)]

[assembly: RegisterWebPageRoute(
    contentTypeName: BlogLandingPage.CONTENT_TYPE_NAME,
    controllerType: typeof(BlogLandingPageTemplateController),
     WebsiteChannelNames = new[] { "Website" }
)]

namespace ETG.Web.Features.Blog;

public class BlogLandingPageTemplateProperties : IPageTemplateProperties { }

public class BlogLandingPageTemplateController(
    IMediator mediator,
    IWebPageUrlRetriever urlRetriever,
    WebPageMetaService metaService,
    IWebsiteChannelContext channelContext,
    ITaxonomyRetriever taxonomyRetriever,
    IWebPageDataContextRetriever contextRetriever) : Controller
{
    private readonly IMediator mediator = mediator;
    private readonly IWebPageUrlRetriever urlRetriever = urlRetriever;
    private readonly IWebsiteChannelContext channelContext = channelContext;
    private readonly WebPageMetaService metaService = metaService;
    private readonly IWebPageDataContextRetriever contextRetriever = contextRetriever;
    private readonly ITaxonomyRetriever taxonomyRetriever = taxonomyRetriever;
    private const int PageSize = 1;

    public async Task<ActionResult> Index(string? search = null, string? categories = null, string? sortBy = "latest", int page = 1)
    {
        if (!contextRetriever.TryRetrieve(out var data))
        {
            return NotFound();
        }

        var result = await mediator.Send(new BlogLandingPageQuery(data.WebPage, channelContext.WebsiteChannelName));
        var allBlogPosts = await BuildPostPageViewModels(result.BlogPosts);

        // Get all categories from ALL posts (before filtering) for the filter panel
        var allCategories = allBlogPosts
            .SelectMany(p => p.BlogCategory ?? [])
            .Select(c => c.Title)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c)
            .ToList();

        // Apply search filter
        var filteredPosts = allBlogPosts;
        if (!string.IsNullOrWhiteSpace(search))
        {
            filteredPosts = filteredPosts
                .Where(p => p.Title.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        // Apply category filter
        if (!string.IsNullOrWhiteSpace(categories))
        {
            var selectedCategories = categories.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            filteredPosts = filteredPosts
                .Where(p => p.BlogCategory.Any(c => selectedCategories.Contains(c.Title, StringComparer.OrdinalIgnoreCase)))
                .ToList();
        }

        // Apply sorting
        filteredPosts = sortBy?.ToLower() switch
        {
            "oldest" => filteredPosts.OrderBy(p => p.Date).ToList(),
            _ => filteredPosts.OrderByDescending(p => p.Date).ToList() // latest (default)
        };

        // Apply pagination
        var totalPosts = filteredPosts.Count;
        var totalPages = (int)Math.Ceiling((double)totalPosts / PageSize);
        var paginatedPosts = filteredPosts
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        //metaService.SetMeta(result.LandingPage);

        var viewModel = new BlogLandingPageViewModel
        {
            BlogPosts = paginatedPosts,
            AllCategories = allCategories,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalPosts = totalPosts,
            SearchQuery = search ?? string.Empty,
            SelectedCategories = categories ?? string.Empty,
            SortBy = sortBy ?? "latest"
        };

        return new TemplateResult(viewModel);
    }

    private async Task<IReadOnlyList<BlogPostViewModel>> BuildPostPageViewModels(IEnumerable<BlogPostPage> pages)
    {
        var vms = new List<BlogPostViewModel>();
        foreach (var page in pages)
        {
            var post = page.BlogPostPageBlogPostContent.FirstOrDefault();
            if (post is null)
            {
                continue;
            }

            var url = await urlRetriever.Retrieve(page);

            var teaserImage = post.BlogPostContentTeaserMediaFileImage?.FirstOrDefault();
            var author = post.BlogPostContentAuthor?.FirstOrDefault();

            IEnumerable<Guid> tagIdentifiers = post.BlogPostContentBlogCategory.Select(tr => tr.Identifier);
            IEnumerable<Tag> tags = await taxonomyRetriever.RetrieveTags(tagIdentifiers, "en");

            vms.Add(new BlogPostViewModel
            {
                Title = post.BasicItemTitle,
                Date = post.BlogPostContentPublishedDate,
                LinkPath = url.RelativePath,
                ShortDescription = post.BasicItemShortDescription,
                TeaserImage = teaserImage != null ? ImageAssetViewModel.Create(teaserImage) : null,
                BlogCategory = tags,
                Author = author != null ? AuthorViewModel.Create(author) : null,
            });
        }

        return vms;
    }
}

public class BlogPostViewModel
{
    public string Title { get; init; } = "";
    public DateTime Date { get; init; }
    public ImageAssetViewModel TeaserImage { get; init; }
    public IEnumerable<Tag> BlogCategory { get; init; } = [];
    public string ShortDescription { get; init; } = "";
    public string LinkPath { get; init; } = "";
    public AuthorViewModel Author { get; init; }
}

public class BlogLandingPageViewModel : IPagedViewModel
{
    public IReadOnlyList<BlogPostViewModel> BlogPosts { get; init; } = [];
    public IReadOnlyList<string> AllCategories { get; init; } = [];
    public int CurrentPage { get; init; } = 1;
    public int TotalPages { get; init; } = 1;
    public int TotalPosts { get; init; } = 0;
    public string SearchQuery { get; init; } = "";
    public string SelectedCategories { get; init; } = "";
    public string SortBy { get; init; } = "latest";

    public int Page => CurrentPage;

    public IEnumerable<KeyValuePair<string, string?>> GetRouteData(int page)
    {
        yield return new("page", page.ToString());
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            yield return new("search", SearchQuery);
        }
        if (!string.IsNullOrWhiteSpace(SelectedCategories))
        {
            yield return new("categories", SelectedCategories);
        }
        if (!string.IsNullOrWhiteSpace(SortBy) && SortBy != "latest")
        {
            yield return new("sortBy", SortBy);
        }
    }
}