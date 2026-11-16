using CMS;
using CMS.Membership;
using ETG.Admin;
using ETG.ContactTags;
using Kentico.Xperience.Admin.Base;

[assembly: UIApplication(
    identifier: ETG.Admin.Features.ContactTags.ContactTagsApplicationPage.IDENTIFIER,
    type: typeof(ETG.Admin.Features.ContactTags.ContactTagsApplicationPage),
    slug: "contact-tags",
    name: "Contact Tags",
    category: WebAdminModule.WEBSITE_CATEGORY,
    icon: Icons.Adjust,
    templateName: TemplateNames.SECTION_LAYOUT)]

[assembly: RegisterObjectType(
    typeof(ContactTagsInfo),
    ContactTagsInfo.OBJECT_TYPE)]

namespace ETG.Admin.Features.ContactTags
{
    [UIPermission(SystemPermissions.VIEW)]
    [UIPermission(SystemPermissions.CREATE)]
    [UIPermission(SystemPermissions.UPDATE)]
    [UIPermission(SystemPermissions.DELETE)]
    public class ContactTagsApplicationPage : ApplicationPage
    {
        public const string IDENTIFIER =
            "ETG.ContactTagsApplication";
    }
}