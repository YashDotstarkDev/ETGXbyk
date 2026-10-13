using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using CSharpFunctionalExtensions;
using ETG.Core.Content;
using ETG.Core.Operations;
using Kentico.Content.Web.Mvc.Routing;
using MediatR;

namespace ETG.Web.Components.ViewComponents.MainNavigation;

public record GetNavigationHierarchyQuery(string RootPath, int MaxDepth = 10)
    : IQuery<Maybe<NavigationViewModel>>, ICacheByValueQuery
{
    public string CacheValueKey => $"Navigation_{RootPath}_{MaxDepth}";
}

public class GetNavigationHierarchyQueryHandler(
    ContentItemQueryTools tools,
    IMediator mediator,
    IWebPageUrlRetriever urlRetriever,
    IPreferredLanguageRetriever languageRetriever)
    : ContentItemQueryHandler<GetNavigationHierarchyQuery, Maybe<NavigationViewModel>>(tools)
{
    private readonly ContentItemQueryTools tools = tools;
    private readonly IMediator mediator = mediator;
    private readonly IWebPageUrlRetriever urlRetriever = urlRetriever;
    private readonly IPreferredLanguageRetriever languageRetriever = languageRetriever;

    public override async Task<Maybe<NavigationViewModel>> Handle(GetNavigationHierarchyQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var allItems = await GetAllLinkItems(request.RootPath, cancellationToken);

            if (allItems.Count == 0)
            {
                return Maybe.None;
            }

            var viewModel = new NavigationViewModel();

            var minDepth = allItems.Min(GetDepth);

            var rootItems = allItems
                .Where(i => GetDepth(i) == minDepth)
                .OrderBy(i => i.SystemFields.WebPageItemOrder)
                .ToList();

            var urlMap = await ResolveUrlsAsync(allItems, cancellationToken);

            foreach (var rootItem in rootItems)
            {
                viewModel.RootItems.Add(BuildNavigationItemViewModel(rootItem, 0, request.MaxDepth, allItems, urlMap));
            }

            var layoutResult = await mediator.Send(new ETG.Web.Components.ViewComponents.Shared.GetWebsiteLayoutContentQuery(), cancellationToken);
            viewModel.Layout = layoutResult.HasValue ? layoutResult.Value : null;

            return viewModel;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    // ── URL resolution ────────────────────────────────────────────────────────────

    private string ResolveChannelName()
    {
        var channelName = tools.WebsiteChannelContext.WebsiteChannelName;
        return string.IsNullOrWhiteSpace(channelName) || channelName == "Website"
            ? "ETG" // fallback for error-page re-execution context
            : channelName;
    }

    private async Task<Dictionary<Guid, string>> ResolveUrlsAsync(List<Link> items, CancellationToken cancellationToken)
    {
        //TEMP - LINKFIELDS(restore after regenerating Link.generated.cs):
         var guids = items
             .SelectMany(i => GuidsOf(i.LinkPath).Concat(GuidsOf(i.LinkCommonUrl)))
             .Distinct()
             .ToList();

        if (guids.Count == 0)
        {
            return [];
        }

        var urls = await urlRetriever.Retrieve(
            guids,
            ResolveChannelName(),
            languageRetriever.Get(),
            tools.WebsiteChannelContext.IsPreview,
            cancellationToken);

        return urls.ToDictionary(kv => kv.Key, kv => kv.Value.RelativePath);
    }

    private static IEnumerable<Guid> GuidsOf(IEnumerable<WebPageRelatedItem>? refs) =>
        (refs ?? Enumerable.Empty<WebPageRelatedItem>())
            .Select(r => r.WebPageGuid)
            .Where(g => g != Guid.Empty);

    private static string? ResolveFirst(IEnumerable<WebPageRelatedItem>? refs, Dictionary<Guid, string> urlMap)
    {
        var guid = refs?.FirstOrDefault()?.WebPageGuid;
        return guid is { } g && g != Guid.Empty && urlMap.TryGetValue(g, out var url) ? url : null;
    }

    // ── Tree building ─────────────────────────────────────────────────────────────

    private int GetDepth(Link item) =>
        item.SystemFields.WebPageItemTreePath?.Split('/', StringSplitOptions.RemoveEmptyEntries).Length ?? 0;

    private NavigationItemViewModel BuildNavigationItemViewModel(
        Link linkItem,
        int currentDepth,
        int maxDepth,
        List<Link> allItems,
        Dictionary<Guid, string> urlMap)
    {
        var viewModel = new NavigationItemViewModel
        {
            Id = linkItem.SystemFields.WebPageItemID.ToString(),
            Name = !string.IsNullOrWhiteSpace(linkItem.LinkLabel) ? linkItem.LinkLabel : linkItem.DocumentName ?? string.Empty,
            IconClass = linkItem.LinkIconClass,
            SvgIcon = linkItem.LinkSVGIcon,
             LinkTo = ResolveFirst(linkItem.LinkPath, urlMap) ?? "#",
            Target = "_self",
            HideOnMobile = false,
            HideOnDesktop = false,
            Level = currentDepth,
            ViewAllCaption = linkItem.LinkCommonCaption,
             ViewAllUrl = ResolveFirst(linkItem.LinkCommonUrl, urlMap),
        };

        if (currentDepth >= maxDepth - 1)
        {
            return viewModel;
        }

        foreach (var childItem in FindChildItems(linkItem, allItems))
        {
            viewModel.Children.Add(BuildNavigationItemViewModel(childItem, currentDepth + 1, maxDepth, allItems, urlMap));
        }

        return viewModel;
    }

    private List<Link> FindChildItems(Link parentItem, List<Link> allItems) =>
        allItems
            .Where(item => item.SystemFields.WebPageItemParentID == parentItem.SystemFields.WebPageItemID)
            .OrderBy(item => item.SystemFields.WebPageItemOrder)
            .ToList();

    private async Task<List<Link>> GetAllLinkItems(string rootPath, CancellationToken cancellationToken)
    {
        var builder = new ContentItemQueryBuilder()
            .ForContentType(
                Link.CONTENT_TYPE_NAME,
                queryParameters => queryParameters
                    .WithLinkedItems(2)
                    .ForWebsite(ResolveChannelName(), PathMatch.Children(rootPath)));

        var result = await Executor.GetMappedResult<Link>(builder, DefaultQueryOptions, cancellationToken);
        return result.ToList();
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(
        GetNavigationHierarchyQuery query,
        Maybe<NavigationViewModel> result,
        ICacheDependencyKeysBuilder builder) =>
        builder.AllContentItems(Link.CONTENT_TYPE_NAME);
}