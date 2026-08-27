using Azure;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using AzureArchitect.Services.BlobStorage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AzureAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StorageController : ControllerBase
    {
        private readonly IBlobService _blobService;
        private readonly ILogger<StorageController> _logger;

        public StorageController(IBlobService blobService, ILogger<StorageController> logger)
        {
            _blobService = blobService;
            _logger = logger;
        }

        [HttpPost("Upload")]
        public async Task<IActionResult> Upload([FromForm] string containerName, [FromForm] string blobName, IFormFile file, bool uniqueBlobName = true)
        {
            if (file == null) return BadRequest("File is required.");
            if (string.IsNullOrWhiteSpace(containerName)) return BadRequest("containerName is required.");

            try
            {
                var name = string.IsNullOrWhiteSpace(blobName) ? file.FileName : blobName;
                await using var stream = file.OpenReadStream();
                await _blobService.UploadAsync(containerName, name, stream, uniqueBlobName);
                return Accepted();
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Upload failed");
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPost("UploadMultiple")]
        public async Task<IActionResult> UploadMultiple(string containerName, List<IFormFile> files)
        {
            if (files == null || files.Count == 0) return BadRequest("At least one file is required.");
            if (string.IsNullOrWhiteSpace(containerName)) return BadRequest("containerName is required.");

            foreach (var file in files)
            {
                try
                {
                    await using var stream = file.OpenReadStream();
                    await _blobService.UploadAsync(containerName, file.FileName, stream);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "UploadMultiple: failed for {File}", file.FileName);
                }
            }

            return Accepted();
        }

        [HttpGet("Download")]
        public async Task<IActionResult> Download(string containerName, string blobName)
        {
            if (string.IsNullOrWhiteSpace(containerName) || string.IsNullOrWhiteSpace(blobName))
                return BadRequest("containerName and blobName are required.");

            try
            {
                var props = await _blobService.GetPropertiesAsync(containerName, blobName);
                var stream = await _blobService.DownloadAsync(containerName, blobName);
                var contentType = props.ContentType ?? "application/octet-stream";
                return File(stream, contentType, blobName);
            }
            catch (FileNotFoundException)
            {
                return NotFound(new { Error = "Blob not found." });
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Download failed");
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpGet("GenerateSas")]
        public async Task<IActionResult> GenerateSas(string containerName, string blobName, int minutes = 15)
        {
            if (string.IsNullOrWhiteSpace(containerName) || string.IsNullOrWhiteSpace(blobName)) return BadRequest("containerName and blobName are required.");
            if (minutes <= 0) return BadRequest("minutes must be positive.");

            try
            {
                var uri = await _blobService.GenerateBlobSasUri(containerName, blobName, TimeSpan.FromMinutes(minutes));
                return Ok(new { SasUri = uri.ToString() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GenerateSas failed");
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpGet("GenerateUserDelegationSas")]
        public async Task<IActionResult> GenerateUserDelegationSas(string containerName, string blobName, int minutes = 15)
        {
            if (string.IsNullOrWhiteSpace(containerName) || string.IsNullOrWhiteSpace(blobName)) return BadRequest("containerName and blobName are required.");
            if (minutes <= 0) return BadRequest("minutes must be positive.");

            try
            {
                var uri = await _blobService.GenerateUserDelegationSasUriAsync(containerName, blobName, TimeSpan.FromMinutes(minutes));
                return Ok(new { SasUri = uri.ToString() });
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "GenerateUserDelegationSas failed");
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpGet("ListBlobs")]
        public async Task<IActionResult> ListBlobs(string containerName, string? prefix = null)
        {
            if (string.IsNullOrWhiteSpace(containerName)) return BadRequest("containerName is required.");

            try
            {
                var items = await _blobService.ListBlobsAsync(containerName, prefix);
                // Return lightweight projection
                var list = items.Select(i => new { Name = i.Name, Size = i.Properties.ContentLength, ContentType = i.Properties.ContentType });
                return Ok(list);
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "ListBlobs failed");
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpDelete("Delete")]
        public async Task<IActionResult> Delete(string containerName, string blobName)
        {
            if (string.IsNullOrWhiteSpace(containerName) || string.IsNullOrWhiteSpace(blobName)) return BadRequest("containerName and blobName are required.");

            try
            {
                await _blobService.DeleteAsync(containerName, blobName);
                return NoContent();
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Delete failed");
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpGet("Properties")]
        public async Task<IActionResult> Properties(string containerName, string blobName)
        {
            if (string.IsNullOrWhiteSpace(containerName) || string.IsNullOrWhiteSpace(blobName)) return BadRequest("containerName and blobName are required.");

            try
            {
                var props = await _blobService.GetPropertiesAsync(containerName, blobName);
                var result = new
                {
                    props.ContentLength,
                    props.ContentType,
                    props.ContentHash,
                    props.LastModified,
                    Metadata = props.Metadata
                };
                return Ok(result);
            }
            catch (FileNotFoundException)
            {
                return NotFound(new { Error = "Blob not found." });
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Properties failed");
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpGet("Metadata")]
        public async Task<IActionResult> Metadata(string containerName, string blobName)
        {
            if (string.IsNullOrWhiteSpace(containerName) || string.IsNullOrWhiteSpace(blobName)) return BadRequest("containerName and blobName are required.");

            try
            {
                var md = await _blobService.GetMetadataAsync(containerName, blobName);
                return Ok(md);
            }
            catch (FileNotFoundException)
            {
                return NotFound(new { Error = "Blob not found." });
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Metadata failed");
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPost("SetMetadata")]
        public async Task<IActionResult> SetMetadata(string containerName, string blobName, [FromBody] IDictionary<string, string> metadata)
        {
            if (string.IsNullOrWhiteSpace(containerName) || string.IsNullOrWhiteSpace(blobName)) return BadRequest("containerName and blobName are required.");
            if (metadata == null) return BadRequest("metadata is required.");

            try
            {
                await _blobService.SetMetadataAsync(containerName, blobName, metadata);
                return Accepted();
            }
            catch (FileNotFoundException)
            {
                return NotFound(new { Error = "Blob not found." });
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "SetMetadata failed");
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPost("StartCopyFromUrl")]
        public async Task<IActionResult> StartCopyFromUrl(string containerName, string blobName, [FromBody] Uri sourceUrl)
        {
            if (string.IsNullOrWhiteSpace(containerName) || string.IsNullOrWhiteSpace(blobName)) return BadRequest("containerName and blobName are required.");
            if (sourceUrl == null) return BadRequest("sourceUrl is required.");

            try
            {
                var copyId = await _blobService.StartCopyFromUriAsync(containerName, blobName, sourceUrl);
                return Accepted(new { CopyId = copyId });
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "StartCopyFromUrl failed");
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}
