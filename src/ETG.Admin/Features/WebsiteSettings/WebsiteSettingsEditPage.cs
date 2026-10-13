using CMS.DataEngine;
using CMS.Membership;
using ETG.Admin.Features.WebsiteSettings;
using ETG.Core.Modules;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: UIPage(
    parentType: typeof(WebsiteSettingsApplicationPage),
    slug: "config",
    uiPageType: typeof(WebsiteSettingsEditPage),
    name: "Configuration",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace ETG.Admin.Features.WebsiteSettings;

[UIEvaluatePermission(SystemPermissions.UPDATE)]
public class WebsiteSettingsEditPage(
    IInfoProvider<WebsiteSettingsInfo> configProvider,
    IFormComponentMapper formComponentMapper,
    IFormDataBinder formDataBinder,
    TimeProvider time)
    : InfoEditPage<WebsiteSettingsInfo>(formComponentMapper, formDataBinder)
{
    private readonly IInfoProvider<WebsiteSettingsInfo> configProvider = configProvider;
    private readonly TimeProvider time = time;

    // Not used since we only edit the singleton configuration
    public override int ObjectId { get; set; }

    protected override async Task<WebsiteSettingsInfo> GetInfoObject(CancellationToken? cancellationToken = null)
    {
        var config = (await configProvider.Get()
            .TopN(1)
            .GetEnumerableTypedResultAsync())
            .FirstOrDefault();

        if (config is null)
        {
            var now = time.GetLocalNow().DateTime;

            config = new WebsiteSettingsInfo
            {
                WebsiteSettingsWebsiteGlobalContent = [],
                WebsiteSettingsDateCreated = now,
                WebsiteSettingsName = "WebsiteSettings"
            };
            config.Insert();
        }

        ObjectId = config.WebsiteSettingsID;

        return config;
    }
}
