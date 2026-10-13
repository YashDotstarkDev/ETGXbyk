using CMS;
using CMS.Membership;
using ETG.Admin;
using ETG.Admin.Features.EnquiryManagement;
using ETG.Core.Modules;
using Kentico.Xperience.Admin.Base;

[assembly: UIApplication(
    identifier: "ETG.EnquiryManagementApplication",
    type: typeof(EnquiryManagementApplicationPage),
    slug: "enquiry-management",
    name: "Enquiry Management",
    category: WebAdminModule.WEBSITE_CATEGORY,
    icon: Icons.ClipboardList,
    templateName: TemplateNames.SECTION_LAYOUT)]

[assembly: RegisterObjectType(
    typeof(RoleToDestinationLinkInfo),
    RoleToDestinationLinkInfo.OBJECT_TYPE)]

namespace ETG.Admin.Features.EnquiryManagement;

[UIPermission(SystemPermissions.VIEW)]
[UIPermission(SystemPermissions.CREATE)]
[UIPermission(SystemPermissions.UPDATE)]
[UIPermission(SystemPermissions.DELETE)]
public class EnquiryManagementApplicationPage : ApplicationPage
{
}