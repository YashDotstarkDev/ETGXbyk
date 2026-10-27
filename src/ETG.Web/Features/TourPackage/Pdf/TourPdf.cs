using System;
using System.IO;

namespace ETG.Web.Features.TourPackage.Pdf;

public sealed class TourPdf
{
    private readonly Stream? _stream;

    public TourPdf(
        string filePath,
        string tourCode)
    {
        FilePath = filePath;
        TourCode = tourCode;
    }

    public TourPdf(
        Stream stream,
        string tourCode)
    {
        _stream = stream;
        TourCode = tourCode;
        FilePath = string.Empty;
    }

    public string FilePath { get; }

    public string TourCode { get; }

    public bool Exists =>
        _stream != null ||
        (!string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath));

    public Stream OpenRead()
    {
        if (_stream != null)
        {
            if (_stream.CanSeek)
            {
                _stream.Position = 0;
            }
            return _stream;
        }

        if (!File.Exists(FilePath))
        {
            throw new FileNotFoundException(
                "Generated tour PDF was not found.",
                FilePath);
        }

        return new FileStream(
            FilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);
    }
}