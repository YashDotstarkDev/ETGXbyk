using CMS.Membership;
using ETG.Admin;
using ETG.Admin.Features.WebsiteSettings;
using Kentico.Xperience.Admin.Base;

[assembly: UIApplication(
    identifier: WebsiteSettingsApplicationPage.IDENTIFIER,
    type: typeof(WebsiteSettingsApplicationPage),
    slug: "website-settings",
    name: "Website Settings",
    category: WebAdminModule.WEBSITE_CATEGORY,
    icon: Icons.CogwheelSquare,
    templateName: TemplateNames.SECTION_LAYOUT)]

namespace ETG.Admin.Features.WebsiteSettings;

[UIPermission(SystemPermissions.VIEW)]
[UIPermission(SystemPermissions.UPDATE)]
[UIPermission(SystemPermissions.DELETE)]
public class WebsiteSettingsApplicationPage : ApplicationPage
{
    public const string IDENTIFIER = "Website.WebsiteSettings";
}
