namespace ETG.Web.Features.TourPackage.Pdf;

public sealed class PdfSettings
{
    public const string SectionName = "Pdf";

    public string LicenseKey { get; set; } = string.Empty;

    public int MarginTop { get; set; } = 20;

    public int MarginBottom { get; set; } = 20;

    public int MarginLeft { get; set; } = 20;

    public int MarginRight { get; set; } = 20;

    public int MinPageLoadTime { get; set; } = 3;
}