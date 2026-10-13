using System.ComponentModel;
using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using CSharpFunctionalExtensions;
using ETG.Core.Components;
using ETG.Core.Content;
using ETG.Core.Operations;
using ETG.Web.Components;
using ETG.Web.Components.PageBuilder.Widgets;
using ETG.Web.Components.PageBuilder.Widgets.RelatedBlogs;
using ETG.Web.Features.Shared;
using ETG.Web.Rendering;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using XperienceCommunity.KenticoComponentIcons;

[assembly: RegisterWidget(
    identifier: RelatedBlogsWidget.IDENTIFIER,
    viewComponentType: typeof(RelatedBlogsWidget),
    name: RelatedBlogsWidget.NAME,
    propertiesType: typeof(RelatedBlogsWidgetProperties),
    Description = "Displays a grid of related blog posts. Can be configured to show manually selected posts or the latest posts from the channel.",
    IconClass = KenticoIcons.CHECKLIST,
    AllowCache = true)]

namespace ETG.Web.Components.PageBuilder.Widgets.RelatedBlogs;

public class RelatedBlogsWidget(
    IMediator mediator,
    IWebPageUrlRetriever urlRetriever,
    IWebsiteChannelContext channelContext,
    ITaxonomyRetriever taxonomyRetriever) : ViewComponent
{
    public const string IDENTIFIER = "Website.RelatedBlogsWidget";
    public const string NAME = "Related Blogs";

    private readonly IMediator mediator = mediator;
    private readonly IWebPageUrlRetriever urlRetriever = urlRetriever;
    private readonly IWebsiteChannelContext channelContext = channelContext;
    private readonly ITaxonomyRetriever taxonomyRetriever = taxonomyRetriever;

    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<RelatedBlogsWidgetProperties> cvm)
    {
        var props = cvm.Properties;

        IReadOnlyList<BlogPostPage> pages;

        if (props.DataSourceParsed == RelatedBlogsDataSource.Manual && props.SelectedPosts.Any())
        {
            // ── Manual selection ──────────────────────────────────────────────────────
            // ContentItemReference.Identifier is the ContentItemGUID (NOT WebPageItemGUID).
            // RelatedBlogsManualQuery filters by ContentItemGUID and preserves selection order.
            var contentItemGuids = props.SelectedPosts.Select(r => r.Identifier).ToList();

            var response = await mediator.Send(new RelatedBlogsManualQuery(
                channelContext.WebsiteChannelName,
                contentItemGuids));

            pages = response.Items;
        }
        else
        {
            // ── Automatic — latest posts ──────────────────────────────────────────────
            var response = await mediator.Send(new RelatedBlogsQuery(
                channelContext.WebsiteChannelName,
                Count: props.MaxPosts));

            pages = response.Items;
        }

        return await Validate(props, pages)
            .Match(
                async vm => View("~/Components/PageBuilder/Widgets/RelatedBlogs/RelatedBlogs.cshtml", vm),
                async vm => View("~/Components/ComponentError.cshtml", vm));
    }

    private async Task<Result<RelatedBlogsWidgetViewModel, ComponentErrorViewModel>> Validate(
        RelatedBlogsWidgetProperties props,
        IReadOnlyList<BlogPostPage> pages)
    {
        if (pages.Count == 0)
        {
            return Result.Failure<RelatedBlogsWidgetViewModel, ComponentErrorViewModel>(
                new ComponentErrorViewModel(NAME, ComponentType.Widget,
                    "No blog posts found. Make sure blog posts are published and visible on this channel."));
        }

        var items = new List<RelatedBlogPostItem>();

        foreach (var page in pages)
        {
            var content = page.BlogPostPageBlogPostContent.FirstOrDefault();
            if (content is null) continue;

            var url = await urlRetriever.Retrieve(page);
            var teaserImage = content.BlogPostContentTeaserMediaFileImage?.FirstOrDefault();
            var author = content.BlogPostContentAuthor?.FirstOrDefault();

            IEnumerable<Tag> tags = [];
            if (props.ShowTags && content.BlogPostContentBlogCategory.Any())
            {
                var tagGuids = content.BlogPostContentBlogCategory.Select(t => t.Identifier);
                tags = await taxonomyRetriever.RetrieveTags(tagGuids, "en");
            }

            items.Add(new RelatedBlogPostItem
            {
                Title = content.BasicItemTitle ?? "",
                ShortDescription = content.BasicItemShortDescription ?? "",
                PublishedDate = content.BlogPostContentPublishedDate,
                LinkPath = url.RelativePath,
                TeaserImage = teaserImage != null ? ImageAssetViewModel.Create(teaserImage) : null,
                Author = author != null ? AuthorViewModel.Create(author) : null,
                Tags = tags
            });
        }

        if (items.Count == 0)
        {
            return Result.Failure<RelatedBlogsWidgetViewModel, ComponentErrorViewModel>(
                new ComponentErrorViewModel(NAME, ComponentType.Widget,
                    "The selected posts have no associated blog content items."));
        }

        return new RelatedBlogsWidgetViewModel(props, items);
    }
}

// ── Properties ────────────────────────────────────────────────────────────────

public class RelatedBlogsWidgetProperties : BaseWidgetProperties
{
    // ── Content ───────────────────────────────────────────────────────────

    [TextInputComponent(
        Label = "Section heading",
        ExplanationText = "Optional heading displayed above the blog card grid.",
        WatermarkText = "Related Posts",
        Order = 1)]
    public string Heading { get; set; } = "Related Posts";

    [DropDownComponent(
        Label = "Data source",
        ExplanationText = "Choose how posts are selected — latest published posts, or a manual selection.",
        DataProviderType = typeof(EnumDropDownOptionsProvider<RelatedBlogsDataSource>),
        Order = 2)]
    public string DataSource { get; set; } = nameof(RelatedBlogsDataSource.Latest);

    public RelatedBlogsDataSource DataSourceParsed =>
        EnumDropDownOptionsProvider<RelatedBlogsDataSource>.Parse(DataSource, RelatedBlogsDataSource.Latest);

    [ContentItemSelectorComponent(
        BlogPostPage.CONTENT_TYPE_NAME,
        Label = "Selected posts",
        ExplanationText = "Choose the blog posts to display. The display order follows your selection order.",
        MinimumItems = 1,
        AllowContentItemCreation = false,
        Order = 3)]
    [VisibleIfEqualTo(nameof(DataSource), nameof(RelatedBlogsDataSource.Manual), StringComparison.OrdinalIgnoreCase)]
    public IEnumerable<ContentItemReference> SelectedPosts { get; set; } = [];

    [NumberInputComponent(
        Label = "Max posts",
        ExplanationText = "Maximum number of posts to display (recommended: 3–12).",
        Order = 4)]
    [VisibleIfNotEqualTo(nameof(DataSource), nameof(RelatedBlogsDataSource.Manual), StringComparison.OrdinalIgnoreCase)]
    public int MaxPosts { get; set; } = 3;

    // ── Display ───────────────────────────────────────────────────────────

    [DropDownComponent(
        Label = "Columns",
        ExplanationText = "Number of columns in the card grid.",
        DataProviderType = typeof(EnumDropDownOptionsProvider<RelatedBlogsColumns>),
        Order = 5)]
    public string Columns { get; set; } = nameof(RelatedBlogsColumns.Three);

    public RelatedBlogsColumns ColumnsParsed =>
        EnumDropDownOptionsProvider<RelatedBlogsColumns>.Parse(Columns, RelatedBlogsColumns.Three);

    [CheckBoxComponent(
        Label = "Show teaser image",
        ExplanationText = "Display the post's teaser image at the top of each card.",
        Order = 6)]
    public bool ShowImage { get; set; } = true;

    [CheckBoxComponent(
        Label = "Show author",
        ExplanationText = "Display the author's name and avatar on each card.",
        Order = 7)]
    public bool ShowAuthor { get; set; } = true;

    [CheckBoxComponent(
        Label = "Show published date",
        ExplanationText = "Display the post's published date on each card.",
        Order = 8)]
    public bool ShowDate { get; set; } = true;

    [CheckBoxComponent(
        Label = "Show tags",
        ExplanationText = "Display the blog category tags on each card.",
        Order = 9)]
    public bool ShowTags { get; set; } = true;

    [CheckBoxComponent(
        Label = "Show short description",
        ExplanationText = "Display the post's short description below the title.",
        Order = 10)]
    public bool ShowDescription { get; set; } = true;

    [DropDownComponent(
        Label = "Card style",
        ExplanationText = "Visual style applied to each post card.",
        DataProviderType = typeof(EnumDropDownOptionsProvider<RelatedBlogsCardStyle>),
        Order = 11)]
    public string CardStyle { get; set; } = nameof(RelatedBlogsCardStyle.WithBorder);

    public RelatedBlogsCardStyle CardStyleParsed =>
        EnumDropDownOptionsProvider<RelatedBlogsCardStyle>.Parse(CardStyle, RelatedBlogsCardStyle.WithBorder);

    [DropDownComponent(
        Label = "Top margin",
        ExplanationText = "Spacing above the widget.",
        DataProviderType = typeof(EnumDropDownOptionsProvider<RelatedBlogsMargin>),
        Order = 12)]
    public string TopMargin { get; set; } = nameof(RelatedBlogsMargin.None);

    public RelatedBlogsMargin TopMarginParsed =>
        EnumDropDownOptionsProvider<RelatedBlogsMargin>.Parse(TopMargin, RelatedBlogsMargin.None);

    [DropDownComponent(
        Label = "Bottom margin",
        ExplanationText = "Spacing below the widget.",
        DataProviderType = typeof(EnumDropDownOptionsProvider<RelatedBlogsMargin>),
        Order = 13)]
    public string BottomMargin { get; set; } = nameof(RelatedBlogsMargin.None);

    public RelatedBlogsMargin BottomMarginParsed =>
        EnumDropDownOptionsProvider<RelatedBlogsMargin>.Parse(BottomMargin, RelatedBlogsMargin.None);
}

// ── Enums ─────────────────────────────────────────────────────────────────────

public enum RelatedBlogsDataSource
{
    [Description("Latest posts")]
    Latest,

    [Description("Manually selected")]
    Manual
}

public enum RelatedBlogsColumns
{
    [Description("1 Column")]
    One,

    [Description("2 Columns")]
    Two,

    [Description("3 Columns")]
    Three,

    [Description("4 Columns")]
    Four
}

public enum RelatedBlogsCardStyle
{
    [Description("With border & shadow")]
    WithBorder,

    [Description("Minimal (no border)")]
    Minimal
}

public enum RelatedBlogsMargin
{
    [Description("None")]
    None,

    [Description("Small")]
    Small,

    [Description("Medium")]
    Medium,

    [Description("Large")]
    Large
}

// ── DTOs ─────────────────────────────────────────────────────────────────────

public class RelatedBlogPostItem
{
    public string Title { get; init; } = "";
    public string ShortDescription { get; init; } = "";
    public DateTime PublishedDate { get; init; }
    public string LinkPath { get; init; } = "";
    public ImageAssetViewModel? TeaserImage { get; init; }
    public AuthorViewModel? Author { get; init; }
    public IEnumerable<Tag> Tags { get; init; } = [];
}

// ── ViewModel ─────────────────────────────────────────────────────────────────

public class RelatedBlogsWidgetViewModel : BaseWidgetViewModel
{
    protected override string WidgetName { get; } = RelatedBlogsWidget.NAME;

    public string Heading { get; }
    public bool HasHeading { get; }
    public IReadOnlyList<RelatedBlogPostItem> Posts { get; }
    public RelatedBlogsColumns Columns { get; }
    public bool ShowImage { get; }
    public bool ShowAuthor { get; }
    public bool ShowDate { get; }
    public bool ShowTags { get; }
    public bool ShowDescription { get; }
    public RelatedBlogsCardStyle CardStyle { get; }
    public RelatedBlogsMargin TopMargin { get; }
    public RelatedBlogsMargin BottomMargin { get; }

    /// <summary>Bootstrap column class derived from the Columns setting.</summary>
    public string ColClass => Columns switch
    {
        RelatedBlogsColumns.One  => "col-12",
        RelatedBlogsColumns.Two  => "col-12 col-sm-6",
        RelatedBlogsColumns.Four => "col-12 col-sm-6 col-lg-3",
        _                        => "col-12 col-sm-6 col-lg-4"   // Three (default)
    };

    public RelatedBlogsWidgetViewModel(
        RelatedBlogsWidgetProperties props,
        IReadOnlyList<RelatedBlogPostItem> posts)
    {
        Heading      = props.Heading ?? "";
        HasHeading   = !string.IsNullOrWhiteSpace(props.Heading);
        Posts        = posts;
        Columns      = props.ColumnsParsed;
        ShowImage    = props.ShowImage;
        ShowAuthor   = props.ShowAuthor;
        ShowDate     = props.ShowDate;
        ShowTags     = props.ShowTags;
        ShowDescription = props.ShowDescription;
        CardStyle    = props.CardStyleParsed;
        TopMargin    = props.TopMarginParsed;
        BottomMargin = props.BottomMarginParsed;
    }
}
