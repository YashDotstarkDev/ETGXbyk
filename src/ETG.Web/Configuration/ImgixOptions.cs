namespace ETG.Web.Configuration;

/// <summary>
/// Configuration options for Imgix CDN integration.
/// </summary>
public sealed class ImgixOptions
{
    public const string SectionName = "Imgix";

    public string Domain { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;
}

