using CMS.Membership;
using ETG.Admin;
using Kentico.Xperience.Admin.Base;

[assembly: UIApplication(
    identifier: ETG.Admin.Features.ETGBooking.ETGBookingApplicationPage.IDENTIFIER,
    type: typeof(ETG.Admin.Features.ETGBooking.ETGBookingApplicationPage),
    slug: "etg-booking",
    name: "ETG Booking",
    category: WebAdminModule.WEBSITE_CATEGORY,
    icon: Icons.Adjust,
    templateName: TemplateNames.SECTION_LAYOUT)]

namespace ETG.Admin.Features.ETGBooking
{
    [UIPermission(SystemPermissions.VIEW)]
    [UIPermission(SystemPermissions.CREATE)]
    [UIPermission(SystemPermissions.UPDATE)]
    [UIPermission(SystemPermissions.DELETE)]
    public class ETGBookingApplicationPage : ApplicationPage
    {
        public const string IDENTIFIER = "ETG.ETGBookingApplication";
    }
}