using ETG.Web.Services.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace ETG.Web.Controllers;

[Route("getfile")]
public class FileController : Controller
{
    private readonly IBlobStorageService _storageService;
    private readonly ILogger<FileController> _logger;

    public FileController(IBlobStorageService storageService, ILogger<FileController> logger)
    {
        _storageService = storageService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] string? path, [FromQuery] string? name)
    {
        var target = !string.IsNullOrWhiteSpace(path) ? path : name;
        if (string.IsNullOrWhiteSpace(target))
        {
            _logger.LogWarning("File download attempted with missing path");
            return NotFound("File path is missing.");
        }

        // SECURITY FIX: Log download attempts for audit trail
        _logger.LogInformation(
            "User {User} requesting file: {Path}",
            User.Identity?.Name ?? "Unknown",
            target);

        var fileResult = await _storageService.GetFileAsync(target);
        if (fileResult == null)
        {
            _logger.LogWarning("Requested file was not found: {Path}", target);
            return NotFound("File not found.");
        }

        // Inline disposition so browser opens PDF directly in a new tab for HR/Admin
        Response.Headers.Append("Content-Disposition", $"inline; filename=\"{fileResult.Value.FileName}\"");
        return File(fileResult.Value.Stream, fileResult.Value.ContentType);
    }
}
