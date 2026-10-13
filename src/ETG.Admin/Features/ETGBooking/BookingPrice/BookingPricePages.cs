using System.Threading.Tasks;
using CMS.Membership;
using ETG.Admin.Features.ETGBooking;
using ETG.Booking.Pricing.Classes.Info;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: UIPage(
    parentType: typeof(ETGBookingApplicationPage),
    slug: "booking-price",
    uiPageType: typeof(ETG.Admin.Features.ETGBooking.BookingPrice.BookingPricesListPage),
    name: "Booking Price",
    templateName: TemplateNames.LISTING,
    order: 1)]

[assembly: UIPage(
    parentType: typeof(ETG.Admin.Features.ETGBooking.BookingPrice.BookingPricesListPage),
    slug: "create",
    uiPageType: typeof(ETG.Admin.Features.ETGBooking.BookingPrice.BookingPriceCreatePage),
    name: "New Booking Price",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

[assembly: UIPage(
    parentType: typeof(ETG.Admin.Features.ETGBooking.BookingPrice.BookingPricesListPage),
    slug: PageParameterConstants.PARAMETERIZED_SLUG,
    uiPageType: typeof(ETG.Admin.Features.ETGBooking.BookingPrice.BookingPriceEditSection),
    name: "Booking Price",
    templateName: TemplateNames.SECTION_LAYOUT,
    order: UIPageOrder.NoOrder)]

[assembly: UIPage(
    parentType: typeof(ETG.Admin.Features.ETGBooking.BookingPrice.BookingPriceEditSection),
    slug: "edit",
    uiPageType: typeof(ETG.Admin.Features.ETGBooking.BookingPrice.BookingPriceEditPage),
    name: "Edit Booking Price",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace ETG.Admin.Features.ETGBooking.BookingPrice
{
    [UIEvaluatePermission(SystemPermissions.VIEW)]
    public class BookingPricesListPage : ListingPage
    {
        protected override string ObjectType => BookingPriceInfo.OBJECT_TYPE;

        public override Task ConfigurePage()
        {
            PageConfiguration.HeaderActions
                .AddLink<BookingPriceCreatePage>("New Booking Price");

            PageConfiguration.ColumnConfigurations
                .AddColumn(
                    nameof(BookingPriceInfo.BookingPriceTourCode),
                    "Tour Code",
                    visible: true,
                    minWidth: 40,
                    maxWidth: 40,
                    searchable: true,
                    localizable: false,
                    sortable: true)
                .AddColumn(
                    nameof(BookingPriceInfo.BookingPriceStartDate),
                    "Start Date",
                    visible: true,
                    minWidth: 50,
                    maxWidth: 50,
                    searchable: false,
                    localizable: false,
                    sortable: true)
                .AddColumn(
                    nameof(BookingPriceInfo.BookingPriceEndDate),
                    "End Date",
                    visible: true,
                    minWidth: 50,
                    maxWidth: 50,
                    searchable: false,
                    localizable: false,
                    sortable: true)
                .AddColumn(
                    nameof(BookingPriceInfo.BookingPriceTwinSharePrice),
                    "Twin Share Price",
                    visible: true,
                    minWidth: 50,
                    maxWidth: 50,
                    searchable: false,
                    localizable: false,
                    sortable: true)
                .AddColumn(
                    nameof(BookingPriceInfo.IsEnabled),
                    "Enabled",
                    visible: true,
                    minWidth: 50,
                    maxWidth: 50,
                    searchable: false,
                    localizable: false,
                    sortable: true);

            PageConfiguration.AddEditRowAction<BookingPriceEditSection>();

            PageConfiguration.TableActions
                .AddDeleteAction(nameof(Delete));

            PageConfiguration.QueryModifiers.AddModifier(
                (query, _) => query.Columns(
                    nameof(BookingPriceInfo.BookingPriceID),
                    nameof(BookingPriceInfo.BookingPriceTourCode),
                    nameof(BookingPriceInfo.BookingPriceStartDate),
                    nameof(BookingPriceInfo.BookingPriceEndDate),
                    nameof(BookingPriceInfo.BookingPriceTwinSharePrice),
                    nameof(BookingPriceInfo.IsEnabled)));

            return base.ConfigurePage();
        }

        [PageCommand(Permission = SystemPermissions.DELETE)]
        public override Task<ICommandResponse<RowActionResult>> Delete(int id)
            => base.Delete(id);
    }

    [UIEvaluatePermission(SystemPermissions.CREATE)]
    public class BookingPriceCreatePage(
        IFormComponentMapper formComponentMapper,
        IFormDataBinder formDataBinder,
        IPageLinkGenerator pageLinkGenerator)
        : CreatePage<BookingPriceInfo, BookingPriceEditSection>(
            formComponentMapper,
            formDataBinder,
            pageLinkGenerator)
    {
        public override Task ConfigurePage()
        {
            PageConfiguration.UIFormName = "BookingPriceCreate";
            return base.ConfigurePage();
        }
    }

    public class BookingPriceEditSection : EditSectionPage<BookingPriceInfo>
    {
    }

    [UIEvaluatePermission(SystemPermissions.UPDATE)]
    public class BookingPriceEditPage(
        IFormComponentMapper formComponentMapper,
        IFormDataBinder formDataBinder)
        : InfoEditPage<BookingPriceInfo>(
            formComponentMapper,
            formDataBinder)
    {
        [PageParameter(
            typeof(IntPageModelBinder),
            typeof(BookingPriceEditSection))]
        public override int ObjectId { get; set; }

        public override Task ConfigurePage()
        {
            PageConfiguration.UIFormName = "BookingPriceCreate";
            return base.ConfigurePage();
        }
    }
}