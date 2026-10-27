using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebSupergoo.ABCpdf12;

namespace ETG.Web.Features.TourPackage.Pdf;

public sealed class PDFGenerator : IPDFGenerator
{
    private readonly PdfSettings _settings;
    private readonly ILogger<PDFGenerator> _logger;

    public PDFGenerator(
        IOptions<PdfSettings> options,
        ILogger<PDFGenerator> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _settings = options.Value ?? new PdfSettings();
        _logger = logger;
    }

    public Task<MemoryStream> GeneratePdfFromHtmlAsync(
        string html,
        string baseUrl,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(html))
            throw new ArgumentException("HTML cannot be empty.", nameof(html));

        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Base URL cannot be empty.", nameof(baseUrl));

        return Task.Run(() => GenerateInternal(html, baseUrl, cancellationToken), cancellationToken);
    }

    private MemoryStream GenerateInternal(string html, string baseUrl, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation("Starting PDF generation. BaseUrl: {BaseUrl}", baseUrl);

        if (!string.IsNullOrWhiteSpace(_settings.LicenseKey))
        {
            XSettings.InstallLicense(_settings.LicenseKey);
        }
        else
        {
            _logger.LogWarning("ABCpdf license key is not configured.");
        }

        var doc = new Doc();

        try
        {

            doc.MediaBox.String = "A4";
            doc.Rect.String = doc.MediaBox.String;
            doc.Rect.Inset(_settings.MarginLeft, _settings.MarginTop);

            doc.HtmlOptions.Engine = EngineType.Chrome86;
            doc.HtmlOptions.Media = MediaType.Print;
            doc.HtmlOptions.BrowserWidth = 1200;
            doc.HtmlOptions.Timeout = Math.Max(_settings.MinPageLoadTime * 1000, 60000);
            doc.HtmlOptions.AddLinks = true;

            cancellationToken.ThrowIfCancellationRequested();

            int pageId = doc.AddImageHtml(html, true, 0, true);

            while (doc.Chainable(pageId))
            {
                cancellationToken.ThrowIfCancellationRequested();
                doc.Page = doc.AddPage();
                doc.Rect.String = doc.MediaBox.String;
                doc.Rect.Inset(_settings.MarginLeft, _settings.MarginTop);
                pageId = doc.AddImageToChain(pageId);
            }

            for (int i = 1; i <= doc.PageCount; i++)
            {
                doc.PageNumber = i;
                doc.Flatten();
            }

            cancellationToken.ThrowIfCancellationRequested();

            var stream = new MemoryStream();
            doc.Save(stream);
            stream.Position = 0;

            _logger.LogInformation(
                "PDF generated. Pages={Pages}, Size={Size}bytes", doc.PageCount, stream.Length);

            return stream;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("PDF generation cancelled.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF from HTML.");
            throw;
        }
        finally
        {
            doc.Clear();
        }
    }
}
