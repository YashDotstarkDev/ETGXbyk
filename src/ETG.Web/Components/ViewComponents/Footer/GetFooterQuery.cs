using CMS.ContentEngine;
using CMS.Websites;
using CSharpFunctionalExtensions;
using ETG.Core.Content;
using ETG.Core.Operations;
using Kentico.Content.Web.Mvc.Routing;
using MediatR;

namespace ETG.Web.Components.ViewComponents.Footer;

public record GetFooterQuery : IQuery<Maybe<FooterViewModel>>;

public class GetFooterQueryHandler(ContentItemQueryTools tools, IMediator mediator, IWebPageUrlRetriever urlRetriever,
    IPreferredLanguageRetriever languageRetriever)
    : ContentItemQueryHandler<GetFooterQuery, Maybe<FooterViewModel>>(tools)
{
    private const string TopMenuPath = "/Shared/Menus/Footer-Menu/Top";
    private const string BottomMenuPath = "/Shared/Menus/Footer-Menu/Bottom";
    private const string FallbackChannelName = "ETG";

    private string ResolveChannelName()
    {
        var channelName = tools.WebsiteChannelContext.WebsiteChannelName;
        return string.IsNullOrWhiteSpace(channelName) || channelName == "Website"
            ? FallbackChannelName
            : channelName;
    }

    private async Task<Dictionary<Guid, string>> ResolveUrlsAsync(List<Link> items, CancellationToken cancellationToken)
    {
        // TEMP-LINKFIELDS (restore after regenerating Link.generated.cs):
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

    public override async Task<Maybe<FooterViewModel>> Handle(
        GetFooterQuery request,
        CancellationToken cancellationToken = default)
    {
        var topLinks = await GetLinkItemsUnderPathAsync(TopMenuPath, cancellationToken);
        var bottomLinks = await GetLinkItemsUnderPathAsync(BottomMenuPath, cancellationToken);
        var urlMap = await ResolveUrlsAsync(topLinks.Concat(bottomLinks).ToList(), cancellationToken);

        var contact = await GetContactAsync(cancellationToken);

        var columns = BuildTopColumns(topLinks, urlMap);
        var bottomLinksResult = BuildBottomLinks(bottomLinks, urlMap);
        var layoutResult = await mediator.Send(new ETG.Web.Components.ViewComponents.Shared.GetWebsiteLayoutContentQuery(), cancellationToken);

        return new FooterViewModel
        {
            Columns = columns,
            BottomLinks = bottomLinksResult,
            Contact = contact,
            Layout = layoutResult.HasValue ? layoutResult.Value : null
        };
    }

    // ── Contact ────────────────────────────────────────────────────────────────────

    private async Task<FooterContactViewModel?> GetContactAsync(CancellationToken cancellationToken)
    {
        var builder = new ContentItemQueryBuilder()
            .ForContentType(Contact.CONTENT_TYPE_NAME, p => p.TopN(1));

        var result = await Executor.GetMappedResult<Contact>(builder, DefaultQueryOptions, cancellationToken);
        var contact = result.FirstOrDefault();

        if (contact is null)
        {
            return null;
        }

        return new FooterContactViewModel
        {
            OpeningHours = contact.ContactOpeningHours,
            Address1 = contact.ContactAddress1,
            Address2 = contact.ContactAddress2,
            PhoneNumber = contact.ContactPhone,
            PhoneNumber2 = contact.ContactPhone2,
            EmailAddress = contact.ContactEmail,
            FacebookUrl = contact.ContactFacebookUrl,
            InstagramUrl = contact.ContactInstagramUrl,
            LinkedInUrl = contact.ContactLinkedInUrl,
            YouTubeUrl = contact.ContactYouTubeUrl
        };
    }

    // ── Link tree helpers ─────────────────────────────────────────────────────────

    private async Task<List<Link>> GetLinkItemsUnderPathAsync(string rootPath, CancellationToken cancellationToken)
    {
        var builder = new ContentItemQueryBuilder()
            .ForContentType(
                Link.CONTENT_TYPE_NAME,
                p => p.WithLinkedItems(2)
                      .ForWebsite(ResolveChannelName(), PathMatch.Children(rootPath)));

        var result = await Executor.GetMappedResult<Link>(builder, DefaultQueryOptions, cancellationToken);
        return result.ToList();
    }

    private int GetDepth(Link item) =>
        item.SystemFields.WebPageItemTreePath?.Split('/', StringSplitOptions.RemoveEmptyEntries).Length ?? 0;

    private List<Link> FindChildLinks(Link parent, List<Link> all) =>
        all.Where(l => l.SystemFields.WebPageItemParentID == parent.SystemFields.WebPageItemID)
           .OrderBy(l => l.SystemFields.WebPageItemOrder)
           .ToList();

    // ── Column builder ────────────────────────────────────────────────────────────

    private List<FooterColumnViewModel> BuildTopColumns(List<Link> topLinks, Dictionary<Guid, string> urlMap)
    {
        if (topLinks.Count == 0) return [];

        var minDepth = topLinks.Min(GetDepth);
        var headings = topLinks.Where(l => GetDepth(l) == minDepth)
                                .OrderBy(l => l.SystemFields.WebPageItemOrder)
                                .ToList();

        var columns = new List<FooterColumnViewModel>(headings.Count);

        foreach (var heading in headings)
        {
            var children = FindChildLinks(heading, topLinks);

            var links = children.Select(child => new FooterLinkViewModel
            {
                Label = !string.IsNullOrWhiteSpace(child.LinkLabel) ? child.LinkLabel : child.DocumentName ?? "",
                 Url= ResolveFirst(child.LinkPath, urlMap) ?? "#",
                Target = "_self",
                Icon = !string.IsNullOrWhiteSpace(child.LinkIconClass) ? child.LinkIconClass : child.LinkSVGIcon
            }).ToList();

            columns.Add(new FooterColumnViewModel
            {
                Heading = !string.IsNullOrWhiteSpace(heading.LinkLabel) ? heading.LinkLabel : heading.DocumentName ?? "",
                 HeadingUrl = ResolveFirst(heading.LinkPath, urlMap),
                Links = links
            });
        }

        return columns;
    }

    // ── Bottom links builder ──────────────────────────────────────────────────────

    private List<FooterBottomLinkViewModel> BuildBottomLinks(List<Link> bottomLinks, Dictionary<Guid, string> urlMap)
    {
        if (bottomLinks.Count == 0) return [];

        var minDepth = bottomLinks.Min(GetDepth);

        return bottomLinks
            .Where(l => GetDepth(l) == minDepth)
            .OrderBy(l => l.SystemFields.WebPageItemOrder)
            .Select(l => new FooterBottomLinkViewModel
            {
                Label = !string.IsNullOrWhiteSpace(l.LinkLabel) ? l.LinkLabel : l.DocumentName ?? "",
                 Url= ResolveFirst(l.LinkPath, urlMap) ?? "#",
                Target = "_self"
            })
            .ToList();
    }

    // ── Cache dependencies ────────────────────────────────────────────────────────

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(
        GetFooterQuery query,
        Maybe<FooterViewModel> result,
        ICacheDependencyKeysBuilder builder) =>
        builder
            .AllContentItems(Link.CONTENT_TYPE_NAME)
            .AllContentItems(Contact.CONTENT_TYPE_NAME);
}