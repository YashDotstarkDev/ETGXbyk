using System;
using System.Threading.Tasks;

using CMS.Membership;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

using ETG.Admin.Features.ETGPayment;
using ETG.Module.Booking.Classes.Info;

// Payments listing
[assembly: UIPage(
    parentType: typeof(ETGPaymentApplicationPage),
    slug: "payments",
    uiPageType: typeof(PaymentsListingPage),
    name: "List of payments",
    templateName: TemplateNames.LISTING,
    order: UIPageOrder.First)]

// Selected payment
[assembly: UIPage(
    parentType: typeof(PaymentsListingPage),
    slug: PageParameterConstants.PARAMETERIZED_SLUG,
    uiPageType: typeof(PaymentViewSection),
    name: "Payment",
    templateName: TemplateNames.SECTION_LAYOUT,
    order: UIPageOrder.First)]

// Read-only payment details
[assembly: UIPage(
    parentType: typeof(PaymentViewSection),
    slug: "view",
    uiPageType: typeof(PaymentViewPage),
    name: "Payment details",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.First)]

namespace ETG.Admin.Features.ETGPayment
{
    [UIEvaluatePermission(SystemPermissions.VIEW)]
    public class PaymentsListingPage : ListingPage
    {
        protected override string ObjectType => PaymentInfo.OBJECT_TYPE;

        public override Task ConfigurePage()
        {
            var columns = PageConfiguration.ColumnConfigurations;

            columns.AddColumn(
                nameof(PaymentInfo.PaymentMerchantUniquePaymentId),
                "Merchant payment ID");

            columns.AddColumn(
                nameof(PaymentInfo.PaymentFirstName),
                "First name");

            columns.AddColumn(
                nameof(PaymentInfo.PaymentLastName),
                "Last name");

            columns.AddColumn(
                nameof(PaymentInfo.PaymentEmail),
                "Email");

            columns.AddColumn(
                nameof(PaymentInfo.PaymentContactNumber),
                "Contact number");

            columns.AddColumn(
                nameof(PaymentInfo.PaymentInvoiceReference),
                "Invoice reference");

            columns.AddColumn(
                nameof(PaymentInfo.PaymentAmount),
                "Amount");

            columns.AddColumn(
                nameof(PaymentInfo.PaymentAmountPaid),
                "Amount paid");

            columns.AddColumn(
                nameof(PaymentInfo.PaymentReference),
                "Payment reference");

            columns.AddColumn(
                nameof(PaymentInfo.PaymentCreated),
                "Created");

            PageConfiguration.AddEditRowAction<PaymentViewPage>();

            return base.ConfigurePage();
        }

        public override Task<ICommandResponse<RowActionResult>> Delete(int id)
        {
            throw new InvalidOperationException(
                "Payments are read-only and cannot be deleted.");
        }
    }

    [UIEvaluatePermission(SystemPermissions.VIEW)]
    public class PaymentViewSection : EditSectionPage<PaymentInfo>
    {
    }

    [UIEvaluatePermission(SystemPermissions.VIEW)]
    public class PaymentViewPage : InfoEditPage<PaymentInfo>
    {
        private const string VIEW_FORM_NAME = "view";

        [PageParameter(typeof(IntPageModelBinder))]
        public override int ObjectId { get; set; }

        public PaymentViewPage(
            IFormComponentMapper formComponentMapper,
            IFormDataBinder formDataBinder)
            : base(formComponentMapper, formDataBinder)
        {
        }

        protected override string GetDefaultUIFormName()
        {
            return VIEW_FORM_NAME;
        }

        public override async Task ConfigurePage()
        {
            // Select the UI form before the base configuration runs.
            PageConfiguration.UIFormName = VIEW_FORM_NAME;

            await base.ConfigurePage();

            PageConfiguration.Headline = "Payment details";
            PageConfiguration.EditMode = FormEditMode.ReadOnly;
            PageConfiguration.SubmitConfiguration.Visible = false;
        }

        public override async Task<EditTemplateClientProperties>
            ConfigureTemplateProperties(
                EditTemplateClientProperties properties)
        {
            var result =
                await base.ConfigureTemplateProperties(properties);

            // Enforce read-only display for every user.
            result.EditMode = FormEditMode.ReadOnly;

            return result;
        }

        public override Task<ICommandResponse> Submit(
            FormSubmissionCommandArguments args)
        {
            throw new InvalidOperationException(
                "Payments are read-only and cannot be updated.");
        }
    }
}