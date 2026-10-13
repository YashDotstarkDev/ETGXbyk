using System;
using System.Threading;
using System.Threading.Tasks;
using CMS.Membership;
using ETG.Admin.Features.ETGBooking;
using ETGBooking;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: UIPage(
    parentType: typeof(ETGBookingApplicationPage),
    slug: "promotions",
    uiPageType: typeof(ETG.Admin.Features.ETGBooking.Promotion.PromotionListingPage),
    name: "Promotions",
    templateName: TemplateNames.LISTING,
    order: 2)]

[assembly: UIPage(
    parentType: typeof(ETG.Admin.Features.ETGBooking.Promotion.PromotionListingPage),
    slug: "create",
    uiPageType: typeof(ETG.Admin.Features.ETGBooking.Promotion.PromotionCreatePage),
    name: "New Promotion",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

[assembly: UIPage(
    parentType: typeof(ETG.Admin.Features.ETGBooking.Promotion.PromotionListingPage),
    slug: PageParameterConstants.PARAMETERIZED_SLUG,
    uiPageType: typeof(ETG.Admin.Features.ETGBooking.Promotion.PromotionEditSection),
    name: "Promotion",
    templateName: TemplateNames.SECTION_LAYOUT,
    order: UIPageOrder.NoOrder)]

[assembly: UIPage(
    parentType: typeof(ETG.Admin.Features.ETGBooking.Promotion.PromotionEditSection),
    slug: "edit",
    uiPageType: typeof(ETG.Admin.Features.ETGBooking.Promotion.PromotionEditPage),
    name: "Edit Promotion",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace ETG.Admin.Features.ETGBooking.Promotion
{
    [UIEvaluatePermission(SystemPermissions.VIEW)]
    public class PromotionListingPage : ListingPage
    {
        protected override string ObjectType => PromotionInfo.OBJECT_TYPE;

        public override Task ConfigurePage()
        {
            PageConfiguration.HeaderActions
                .AddLink<PromotionCreatePage>("New Promotion");

            PageConfiguration.ColumnConfigurations
                .AddColumn(
                    nameof(PromotionInfo.PromotionName),
                    "Promotion Name",
                    searchable: true,
                    sortable: true)
                .AddColumn(
                    nameof(PromotionInfo.PromotionPromoCode),
                    "Promo Code",
                    searchable: true,
                    sortable: true)
                .AddColumn(
                    nameof(PromotionInfo.PromotionFromDate),
                    "From Date",
                    sortable: true)
                .AddColumn(
                    nameof(PromotionInfo.PromotionToDate),
                    "To Date",
                    sortable: true)
                .AddColumn(
                    nameof(PromotionInfo.PromotionPercentDiscount),
                    "Percent Discount",
                    sortable: true)
                .AddColumn(
                    nameof(PromotionInfo.PromotionDollarDiscount),
                    "Dollar Discount",
                    sortable: true);

            PageConfiguration.AddEditRowAction<PromotionEditSection>();

            PageConfiguration.TableActions
                .AddDeleteAction(nameof(Delete));

            PageConfiguration.QueryModifiers.AddModifier(
                (query, _) => query.Columns(
                    nameof(PromotionInfo.PromotionID),
                    nameof(PromotionInfo.PromotionName),
                    nameof(PromotionInfo.PromotionPromoCode),
                    nameof(PromotionInfo.PromotionFromDate),
                    nameof(PromotionInfo.PromotionToDate),
                    nameof(PromotionInfo.PromotionPercentDiscount),
                    nameof(PromotionInfo.PromotionDollarDiscount)));

            return base.ConfigurePage();
        }

        [PageCommand(Permission = SystemPermissions.DELETE)]
        public override Task<ICommandResponse<RowActionResult>> Delete(int id)
            => base.Delete(id);
    }

    [UIEvaluatePermission(SystemPermissions.CREATE)]
    public class PromotionCreatePage(
        IFormComponentMapper formComponentMapper,
        IFormDataBinder formDataBinder,
        IPageLinkGenerator pageLinkGenerator)
        : CreatePage<PromotionInfo, PromotionEditSection>(
            formComponentMapper,
            formDataBinder,
            pageLinkGenerator)
    {
        public override Task ConfigurePage()
        {
            PageConfiguration.UIFormName = "create";
            return base.ConfigurePage();
        }

        protected override async Task FinalizeInfoObject(
            PromotionInfo infoObject,
            IFormFieldValueProvider fieldValueProvider,
            CancellationToken cancellationToken)
        {
            await base.FinalizeInfoObject(
                infoObject,
                fieldValueProvider,
                cancellationToken);

            infoObject.PromotionGuid = Guid.NewGuid();
            infoObject.PromotionLastModified = DateTime.UtcNow;
        }
    }

    public class PromotionEditSection : EditSectionPage<PromotionInfo>
    {
    }

    [UIEvaluatePermission(SystemPermissions.UPDATE)]
    public class PromotionEditPage(
        IFormComponentMapper formComponentMapper,
        IFormDataBinder formDataBinder)
        : InfoEditPage<PromotionInfo>(
            formComponentMapper,
            formDataBinder)
    {
        [PageParameter(
            typeof(IntPageModelBinder),
            typeof(PromotionEditSection))]
        public override int ObjectId { get; set; }

        public override Task ConfigurePage()
        {
            PageConfiguration.UIFormName = "edit";
            return base.ConfigurePage();
        }
    }
}