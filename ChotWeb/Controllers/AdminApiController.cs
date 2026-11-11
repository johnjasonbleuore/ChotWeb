using ChotWeb.Models;
using ChotWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChotWeb.Controllers
{
    /// <summary>JSON endpoints used by the designer and media library (wwwroot/admin/*.js).</summary>
    [Authorize]
    [ApiController]
    [Route("admin/api")]
    [AutoValidateAntiforgeryToken]
    public class AdminApiController(SiteContentStore store, MediaStore media, ILogger<AdminApiController> logger) : ControllerBase
    {
        [HttpGet("draft")]
        public IActionResult GetDraft()
        {
            return Ok(State(includeContent: true));
        }

        [HttpPut("draft")]
        public IActionResult SaveDraft([FromBody] SiteContent content)
        {
            store.SaveDraft(content);
            return Ok(State(includeContent: false));
        }

        [HttpPost("publish")]
        public IActionResult Publish()
        {
            store.Publish();
            return Ok(State(includeContent: false));
        }

        [HttpPost("discard")]
        public IActionResult Discard()
        {
            store.DiscardDraft();
            return Ok(State(includeContent: true));
        }

        [HttpPost("reset")]
        public IActionResult Reset()
        {
            store.ResetDraftToDefaults();
            return Ok(State(includeContent: true));
        }

        [HttpGet("media")]
        public IActionResult ListMedia(string? kind = null)
        {
            return Ok(media.List(kind));
        }

        [HttpPost("media")]
        [RequestSizeLimit(MediaStore.MaxVideoBytes + (1024 * 1024))]
        [RequestFormLimits(MultipartBodyLengthLimit = MediaStore.MaxVideoBytes + (1024 * 1024))]
        public async Task<IActionResult> Upload(IFormFile? file, CancellationToken cancellationToken)
        {
            if (file is null)
            {
                return BadRequest(new { error = "Choose a file to upload." });
            }

            try
            {
                MediaFile saved = await media.SaveAsync(file, cancellationToken);
                logger.LogInformation("Uploaded {Name} ({Size} bytes) as {Stored}.", file.FileName, file.Length, saved.Name);
                return Ok(saved);
            }
            catch (MediaException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("media/{name}")]
        public IActionResult DeleteMedia(string name)
        {
            return media.Delete(name) ? NoContent() : NotFound(new { error = "File not found." });
        }

        private object State(bool includeContent)
        {
            return new
            {
                content = includeContent ? store.GetDraft() : null,
                hasUnpublishedChanges = store.HasUnpublishedChanges,
                draftSavedAt = store.DraftSavedAt,
                publishedAt = store.PublishedAt,
            };
        }
    }
}
