using System;
using System.Threading;
using System.Threading.Tasks;
using CMS;
using CMS.Base;
using CMS.Core;
using CMS.Membership;
using CMS.Websites;
using ETG.Core.Content;

[assembly: RegisterModule(typeof(ETG.Admin.Services.TourVanityUrlModule))]

namespace ETG.Admin.Services;

public sealed class TourVanityUrlModule : CMS.DataEngine.Module
{
    public TourVanityUrlModule() : base(nameof(TourVanityUrlModule)) { }

    protected override void OnPreInit(ModulePreInitParameters parameters)
    {
        base.OnPreInit(parameters);
        parameters.Services
            .AddEventHandler<AfterPublishWebPageEvent, TourVanityUrlHandler>();
    }
}

public sealed class TourVanityUrlHandler(TourVanityUrlService service)
    : IAsyncEventHandler<AfterPublishWebPageEvent>
{
    public async Task HandleAsync(AfterPublishWebPageEvent e, CancellationToken ct)
    {
        var page = e.Data;
        var user = MembershipContext.AuthenticatedUser;

        if (!string.Equals(page.ContentTypeName, Tour.CONTENT_TYPE_NAME, StringComparison.OrdinalIgnoreCase)
            || user is null || user.UserID <= 0)
        {
            return;
        }

        await service.ProcessAsync(
            page.WebsiteChannelID, user.UserID, page.ID, page.ContentLanguageName, apply: true, ct);
    }
}
