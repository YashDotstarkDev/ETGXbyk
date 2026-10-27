using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

namespace ETG.Web.Services.Storage;

public interface IBlobStorageService
{
    /// <summary>
    /// Uploads a CV/document to Azure Blob Storage (or fallback storage) with a timestamped unique name.
    /// Returns the application download URL (e.g. /getfile?path=/etg/jobs/filename_timestamp.pdf).
    /// </summary>
    Task<string> UploadCvAsync(IFormFile file, string folder = "jobs");

    /// <summary>
    /// Retrieves a file stream, content type, and file name from Azure Blob Storage or local storage.
    /// </summary>
    Task<(Stream Stream, string ContentType, string FileName)?> GetFileAsync(string path);

    /// <summary>
    /// Checks if a file exists in Azure Blob Storage or local fallback storage.
    /// Path can be "pdf/EUR001.pdf" or "/etg/pdf/EUR001.pdf".
    /// </summary>
    Task<bool> FileExistsAsync(string relativePath);

    /// <summary>
    /// Saves a stream (e.g. generated Tour PDF) to Azure Blob Storage (or local fallback).
    /// Path can be "pdf/EUR001.pdf" or "/etg/pdf/EUR001.pdf".
    /// </summary>
    Task<string> SaveFileAsync(Stream stream, string relativePath, string contentType = "application/pdf");

    /// <summary>
    /// Opens a read stream for a file from Azure Blob Storage (or local fallback).
    /// </summary>
    Task<Stream?> OpenReadAsync(string relativePath);

    /// <summary>
    /// Returns the UTC last-modified timestamp of a blob, or null if the file does not exist.
    /// </summary>
    Task<DateTimeOffset?> GetLastModifiedAsync(string relativePath);

    /// <summary>
    /// Deletes a file from Azure Blob Storage (or local fallback).
    /// </summary>
    Task DeleteFileAsync(string relativePath);
}
