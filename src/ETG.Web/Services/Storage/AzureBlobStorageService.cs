using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ETG.Web.Services.Storage;

public class AzureBlobStorageService : IBlobStorageService
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AzureBlobStorageService> _logger;
    private readonly string _accountName;
    private readonly string _sharedKey;
    private readonly string _containerName;
    private readonly string _connectionString;

    public AzureBlobStorageService(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<AzureBlobStorageService> logger)
    {
        _configuration = configuration;
        _environment = environment;
        _logger = logger;

        _accountName = _configuration["CMSAzureAccountName"] ?? string.Empty;
        _sharedKey = _configuration["CMSAzureSharedKey"] ?? string.Empty;
        _containerName = _configuration["CMSAzureContainerName"]
                         ?? _configuration["AzureStorage:ContainerName"]
                         ?? "etgfilescontainer";
        _connectionString = _configuration["AzureStorage:ConnectionString"] ?? string.Empty;
    }

    private BlobContainerClient? GetContainerClient()
    {
        // 1. Primary: Use CMSAzureAccountName + CMSAzureSharedKey (matches K13 unified configuration)
        if (!string.IsNullOrWhiteSpace(_accountName) && !string.IsNullOrWhiteSpace(_sharedKey))
        {
            try
            {
                var serviceUri = new Uri($"https://{_accountName}.blob.core.windows.net");
                var credential = new StorageSharedKeyCredential(_accountName, _sharedKey);
                var serviceClient = new BlobServiceClient(serviceUri, credential);
                return serviceClient.GetBlobContainerClient(_containerName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create BlobContainerClient from CMSAzure credentials.");
            }
        }

        // 2. Fallback: ConnectionString if explicitly provided
        if (!string.IsNullOrWhiteSpace(_connectionString))
        {
            try
            {
                return new BlobContainerClient(_connectionString, _containerName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create BlobContainerClient from ConnectionString.");
            }
        }

        return null;
    }

    public async Task<string> UploadCvAsync(IFormFile file, string folder = "jobs")
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File cannot be empty.", nameof(file));
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var rawFileName = Path.GetFileNameWithoutExtension(file.FileName);
        // Replace spaces or special characters in filename for URL safety
        var safeFileName = string.Concat(rawFileName.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
        var timeStamp = DateTime.UtcNow.ToString("ddMMyyyyHHmmss");
        var uniqueFileName = $"{safeFileName}_{timeStamp}{extension}";

        var blobPath = !string.IsNullOrWhiteSpace(folder)
            ? $"{folder.Trim('/')}/{uniqueFileName}"
            : uniqueFileName;

        var contentType = GetContentType(extension);

        // 1. Try uploading to Azure Blob Storage
        var containerClient = GetContainerClient();
        if (containerClient != null)
        {
            try
            {
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

                var blobClient = containerClient.GetBlobClient(blobPath);
                using (var stream = file.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream, new BlobHttpHeaders
                    {
                        ContentType = contentType
                    });
                }

                _logger.LogInformation("File {FileName} successfully uploaded to Azure Blob container {Container}/{BlobPath}",
                    uniqueFileName, _containerName, blobPath);

                // Return K13 compatible /getfile route path
                return $"/getfile?path=/etg/{blobPath}";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to upload to Azure Blob Storage. Falling back to local disk storage.");
            }
        }

        // 2. Fallback to local storage (e.g. wwwroot/etg/jobs/)
        var localFolder = Path.Combine(_environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "etg", folder);
        if (!Directory.Exists(localFolder))
        {
            Directory.CreateDirectory(localFolder);
        }

        var localFilePath = Path.Combine(localFolder, uniqueFileName);
        using (var localStream = new FileStream(localFilePath, FileMode.Create))
        {
            await file.CopyToAsync(localStream);
        }

        _logger.LogInformation("File {FileName} saved locally to {FilePath}", uniqueFileName, localFilePath);
        return $"/getfile?path=/etg/{blobPath}";
    }

    public async Task<(Stream Stream, string ContentType, string FileName)?> GetFileAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var cleanPath = CleanRelativePath(path);
        var fileName = Path.GetFileName(cleanPath);
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var contentType = GetContentType(extension);

        // 1. Try reading from Azure Blob Storage
        var containerClient = GetContainerClient();
        if (containerClient != null)
        {
            try
            {
                if (await containerClient.ExistsAsync())
                {
                    // Check with folder path e.g. "jobs/file.pdf"
                    var blobClient = containerClient.GetBlobClient(cleanPath);
                    if (!await blobClient.ExistsAsync())
                    {
                        // Fallback check: just fileName
                        blobClient = containerClient.GetBlobClient(fileName);
                    }

                    if (await blobClient.ExistsAsync())
                    {
                        var response = await blobClient.DownloadStreamingAsync();
                        var blobContentType = response.Value.Details.ContentType ?? contentType;
                        return (response.Value.Content, blobContentType, fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve file from Azure Blob Storage: {Path}", cleanPath);
            }
        }

        // 2. Check local disk fallback
        var localFolder = Path.Combine(_environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "etg");
        var localFilePath = Path.Combine(localFolder, cleanPath.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(localFilePath))
        {
            // Also try jobs/fileName
            localFilePath = Path.Combine(localFolder, "jobs", fileName);
        }

        if (File.Exists(localFilePath))
        {
            var stream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return (stream, contentType, fileName);
        }

        return null;
    }

    public async Task<bool> FileExistsAsync(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return false;
        }

        var cleanPath = CleanRelativePath(relativePath);

        // 1. Check Azure Blob Storage if configured
        var containerClient = GetContainerClient();
        if (containerClient != null)
        {
            try
            {
                if (await containerClient.ExistsAsync())
                {
                    var blobClient = containerClient.GetBlobClient(cleanPath);
                    if (await blobClient.ExistsAsync())
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking blob existence in Azure: {Path}", cleanPath);
            }
        }

        // 2. Check local disk fallback
        var localFolder = Path.Combine(_environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "etg");
        var localFilePath = Path.Combine(localFolder, cleanPath.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(localFilePath);
    }

    public async Task<string> SaveFileAsync(Stream stream, string relativePath, string contentType = "application/pdf")
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        var cleanPath = CleanRelativePath(relativePath);

        // 1. Try uploading to Azure Blob Storage if configured
        var containerClient = GetContainerClient();
        if (containerClient != null)
        {
            try
            {
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

                var blobClient = containerClient.GetBlobClient(cleanPath);
                if (stream.CanSeek)
                {
                    stream.Position = 0;
                }

                await blobClient.UploadAsync(stream, new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = contentType
                    }
                });

                _logger.LogInformation("File {Path} successfully uploaded to Azure Blob container {Container}",
                    cleanPath, _containerName);

                return $"/getfile?path=/etg/{cleanPath}";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to upload file to Azure Blob Storage: {Path}. Falling back to local disk storage.", cleanPath);
            }
        }

        // 2. Fallback to local storage (e.g. wwwroot/etg/pdf/EUR001.pdf)
        var localFolder = Path.Combine(_environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "etg");
        var localFilePath = Path.Combine(localFolder, cleanPath.Replace('/', Path.DirectorySeparatorChar));
        var dir = Path.GetDirectoryName(localFilePath);
        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        using (var localStream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await stream.CopyToAsync(localStream);
        }

        _logger.LogInformation("File {Path} saved locally to {FilePath}", cleanPath, localFilePath);
        return $"/getfile?path=/etg/{cleanPath}";
    }

    public async Task<Stream?> OpenReadAsync(string relativePath)
    {
        var file = await GetFileAsync(relativePath);
        return file?.Stream;
    }

    public async Task<DateTimeOffset?> GetLastModifiedAsync(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return null;

        var cleanPath = CleanRelativePath(relativePath);

        var containerClient = GetContainerClient();
        if (containerClient != null)
        {
            try
            {
                if (await containerClient.ExistsAsync())
                {
                    var blobClient = containerClient.GetBlobClient(cleanPath);
                    if (await blobClient.ExistsAsync())
                    {
                        var props = await blobClient.GetPropertiesAsync();
                        return props.Value.LastModified;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error reading blob LastModified: {Path}", cleanPath);
            }
        }

        // Local fallback
        var localFolder = Path.Combine(_environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "etg");
        var localFilePath = Path.Combine(localFolder, cleanPath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(localFilePath))
            return new DateTimeOffset(File.GetLastWriteTimeUtc(localFilePath), TimeSpan.Zero);

        return null;
    }

    public async Task DeleteFileAsync(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return;

        var cleanPath = CleanRelativePath(relativePath);

        var containerClient = GetContainerClient();
        if (containerClient != null)
        {
            try
            {
                if (await containerClient.ExistsAsync())
                {
                    var blobClient = containerClient.GetBlobClient(cleanPath);
                    await blobClient.DeleteIfExistsAsync();
                    _logger.LogInformation("Deleted blob: {Path}", cleanPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error deleting blob: {Path}", cleanPath);
            }
        }

        // Local fallback
        var localFolder = Path.Combine(_environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "etg");
        var localFilePath = Path.Combine(localFolder, cleanPath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(localFilePath))
        {
            File.Delete(localFilePath);
            _logger.LogInformation("Deleted local file: {FilePath}", localFilePath);
        }
    }

    private static string CleanRelativePath(string path)
    {
        var cleanPath = path.Trim();
        if (cleanPath.StartsWith("/getfile?path=", StringComparison.OrdinalIgnoreCase))
        {
            cleanPath = cleanPath["/getfile?path=".Length..];
        }
        else if (cleanPath.StartsWith("/getfile?name=", StringComparison.OrdinalIgnoreCase))
        {
            cleanPath = cleanPath["/getfile?name=".Length..];
        }

        cleanPath = cleanPath.TrimStart('/');
        if (cleanPath.StartsWith("etg/", StringComparison.OrdinalIgnoreCase))
        {
            cleanPath = cleanPath["etg/".Length..];
        }

        return cleanPath;
    }

    private static string GetContentType(string extension) => extension switch
    {
        ".pdf" => "application/pdf",
        ".doc" => "application/msword",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".txt" => "text/plain",
        _ => "application/octet-stream"
    };
}
