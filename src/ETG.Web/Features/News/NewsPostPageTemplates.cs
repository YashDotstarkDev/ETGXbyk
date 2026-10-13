using ETG.Core.Content;
using ETG.Web.Features.News;
using ETG.Web.Features.News.Operations;
using ETG.Web.Infrastructure;
using ETG.Web.Features.Shared;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using MediatR;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Web.Rendering;

[assembly: RegisterPageTemplate(
    identifier: "Website.NewsPostPage_Default",
    name: "News Post Page - Default",
    propertiesType: typeof(NewsPostPageTemplateProperties),
    customViewName: "~/Features/News/NewsPostPage_Default.cshtml",
    ContentTypeNames = [NewsPostPage.CONTENT_TYPE_NAME],
    Description = "",
    IconClass = ""
)]

[assembly: RegisterWebPageRoute(
    contentTypeName: NewsPostPage.CONTENT_TYPE_NAME,
    controllerType: typeof(NewsPostPageTemplateController),
    WebsiteChannelNames = new[] { "Website" }
)]

namespace ETG.Web.Features.News;

public class NewsPostPageTemplateProperties : IPageTemplateProperties { }

public class NewsPostPageTemplateController(
    IMediator mediator,
    ITaxonomyRetriever taxonomyRetriever,
    IWebPageUrlRetriever urlRetriever,
    IContentQueryExecutor executor,
    WebPageMetaService metaService,
    IWebsiteChannelContext channelContext,
    IWebPageDataContextRetriever contextRetriever) : Controller
{
    private readonly IMediator mediator = mediator;
    private readonly IWebPageDataContextRetriever contextRetriever = contextRetriever;
    private readonly IWebPageUrlRetriever urlRetriever = urlRetriever;
    private readonly WebPageMetaService metaService = metaService;
    private readonly ITaxonomyRetriever taxonomyRetriever = taxonomyRetriever;
    private readonly IWebsiteChannelContext channelContext = channelContext;
    private readonly IContentQueryExecutor executor = executor;

    public async Task<ActionResult> Index()
    {
        if (!contextRetriever.TryRetrieve(out var data))
        {
            return NotFound();
        }

        var newsPage = await mediator.Send(new NewsPostPageQuery(data.WebPage));
        var post = newsPage.NewsPostPageNewsPostContent?.FirstOrDefault();

        if (post is null)
        {
            return NotFound();
        }

        var relatedNewsResult = await mediator.Send(new NewsPostPagesLatestQuery(post, channelContext.WebsiteChannelName));
        var relatedNewsViewModels = await GetRelatedNews(relatedNewsResult.Items);

        IEnumerable<Guid> tagIdentifiers = post.NewsPostContentNewsCategory.Select(tr => tr.Identifier);
        IEnumerable<Tag> tags = await taxonomyRetriever.RetrieveTags(tagIdentifiers, "en");

        var image = post.NewsPostContentImageContent?.FirstOrDefault();
        var author = post.NewsPostContentAuthor?.FirstOrDefault();

        var vm = new NewsPostDetailViewModel()
        {
            Title = post.BasicItemTitle,
            Image = image != null ? ImageAssetViewModel.Create(image) : null,
            PublishedDate = post.NewsPostContentPublishedDate.ToString("MMM d, yyyy"),
            ShortDescription = post.BasicItemShortDescription,
            NewsCategory = tags,
            Author = author != null ? AuthorViewModel.Create(author) : null,
            NewsPostContentRelatedNews = relatedNewsViewModels,
            UnsanitizedContentHTML = new(post.NewsPostContentContentHTML),
            AbsoluteURL = $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Request.Path}",
        };

        //metaService.SetMeta(newsPage);

        return new TemplateResult(vm);
    }

    private async Task<List<NewsPostDetailViewModel>> GetRelatedNews(IReadOnlyList<NewsPostPage> newsPostPages)
    {
        var relatedNewsViewModels = new List<NewsPostDetailViewModel>();
        foreach (var newsPostPage in newsPostPages)
        {
            var url = await urlRetriever.Retrieve(newsPostPage);
            var newsPostContent = newsPostPage.NewsPostPageNewsPostContent.FirstOrDefault();
            if (newsPostContent == null) continue;

            var image = newsPostContent.NewsPostContentImageContent?.FirstOrDefault();
            var author = newsPostContent.NewsPostContentAuthor?.FirstOrDefault();

            IEnumerable<Guid> tagIdentifiers = newsPostContent.NewsPostContentNewsCategory.Select(tr => tr.Identifier);
            IEnumerable<Tag> tags = await taxonomyRetriever.RetrieveTags(tagIdentifiers, "en");

            relatedNewsViewModels.Add(new NewsPostDetailViewModel()
            {
                Title = newsPostContent.BasicItemTitle,
                Image = image != null ? ImageAssetViewModel.Create(image) : null,
                NewsCategory = tags,
                Author = author != null ? AuthorViewModel.Create(author) : null,
                ShortDescription = newsPostContent.BasicItemShortDescription,
                PublishedDate = newsPostContent.NewsPostContentPublishedDate.ToString("MMM d, yyyy"),
                UnsanitizedContentHTML = new HtmlString(newsPostContent.NewsPostContentContentHTML),
                AbsoluteURL = url.RelativePath
            });
        }
        return relatedNewsViewModels;
    }
}

public class NewsPostDetailViewModel()
{
    public IEnumerable<Tag> NewsCategory { get; init; } = [];
    public ImageAssetViewModel Image { get; init; }
    public string Title { get; init; } = "";
    public string PublishedDate { get; init; } = "";
    public string ShortDescription { get; init; } = "";
    public HtmlString UnsanitizedContentHTML { get; init; } = HtmlString.Empty;
    public string AbsoluteURL { get; init; } = "";
    public AuthorViewModel Author { get; init; }
    public IEnumerable<NewsPostDetailViewModel> NewsPostContentRelatedNews { get; init; } = [];
}
