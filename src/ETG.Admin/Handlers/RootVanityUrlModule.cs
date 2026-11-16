using System.Collections.Concurrent;
using CMS;
using CMS.Base;
using CMS.Core;
using CMS.Membership;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Admin.Handlers;
using ETG.Core.Content;

[assembly: RegisterModule(typeof(RootVanityUrlModule))]

namespace ETG.Admin.Handlers;

public sealed class RootVanityUrlModule : CMS.DataEngine.Module
{
    public RootVanityUrlModule() : base(nameof(RootVanityUrlModule)) { }

    protected override void OnPreInit(ModulePreInitParameters parameters)
    {
        base.OnPreInit(parameters);
        parameters.Services.AddEventHandler<AfterPublishWebPageEvent, RootVanityUrlHandler>();
    }
}

public sealed class RootVanityUrlHandler(IWebPageUrlPathDataRetriever urlDataRetriever, IWebPageManagerFactory pageManagerFactory) : IAsyncEventHandler<AfterPublishWebPageEvent>
{
    private static readonly HashSet<string> ContentTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            Destination.CONTENT_TYPE_NAME,
        };

    private static readonly ConcurrentDictionary<string, byte> Active = new();

    public async Task HandleAsync(AfterPublishWebPageEvent e, CancellationToken ct)
    {
        var page = e.Data;
        var user = MembershipContext.AuthenticatedUser;

        if (!ContentTypes.Contains(page.ContentTypeName) || user is null || user.UserID <= 0)
        {
            return;
        }

        var key = $"{page.WebsiteChannelID}:{page.ID}:{page.ContentLanguageName}";

        if (!Active.TryAdd(key, 0))
        {
            return;
        }

        try
        {
            var manager = pageManagerFactory.Create(page.WebsiteChannelID, user.UserID);

            if (!await manager.TryCreateDraft(page.ID, page.ContentLanguageName, ct))
            {
                return;
            }

            var urls = await urlDataRetriever.GetDraftData(page.ID, page.ContentLanguageName, ct);
            var slug = urls.SystemUrlSlug?.Trim('/');
            var path = "/" + slug;

            var existing = urls.VanityUrlPaths.FirstOrDefault(p => Same(p, path));

            if (string.IsNullOrEmpty(slug) || Same(urls.SystemUrlFullPath, path) || (existing is not null && Same(urls.CanonicalUrlPath, existing)))
            {
                await manager.TryDiscardDraft(page.ID, page.ContentLanguageName, ct);
                return;
            }

            if (existing is null)
            {
                urls.AddVanityUrlPath(path);
                existing = urls.VanityUrlPaths.Single(p => Same(p, path));
            }

            urls.SetCanonicalUrlPath(existing);

            if (await manager.TryUpdateDraft(page.ID, page.ContentLanguageName, new UpdateDraftData(urls), ct))
            {
                await manager.TryPublish(page.ID, page.ContentLanguageName, ct);
            }
        }
        finally
        {
            Active.TryRemove(key, out _);
        }
    }

    private static bool Same(string? a, string? b) => string.Equals(a?.Trim('/'), b?.Trim('/'), StringComparison.OrdinalIgnoreCase);
}
