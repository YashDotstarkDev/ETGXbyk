using CMS;
using CMS.Membership;
using ETG.Admin;
using ETG.Admin.Features.ETG;
using ETG.Core.Modules;
using Kentico.Xperience.Admin.Base;

[assembly: UIApplication(
    identifier: "ETG.Admin.Application",
    type: typeof(ETGApplicationPage),
    slug: "etg",
    name: "ETG",
    category: WebAdminModule.WEBSITE_CATEGORY,
    icon: Icons.Building,
    templateName: TemplateNames.SECTION_LAYOUT)]

[assembly: RegisterObjectType(
    typeof(ArticleAuthorInfo),
    ArticleAuthorInfo.OBJECT_TYPE)]

[assembly: RegisterObjectType(
    typeof(ArticleCategoryInfo),
    ArticleCategoryInfo.OBJECT_TYPE)]

[assembly: RegisterObjectType(
    typeof(TourTypeInfo),
    TourTypeInfo.OBJECT_TYPE)]

namespace ETG.Admin.Features.ETG;

[UIPermission(SystemPermissions.VIEW)]
[UIPermission(SystemPermissions.CREATE)]
[UIPermission(SystemPermissions.UPDATE)]
[UIPermission(SystemPermissions.DELETE)]
public class ETGApplicationPage : ApplicationPage
{
}