namespace ETG.Web.Features.TourPackage.Pdf;

public interface IPDFGenerator
{
    Task<MemoryStream> GeneratePdfFromHtmlAsync(
        string html,
        string baseUrl,
        CancellationToken cancellationToken = default);
}
