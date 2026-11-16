using CMS.Membership;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.DigitalCommerce.UIPages;

using ETG.Admin.Features.ETGPayment;

[assembly: UIApplication(
    identifier: ETGPaymentApplicationPage.IDENTIFIER,
    type: typeof(ETGPaymentApplicationPage),
    slug: "etg-payment",
    name: "Payments",
    category: DigitalCommerceApplicationCategories.DIGITAL_COMMERCE,
    icon: Icons.MoneyBill,
    templateName: TemplateNames.SECTION_LAYOUT)]

namespace ETG.Admin.Features.ETGPayment
{
    [UIPermission(SystemPermissions.VIEW)]
    public class ETGPaymentApplicationPage : ApplicationPage
    {
        public const string IDENTIFIER = "ETG.PaymentApplication";
    }
}