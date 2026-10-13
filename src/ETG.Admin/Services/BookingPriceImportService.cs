using CMS.ContentEngine;
using CMS.Core;
using CMS.DataEngine;
using CMS.Websites;
using ETG.Booking.Pricing.Classes.Info;
using ETG.Core;
using ETG.Core.Content;
using ETG.Core.Operations;
using Microsoft.VisualBasic.FileIO;
using System.Globalization;

namespace ETG.Admin.Features.ETGBooking.ImportBookingPrices;

public sealed class BookingPriceImportResult
{
    public bool Success { get; set; }

    public int RowCount { get; set; }

    public int ImportedCount { get; set; }

    /// <summary>Blank rows and XXXXX placeholder rows that were ignored.</summary>
    public int SkippedCount { get; set; }

    public List<string> Errors { get; set; } = [];
}

public sealed class BookingPriceImportCommandArguments
{
    public string CsvText { get; set; } = string.Empty;
}

public sealed class BookingPriceImportService(
    IInfoProvider<BookingPriceInfo> bookingPriceProvider,
    IContentQueryExecutor contentQueryExecutor,
    IContentQueryExecutionOptionsCreator queryOptionsCreator,
    IEventLogService eventLogService)
{
    // Confirmed in your XbyK channel settings.
    private const string WebsiteChannelCodeName = "ETG";

    // Used in the Event Log so failed imports are easy to find.
    private const string EventLogSource = "ETGBookingImport";

    // Keeps the error message readable when many codes are unknown.
    private const int MaxCodesInMessage = 25;

    // Placeholder rows used in the client's CSV (the old Kentico 13 module skipped them too).
    private const string PlaceholderPackageCode = "XXXXX";

    private static readonly CultureInfo ImportCulture = CultureInfo.GetCultureInfo("en-AU");

    private sealed record ImportRow(
        string PackageCode,
        DateTime StartDate,
        DateTime EndDate,
        double TwinSharePrice);

    private sealed record ParseOutput(
        List<ImportRow> Rows,
        List<string> Errors,
        int SkippedCount);

    public async Task<BookingPriceImportResult> ValidateAsync(string csvText)
    {
        var (result, _) = await ValidateCoreAsync(csvText);

        return result;
    }

    public async Task<BookingPriceImportResult> ImportAsync(string csvText)
    {
        // Validate again on the server before changing any prices (parses the CSV once).
        var (validation, rows) = await ValidateCoreAsync(csvText);

        if (!validation.Success)
        {
            return validation;
        }

        var packageCodes = rows
            .Select(row => row.PackageCode)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        try
        {
            // Load the existing rows BEFORE the transaction starts, so no
            // 'await' happens while the transaction scope is open.
            var existingRows = (await bookingPriceProvider.Get()
                    .WhereIn(
                        nameof(BookingPriceInfo.BookingPriceTourCode),
                        packageCodes)
                    .GetEnumerableTypedResultAsync())
                .ToList();

            // If anything throws before Commit(), everything below is rolled back.
            using var transaction = new CMSTransactionScope();

            // Replace existing prices for the package codes in this CSV.
            foreach (var existing in existingRows)
            {
                existing.Delete();
            }

            foreach (var row in rows)
            {
                var price = new BookingPriceInfo
                {
                    BookingPriceGuid = Guid.NewGuid(),
                    BookingPriceLastModified = DateTime.Now,
                    BookingPriceTourCode = row.PackageCode,
                    BookingPriceStartDate = row.StartDate,
                    BookingPriceEndDate = row.EndDate,
                    BookingPriceTwinSharePrice = row.TwinSharePrice,

                    // This value is not read from the CSV.
                    IsEnabled = true
                };

                price.Insert();
            }

            transaction.Commit();

            return new BookingPriceImportResult
            {
                Success = true,
                RowCount = rows.Count,
                ImportedCount = rows.Count,
                SkippedCount = validation.SkippedCount
            };
        }
        catch (Exception exception)
        {
            eventLogService.LogException(
                EventLogSource,
                "IMPORT",
                exception,
                additionalMessage: $"Import of {rows.Count} rows for {packageCodes.Length} package codes failed and was rolled back.");

            return Failure(
            [
                "Import failed and no prices were changed. Details were written to the Event Log "
                + $"(source: {EventLogSource})."
            ]);
        }
    }

    private async Task<(BookingPriceImportResult Result, List<ImportRow> Rows)> ValidateCoreAsync(
        string csvText)
    {
        var parsed = ParseCsv(csvText);

        if (parsed.Errors.Count > 0)
        {
            return (Failure(parsed.Errors), parsed.Rows);
        }

        if (parsed.Rows.Count == 0)
        {
            return (Failure(["The CSV contains no pricing rows."]), parsed.Rows);
        }

        var packageCodes = parsed.Rows
            .Select(row => row.PackageCode)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var unknownCodes = await FindUnknownTourCodesAsync(packageCodes);

        if (unknownCodes.Count > 0)
        {
            var shown = unknownCodes.Take(MaxCodesInMessage).ToList();
            var hidden = unknownCodes.Count - shown.Count;

            var message = $"Package codes not found on the website: {string.Join(", ", shown)}";

            if (hidden > 0)
            {
                message += $" and {hidden} more";
            }

            return (Failure([message]), parsed.Rows);
        }

        var result = new BookingPriceImportResult
        {
            Success = true,
            RowCount = parsed.Rows.Count,
            SkippedCount = parsed.SkippedCount
        };

        return (result, parsed.Rows);
    }

    private async Task<List<string>> FindUnknownTourCodesAsync(
        List<string> packageCodes)
    {
        var query = new ContentItemQueryBuilder()
            .ForContentType(
                Tour.CONTENT_TYPE_NAME,
                parameters => parameters
                    .ForWebsite(
                        websiteChannelName: WebsiteChannelCodeName,
                        includeUrlPath: false)
                    .Where(where =>
                        where.WhereIn(
                            nameof(Tour.TourCode),
                            packageCodes.ToArray())));

        var tours = await contentQueryExecutor.GetMappedWebPageResult<Tour>(
            query,
            queryOptionsCreator.Create());

        var knownCodes = tours
            .Select(tour => tour.TourCode)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return packageCodes
            .Where(code => !knownCodes.Contains(code))
            .ToList();
    }

    private static ParseOutput ParseCsv(string csvText)
    {
        var rows = new List<ImportRow>();
        var errors = new List<string>();
        var skipped = 0;

        if (string.IsNullOrWhiteSpace(csvText))
        {
            return new ParseOutput(rows, ["Choose a CSV file first."], skipped);
        }

        // Remove a UTF-8 byte order mark if the file has one (Excel "CSV UTF-8" adds it).
        csvText = csvText.TrimStart('\uFEFF');

        using var parser = new TextFieldParser(new StringReader(csvText))
        {
            TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = false
        };

        parser.SetDelimiters(",");

        var csvRowNumber = 0;

        while (!parser.EndOfData)
        {
            string[]? fields;

            try
            {
                fields = parser.ReadFields();
            }
            catch (MalformedLineException)
            {
                errors.Add(
                    $"CSV row near line {parser.ErrorLineNumber} is malformed.");

                break;
            }

            csvRowNumber++;

            if (fields is null || fields.Length == 0)
            {
                continue;
            }

            // Excel often leaves rows like ",,,,," at the end of a file. Ignore them.
            if (fields.All(field => string.IsNullOrWhiteSpace(field)))
            {
                skipped++;
                continue;
            }

            // Skip the header. Map pricing values by column position.
            if (csvRowNumber == 1 && IsHeaderRow(fields))
            {
                continue;
            }

            if (fields.Length < 4)
            {
                errors.Add(
                    $"Row {csvRowNumber}: expected at least four columns (A–D).");

                continue;
            }

            var packageCode = fields[0].Trim();
            var startText = fields[1].Trim();
            var endText = fields[2].Trim();
            var priceText = fields[3].Trim();

            if (string.IsNullOrWhiteSpace(packageCode))
            {
                errors.Add(
                    $"Row {csvRowNumber}: Package Code is required.");

                continue;
            }

            // Placeholder rows are not real packages. Skip them like the old module did.
            if (packageCode.Contains(PlaceholderPackageCode, StringComparison.OrdinalIgnoreCase))
            {
                skipped++;
                continue;
            }

            if (!DateTime.TryParse(
                    startText,
                    ImportCulture,
                    DateTimeStyles.None,
                    out var startDate))
            {
                errors.Add(
                    $"Row {csvRowNumber} ({packageCode}): Start Date is invalid.");

                continue;
            }

            if (!DateTime.TryParse(
                    endText,
                    ImportCulture,
                    DateTimeStyles.None,
                    out var endDate))
            {
                errors.Add(
                    $"Row {csvRowNumber} ({packageCode}): End Date is invalid.");

                continue;
            }

            if (endDate.Date < startDate.Date)
            {
                errors.Add(
                    $"Row {csvRowNumber} ({packageCode}): End Date is before Start Date.");

                continue;
            }

            if (!double.TryParse(
                    priceText,
                    NumberStyles.Currency | NumberStyles.Number,
                    ImportCulture,
                    out var twinSharePrice)
                || twinSharePrice <= 0)
            {
                errors.Add(
                    $"Row {csvRowNumber} ({packageCode}): Twin Share Per Person Price must be greater than zero.");

                continue;
            }

            rows.Add(new ImportRow(
                packageCode,
                startDate,
                endDate,
                twinSharePrice));

            // Only CSV columns A–D are mapped and saved.
        }

        return new ParseOutput(rows, errors, skipped);
    }

    private static bool IsHeaderRow(string[] fields)
    {
        if (fields.Length == 0)
        {
            return false;
        }

        var firstColumn = new string(
            fields[0].Where(char.IsLetterOrDigit).ToArray());

        return firstColumn.Equals(
                   "packagecode",
                   StringComparison.OrdinalIgnoreCase)
            || firstColumn.Equals(
                   "tourcode",
                   StringComparison.OrdinalIgnoreCase)
            || firstColumn.Equals(
                   "package",
                   StringComparison.OrdinalIgnoreCase);
    }

    private static BookingPriceImportResult Failure(
        IEnumerable<string> errors) =>
        new()
        {
            Success = false,
            Errors = errors.ToList()
        };
}