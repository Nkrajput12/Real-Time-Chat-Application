using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using ConnectHub.Media.API.Service.Interface;

namespace ConnectHub.Media.API.Controllers
{
    [ApiController]
    [Route("api/media")]
    [Authorize]
    public class MediaController : ControllerBase
    {
        private readonly IMediaService _mediaService;

        public MediaController(IMediaService mediaService)
        {
            _mediaService = mediaService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] int? roomId = null, [FromForm] int? messageId = null)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var result = await _mediaService.UploadFileAsync(file, userId, roomId, messageId);
            
            if (result == null) return BadRequest("Upload failed.");
            
            return Ok(result);
        }

        [HttpGet("{fileId}/sas-url")]
        public async Task<IActionResult> GetSasUrl(Guid fileId)
        {
            var url = await _mediaService.GenerateSasUrlAsync(fileId);
            if (url == null) return NotFound();
            
            return Ok(new { SasUrl = url });
        }

        [HttpDelete("{fileId}")]
        public async Task<IActionResult> Delete(Guid fileId)
        {
            var success = await _mediaService.DeleteFileAsync(fileId);
            if (!success) return NotFound();
            
            return NoContent();
        }
    }
}
