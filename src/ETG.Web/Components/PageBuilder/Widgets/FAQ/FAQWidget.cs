using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using CSharpFunctionalExtensions;
using ETG.Core.Components;
using ETG.Core.Content;
using ETG.Web.Components.PageBuilder.Widgets.FAQ;
using Kentico.Components.Web.Mvc.FormComponents;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Admin.Websites.FormAnnotations;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using XperienceCommunity.KenticoComponentIcons;

[assembly: RegisterWidget(
    identifier: FAQWidget.IDENTIFIER,
    viewComponentType: typeof(FAQWidget),
    name: FAQWidget.NAME,
    propertiesType: typeof(FAQWidgetProperties),
    Description = "Displays FAQ items in an expandable accordion format",
    IconClass = KenticoIcons.CHECKLIST,
    AllowCache = false)]

namespace ETG.Web.Components.PageBuilder.Widgets.FAQ;

public class FAQWidget(IContentQueryExecutor executor, IWebsiteChannelContext websiteChannelContext) : ViewComponent
{
    public const string IDENTIFIER = "ETG.Web.Widget.FAQWidget";
    public const string NAME = "FAQ";

    private readonly IContentQueryExecutor executor = executor;
    private readonly IWebsiteChannelContext websiteChannelContext =
      websiteChannelContext;

    public async Task<IViewComponentResult> InvokeAsync(
        ComponentViewModel<FAQWidgetProperties> cvm)
    {
        var props = cvm.Properties;

        using var dependencyCollector = new CacheDependencyCollector();

        var (faqItems, faqPage) = await GetFAQItemsFromPage(props);


        cvm.CacheDependencies.CacheKeys =
            dependencyCollector.GetCacheDependency()?.CacheKeys ?? [];

        return Validate(faqItems, faqPage, props)
            .Match(
                vm => View(
                    "~/Components/PageBuilder/Widgets/FAQ/FAQ.cshtml",
                    vm),
                vm => View("~/Components/ComponentError.cshtml", vm));
    }

    private async Task<(
    IReadOnlyList<FAQItemViewModel> Items,
    Maybe<Folder> Page)> GetFAQItemsFromPage(
    FAQWidgetProperties props)
    {
        // 1. Selected content item GUIDs (a page's content item GUID equals its page GUID)
        var pageGuids = (props.Path ?? [])
            .Where(x => x != null && x.Identifier != Guid.Empty)
            .Select(x => x.Identifier)
            .Distinct()
            .ToList();

        if (pageGuids.Count == 0)
        {
            return ([], Maybe<Folder>.None);
        }

        // 2. Selected Folder page, with its linked items (SelectFAQ) loaded
        var folderQuery = new ContentItemQueryBuilder()
            .ForContentType(
                Folder.CONTENT_TYPE_NAME,
                config => config
                    .ForWebsite(websiteChannelContext.WebsiteChannelName)
                    .WithLinkedItems(1)
                    .Where(where => where.WhereIn("ContentItemGUID", pageGuids))
                    .TopN(1));
            //.InLanguage(ContentLanguage);

        var folders = (await executor.GetMappedWebPageResult<Folder>(
                folderQuery,
                cancellationToken: CancellationToken.None))
            .ToList();

        var selectedFolder = folders.FirstOrDefault();

        if (selectedFolder == null)
        {
            return ([], Maybe<Folder>.None);
        }

        // 3. Read the Folder's own stored FAQ field value (no republish needed)
        ContentItemQueryBuilder BuildFolderQuery() =>
            new ContentItemQueryBuilder().ForContentType(
                Folder.CONTENT_TYPE_NAME,
                config => config
                    .ForWebsite(websiteChannelContext.WebsiteChannelName)
                    .Where(where => where.WhereIn("ContentItemGUID", pageGuids))
                    .TopN(1));

        var rawValues = (await executor.GetWebPageResult(
                BuildFolderQuery(),
                container => container.GetValue<string>("SelectedFAQs"),
                cancellationToken: CancellationToken.None))
            .ToList();

        // Pull the FAQ GUIDs out of the stored value, in the editor's order
        var faqGuids = rawValues
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .SelectMany(v => Regex
                .Matches(v!, @"[0-9a-fA-F]{8}(-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}")
                .Select(m => Guid.Parse(m.Value)))
            .Distinct()
            .ToList();

        if (faqGuids.Count == 0)
        {
            return ([], folders.TryFirst());
        }

        // 4. Fetch the FAQ items by GUID
        var faqQuery = new ContentItemQueryBuilder()
            .ForContentType(
                ETG.FAQ.CONTENT_TYPE_NAME,
                config => config.Where(where => where.WhereIn("ContentItemGUID", faqGuids)));

        var faqs = (await executor.GetMappedResult<ETG.FAQ>(
                faqQuery,
                cancellationToken: CancellationToken.None))
            .GroupBy(faq => faq.SystemFields.ContentItemGUID)
            .Select(group => group.First())
            .OrderBy(faq => faqGuids.IndexOf(faq.SystemFields.ContentItemGUID))
            .ToList();

        // 5. Count limit (0 = all) and map to the view model
        var limitedFaqs = props.Count > 0
            ? faqs.Take(props.Count)
            : faqs;

        var items = limitedFaqs
            .Select(faq => new FAQItemViewModel(
                faq.FAQQuestion ?? string.Empty,
                faq.FAQAnswer ?? string.Empty))
            .ToList();

        return (items, folders.TryFirst());
    }

    private static Result<FAQWidgetViewModel, ComponentErrorViewModel> Validate(
        IReadOnlyList<FAQItemViewModel> items,
        Maybe<Folder> faqPage,
        FAQWidgetProperties props)
    {
        if (items.Count == 0)
        {
            return Result.Failure<FAQWidgetViewModel, ComponentErrorViewModel>(
                new ComponentErrorViewModel(
                    NAME,
                    ComponentType.Widget,
                    "Select the FAQ page or ensure the selected page contains FAQ items."));
        }

        return new FAQWidgetViewModel(props, items, faqPage);
    }
}

public class FAQWidgetProperties : BaseWidgetProperties
{
    [TextInputComponent(Label = "Heading", Order = 1)]
    public string Heading { get; set; } = "";

    [NumberInputComponent(Label = "Count", Order = 2)]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public int Count { get; set; } = 10;

    [CheckBoxComponent(Label = "Hide View All", Order = 3)]
    public bool HideViewAll { get; set; }

    [TextInputComponent(Label = "FAQ CTA Text", Order = 4)]
    [VisibleIfEqualTo(nameof(HideViewAll), false)]
    public string FaqCtaText { get; set; } = "View All";

    [TextInputComponent(Label = "FAQ CTA Link", Order = 5)]
    [VisibleIfEqualTo(nameof(HideViewAll), false)]
    public string FaqCtaLink { get; set; } = "/faqs";

    [ContentItemSelectorComponent(
           contentTypeName: Folder.CONTENT_TYPE_NAME,
           Label = "Path",
           Order = 6)]
    public IEnumerable<ContentItemReference> Path { get; set; } = [];
}

public record FAQItemViewModel(string Question, string AnswerHtml);

public class FAQWidgetViewModel
{
    public string Heading { get; }
    public string DisplayLabel { get; }
    public bool HasLabel { get; }
    public IReadOnlyList<FAQItemViewModel> FAQItems { get; }
    public bool HideViewAll { get; }
    public string FaqCtaText { get; }
    public string FaqCtaLink { get; }
    public Guid WidgetInstanceID { get; }
    public string? GroupTitle { get; }
    public string? GroupDescription { get; }
    public bool HasGroupDescription { get; }

    public FAQWidgetViewModel(
        FAQWidgetProperties props,
        IReadOnlyList<FAQItemViewModel> items,
        Maybe<Folder> faqPage)
    {
        Heading = props.Heading ?? "";
        DisplayLabel = props.Heading ?? "";
        HasLabel = !string.IsNullOrWhiteSpace(DisplayLabel);
        FAQItems = items ?? [];
        HideViewAll = props.HideViewAll;
        FaqCtaText = props.FaqCtaText ?? "";
        FaqCtaLink = string.IsNullOrWhiteSpace(props.FaqCtaLink) ? new FAQWidgetProperties().FaqCtaLink : props.FaqCtaLink;
        ;
        WidgetInstanceID = props.ID;

        GroupTitle = faqPage.TryGetValue(out var page)
            ? page.FolderName
            : null;

        // The Folder type has no description field.
        GroupDescription = null;
        HasGroupDescription = false;
    }
}