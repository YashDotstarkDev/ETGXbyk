using ETG.Web.Features.TourPackage.Pdf;
using Microsoft.AspNetCore.Mvc;

namespace ETG.Web.Features.TourPackage;

[Route("pdf")]
public sealed class TourPdfController(
    ITourPdfProvider tourPdfProvider,
    IWebHostEnvironment environment,
    ILogger<TourPdfController> logger) : Controller
{
    private readonly ITourPdfProvider _tourPdfProvider = tourPdfProvider;
    private readonly IWebHostEnvironment _environment = environment;
    private readonly ILogger<TourPdfController> _logger = logger;

    [HttpGet("{tourCode}")]
    public async Task<IActionResult> GeneratePdf(
        string tourCode,
        [FromQuery] bool force = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tourCode))
            return BadRequest("Tour code is required.");

        try
        {
            tourCode = tourCode.Trim();

            _logger.LogInformation(
                "Tour PDF requested. TourCode: {TourCode}, Force: {Force}", tourCode, force);

            var tourPdf = await _tourPdfProvider.GetTourPdfAsync(
                tourCode, force, cancellationToken);

            if (tourPdf == null || !tourPdf.Exists)
            {
                _logger.LogWarning(
                    "Tour PDF was not generated. TourCode: {TourCode}", tourCode);

                return NotFound($"PDF could not be generated for TourCode '{tourCode}'.");
            }

            var stream = tourPdf.OpenRead();
            Response.Headers.ContentDisposition = $"inline; filename=\"{tourCode}.pdf\"";
            return File(stream, "application/pdf");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Tour PDF request cancelled. TourCode: {TourCode}", tourCode);
            return new EmptyResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serving Tour PDF. TourCode: {TourCode}", tourCode);

            if (_environment.IsDevelopment())
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    $"PDF generation failed: {ex.GetType().Name}: {ex.Message}\n\n{ex.StackTrace}");
            }

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "PDF generation failed. Please try again later.");
        }
    }
}
