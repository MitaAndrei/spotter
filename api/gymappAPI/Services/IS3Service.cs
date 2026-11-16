namespace gymappAPI.Services;

public interface IS3Service
{
    Task<string> UploadFileAsync(IFormFile file, string? fileName = null);
    Task<bool> DeleteFileAsync(string fileKey);
    Task<string> GetPreSignedUrlAsync(string fileKey, int expirationMinutes = 60);
}