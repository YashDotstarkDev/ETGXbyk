namespace ETG.Web.Features.TourPackage.Pdf;

public interface ITourPdfProvider
{
    Task<TourPdf?> GetTourPdfAsync(
        string tourCode,
        bool forceRegenerate = false,
        CancellationToken cancellationToken = default);
}