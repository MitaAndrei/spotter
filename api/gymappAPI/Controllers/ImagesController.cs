using gymappAPI.DTOs;
using gymappAPI.Managers;
using gymappAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gymappAPI.Controllers;

// Profile pictures in S3.
[ApiController]
[Authorize]
[Route("api/images")]
public class ImagesController : ControllerBase
{
    private const long MaxFileSize = 5 * 1024 * 1024;
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png"];

    private readonly IS3Service _s3Service;
    private readonly IUserManager _userManager;
    private readonly ILogger<ImagesController> _logger;

    public ImagesController(IS3Service s3Service, IUserManager userManager, ILogger<ImagesController> logger)
    {
        _s3Service = s3Service;
        _userManager = userManager;
        _logger = logger;
    }

    // Uploads a new profile picture for the current user and deletes the previous one.
    [HttpPost]
    public async Task<IActionResult> Upload([FromForm] FileUploadDto uploadDto)
    {
        var file = uploadDto.File;
        if (file.Length == 0)
            return BadRequest(new { message = "No file uploaded." });

        if (!AllowedExtensions.Contains(Path.GetExtension(file.FileName).ToLowerInvariant()))
            return BadRequest(new { message = "Only JPG and PNG images are allowed." });

        if (file.Length > MaxFileSize)
            return BadRequest(new { message = "The image must be smaller than 5 MB." });

        var fileKey = await _s3Service.UploadFileAsync(file);

        var user = await _userManager.GetCurrentUserAsync();
        var previousKey = user.ProfileImageKey;
        await _userManager.UpdateProfileImageAsync(user, fileKey);

        // The new picture is saved; failing to remove the old one shouldn't fail the upload.
        if (!string.IsNullOrEmpty(previousKey))
        {
            try
            {
                await _s3Service.DeleteFileAsync(previousKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not delete previous profile image {Key}", previousKey);
            }
        }

        return StatusCode(StatusCodes.Status201Created, new { fileKey });
    }

    [HttpGet("{fileKey}/url")]
    public async Task<IActionResult> GetUrl(string fileKey)
    {
        var url = await _s3Service.GetPreSignedUrlAsync(fileKey);
        return Ok(new { url });
    }

    // Only your own current profile picture can be deleted.
    [HttpDelete("{fileKey}")]
    public async Task<IActionResult> Delete(string fileKey)
    {
        var user = await _userManager.GetCurrentUserAsync();
        if (user.ProfileImageKey != fileKey)
            return Forbid();

        await _s3Service.DeleteFileAsync(fileKey);
        await _userManager.UpdateProfileImageAsync(user, null);
        return NoContent();
    }
}
