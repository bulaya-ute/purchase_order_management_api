using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PurchaseOrderManagement.Api.Dtos.Files;
using PurchaseOrderManagement.Api.Enums;
using PurchaseOrderManagement.Api.Services;

namespace PurchaseOrderManagement.Api.Controllers;

// Generic file attachment with API-side URL resolution (docs/02, docs/05). Authz: reads = any
// authenticated user; uploads = any authenticated user (flagged assumption pending Q7).
[ApiController]
[Route("api/files")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly IFileService _fileService;

    public FilesController(IFileService fileService)
    {
        _fileService = fileService;
    }

    [HttpPost]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<ActionResult<FileDto>> Upload([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        var created = await _fileService.UploadAsync(file, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await _fileService.GetContentAsync(id, cancellationToken);
        if (result is null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found", detail: $"File {id} was not found.");
        }

        if (result.SourceType == FileSourceType.Url)
        {
            return Redirect(result.RedirectUrl!);
        }

        return File(result.Content!, result.ContentType ?? "application/octet-stream", result.OriginalFileName);
    }
}
