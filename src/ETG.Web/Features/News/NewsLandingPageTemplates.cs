using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Content;
using ETG.Web.Features.News.Operations;
using ETG.Web.Infrastructure;
using ETG.Web.Rendering;
using ETG.Web.Features.Shared;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using ETG.Web.Features.News;
using ETG.Web.Components.ViewComponents.Pagination;
using Microsoft.AspNetCore.WebUtilities;

[assembly: RegisterPageTemplate(
    identifier: "Website.NewsLandingPage_Default",
    name: "News Page - Default",
    propertiesType: typeof(NewsLandingPageTemplateProperties),
    customViewName: "~/Features/News/NewsLandingPage_Default.cshtml",
    ContentTypeNames = [NewsLandingPage.CONTENT_TYPE_NAME],
    Description = "",
    IconClass = ""
)]

[assembly: RegisterWebPageRoute(
    contentTypeName: NewsLandingPage.CONTENT_TYPE_NAME,
    controllerType: typeof(NewsLandingPageTemplateController),
    WebsiteChannelNames = new[] { "Website" }
)]

namespace ETG.Web.Features.News;

public class NewsLandingPageTemplateProperties : IPageTemplateProperties { }

public class NewsLandingPageTemplateController(
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

    public async Task<ActionResult> Index(string? search = null, int page = 1)
    {
        if (!contextRetriever.TryRetrieve(out var data))
        {
            return NotFound();
        }

        var result = await mediator.Send(new NewsLandingPageQuery(data.WebPage, channelContext.WebsiteChannelName));
        var allNewsPosts = await BuildPostPageViewModels(result.NewsPosts);

        // Apply search filter
        var filteredPosts = allNewsPosts;
        if (!string.IsNullOrWhiteSpace(search))
        {
            filteredPosts = filteredPosts
                .Where(p => p.Title.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        // Apply sorting (latest first)
        filteredPosts = filteredPosts.OrderByDescending(p => p.Date).ToList();

        // Apply pagination
        var totalPosts = filteredPosts.Count;
        var totalPages = (int)Math.Ceiling((double)totalPosts / PageSize);
        var paginatedPosts = filteredPosts
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        //metaService.SetMeta(result.LandingPage);

        var viewModel = new NewsLandingPageViewModel
        {
            NewsPosts = paginatedPosts,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalPosts = totalPosts,
            SearchQuery = search ?? string.Empty
        };

        return new TemplateResult(viewModel);
    }

    private async Task<IReadOnlyList<NewsPostViewModel>> BuildPostPageViewModels(IEnumerable<NewsPostPage> pages)
    {
        var vms = new List<NewsPostViewModel>();
        foreach (var page in pages)
        {
            var post = page.NewsPostPageNewsPostContent.FirstOrDefault();
            if (post is null)
            {
                continue;
            }

            var url = await urlRetriever.Retrieve(page);

            var image = post.NewsPostContentImageContent?.FirstOrDefault();
            var author = post.NewsPostContentAuthor?.FirstOrDefault();

            IEnumerable<Guid> tagIdentifiers = post.NewsPostContentNewsCategory.Select(tr => tr.Identifier);
            IEnumerable<Tag> tags = await taxonomyRetriever.RetrieveTags(tagIdentifiers, "en");

            vms.Add(new NewsPostViewModel
            {
                Title = post.BasicItemTitle,
                Date = post.NewsPostContentPublishedDate,
                LinkPath = url.RelativePath,
                ShortDescription = post.BasicItemShortDescription,
                Image = image != null ? ImageAssetViewModel.Create(image) : null,
                NewsCategory = tags,
                Author = author != null ? AuthorViewModel.Create(author) : null,
            });
        }

        return vms;
    }
}

public class NewsPostViewModel
{
    public string Title { get; init; } = "";
    public DateTime Date { get; init; }
    public ImageAssetViewModel Image { get; init; }
    public IEnumerable<Tag> NewsCategory { get; init; } = [];
    public string ShortDescription { get; init; } = "";
    public string LinkPath { get; init; } = "";
    public AuthorViewModel Author { get; init; }
}

public class NewsLandingPageViewModel : IPagedViewModel
{
    public IReadOnlyList<NewsPostViewModel> NewsPosts { get; init; } = [];
    public int CurrentPage { get; init; } = 1;
    public int TotalPages { get; init; } = 1;
    public int TotalPosts { get; init; } = 0;
    public string SearchQuery { get; init; } = "";

    public int Page => CurrentPage;

    public IEnumerable<KeyValuePair<string, string?>> GetRouteData(int page)
    {
        yield return new("page", page.ToString());
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            yield return new("search", SearchQuery);
        }
    }
}
