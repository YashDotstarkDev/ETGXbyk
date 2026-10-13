using CMS.Membership;
using ETG.Admin.Features.ETGBooking;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(ETGBookingApplicationPage),
    slug: "import-booking-prices",
    uiPageType: typeof(ETG.Admin.Features.ETGBooking.ImportBookingPrices.ImportBookingPricesPage),
    name: "Import Booking Prices",
    templateName: "@website/web-admin/ImportBookingPrices",
    order: 2)]

namespace ETG.Admin.Features.ETGBooking.ImportBookingPrices;

[UIEvaluatePermission(SystemPermissions.UPDATE)]
public class ImportBookingPricesPage(
    BookingPriceImportService importService) : Page
{
    [PageCommand(Permission = SystemPermissions.UPDATE)]
    public async Task<ICommandResponse<BookingPriceImportResult>> ValidateCsv(
        BookingPriceImportCommandArguments arguments)
    {
        var result = await importService.ValidateAsync(arguments.CsvText);
        return ResponseFrom(result);
    }

    [PageCommand(Permission = SystemPermissions.UPDATE)]
    public async Task<ICommandResponse<BookingPriceImportResult>> ImportCsv(
        BookingPriceImportCommandArguments arguments)
    {
        var result = await importService.ImportAsync(arguments.CsvText);
        return ResponseFrom(result);
    }
}