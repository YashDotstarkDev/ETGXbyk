using System.Threading.Tasks;
using CMS.ContactManagement;
using CMS.Membership;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Admin.DigitalMarketing.UIPages;
using ETG.Admin.Features.ContactManagement;

[assembly: UIPage(
    parentType: typeof(ContactEditSection),
    slug: "unsorted-fields",
    uiPageType: typeof(ContactUnsortedFieldsEditPage),
    name: "Unsorted fields",
    templateName: TemplateNames.EDIT,
    order: 200,
    Icon = Icons.Tag)]

namespace ETG.Admin.Features.ContactManagement;

[UIEvaluatePermission(SystemPermissions.UPDATE)]
public class ContactUnsortedFieldsEditPage(
    IFormComponentMapper formComponentMapper,
    IFormDataBinder formDataBinder)
    : InfoEditPage<ContactInfo>(
        formComponentMapper,
        formDataBinder)
{
    [PageParameter(
        typeof(IntPageModelBinder),
        typeof(ContactEditSection))]
    public override int ObjectId { get; set; }

    public override Task ConfigurePage()
    {
        // Must match your UI Form Code name from the screenshot: UnsortedField
        PageConfiguration.UIFormName = "UnsortedField";

        return base.ConfigurePage();
    }
}