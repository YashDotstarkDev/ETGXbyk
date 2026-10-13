using ETG.Core.Content;
using ETG.Web.Features.Blog;
using ETG.Web.Features.Blog.Operations;
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
    identifier: "Website.BlogPostPage_Default",
    name: "Blog Post Page - Default",
    propertiesType: typeof(BlogPostPageTemplateProperties),
    customViewName: "~/Features/Blog/BlogPostPage_Default.cshtml",
    ContentTypeNames = [BlogPostPage.CONTENT_TYPE_NAME],
    Description = "",
    IconClass = ""
)]

[assembly: RegisterWebPageRoute(
    contentTypeName: BlogPostPage.CONTENT_TYPE_NAME,
    controllerType: typeof(BlogPostPageTemplateController),
    WebsiteChannelNames = new[] { "Website" }
)]

namespace ETG.Web.Features.Blog;

public class BlogPostPageTemplateProperties : IPageTemplateProperties { }

public class BlogPostPageTemplateController(
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

        var blogPage = await mediator.Send(new BlogPostPageQuery(data.WebPage));
        var post = blogPage.BlogPostPageBlogPostContent?.FirstOrDefault();

        if (post is null)
        {
            return NotFound();
        }

        var relatedBlogsResult = await mediator.Send(new BlogPostPagesLatestQuery(post, channelContext.WebsiteChannelName));
        var relatedBlogViewModels = await GetRelatedBlogs(relatedBlogsResult.Items);

        IEnumerable<Guid> tagIdentifiers = post.BlogPostContentBlogCategory.Select(tr => tr.Identifier);
        IEnumerable<Tag> tags = await taxonomyRetriever.RetrieveTags(tagIdentifiers, "en");

        var teaserImage = post.BlogPostContentTeaserMediaFileImage?.FirstOrDefault();
        var author = post.BlogPostContentAuthor?.FirstOrDefault();

        var vm = new BlogPostDetailViewModel()
        {
            Title = post.BasicItemTitle,
            TeaserImage = teaserImage != null ? ImageAssetViewModel.Create(teaserImage) : null,
            PublishedDate = post.BlogPostContentPublishedDate.ToString("MMM d, yyyy"),
            ShortDescription = post.BasicItemShortDescription,
            BlogCategory = tags,
            Author = author != null ? AuthorViewModel.Create(author) : null,
            BlogPostContentRelatedBlogs = relatedBlogViewModels,
            UnsanitizedContentHTML = new(post.BlogPostContentContentHTML),
            AbsoluteURL = $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Request.Path}",
        };

        //metaService.SetMeta(blogPage);

        return new TemplateResult(vm);
    }

    private async Task<List<BlogPostDetailViewModel>> GetRelatedBlogs(IReadOnlyList<BlogPostPage> blogPostPages)
    {
        var relatedBlogViewModels = new List<BlogPostDetailViewModel>();
        foreach (var blogPostPage in blogPostPages)
        {
            var url = await urlRetriever.Retrieve(blogPostPage);
            var blogPostContent = blogPostPage.BlogPostPageBlogPostContent.FirstOrDefault();
            if (blogPostContent == null) continue;

            var teaserImage = blogPostContent.BlogPostContentTeaserMediaFileImage?.FirstOrDefault();
            var author = blogPostContent.BlogPostContentAuthor?.FirstOrDefault();

            IEnumerable<Guid> tagIdentifiers = blogPostContent.BlogPostContentBlogCategory.Select(tr => tr.Identifier);
            IEnumerable<Tag> tags = await taxonomyRetriever.RetrieveTags(tagIdentifiers, "en");

            relatedBlogViewModels.Add(new BlogPostDetailViewModel()
            {
                Title = blogPostContent.BasicItemTitle,
                TeaserImage = teaserImage != null ? ImageAssetViewModel.Create(teaserImage) : null,
                BlogCategory = tags,
                Author = author != null ? AuthorViewModel.Create(author) : null,
                ShortDescription = blogPostContent.BasicItemShortDescription,
                PublishedDate = blogPostContent.BlogPostContentPublishedDate.ToString("MMM d, yyyy"),
                UnsanitizedContentHTML = new HtmlString(blogPostContent.BlogPostContentContentHTML),
                AbsoluteURL = url.RelativePath
            });
        }
        return relatedBlogViewModels;
    }
}

public class BlogPostDetailViewModel()
{
    public IEnumerable<Tag> BlogCategory { get; init; } = [];
    public ImageAssetViewModel TeaserImage { get; init; }
    public string Title { get; init; } = "";
    public string PublishedDate { get; init; } = "";
    public string ShortDescription { get; init; } = "";
    public HtmlString UnsanitizedContentHTML { get; init; } = HtmlString.Empty;
    public string AbsoluteURL { get; init; } = "";
    public AuthorViewModel Author { get; init; }
    public IEnumerable<BlogPostDetailViewModel> BlogPostContentRelatedBlogs { get; init; } = [];
}