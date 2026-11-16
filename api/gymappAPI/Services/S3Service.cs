using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using gymappAPI.Configuration;
using Microsoft.Extensions.Options;

namespace gymappAPI.Services;

// S3 errors are not caught here: they reach the global exception handler, which logs them and returns a 500.
public class S3Service : IS3Service
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public S3Service(IOptions<AwsSettings> options)
    {
        var settings = options.Value;
        _bucketName = settings.BucketName;
        _s3Client = new AmazonS3Client(settings.AccessKey, settings.SecretKey,
            new AmazonS3Config { RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(settings.Region) });
    }

    public async Task<string> UploadFileAsync(IFormFile file, string? fileName = null)
    {
        var fileKey = fileName ?? $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

        await using var stream = file.OpenReadStream();
        await new TransferUtility(_s3Client).UploadAsync(new TransferUtilityUploadRequest
        {
            InputStream = stream,
            Key = fileKey,
            BucketName = _bucketName,
            ContentType = file.ContentType,
            CannedACL = S3CannedACL.Private
        });

        return fileKey;
    }

    public async Task<bool> DeleteFileAsync(string fileKey)
    {
        await _s3Client.DeleteObjectAsync(new DeleteObjectRequest { BucketName = _bucketName, Key = fileKey });
        return true;
    }

    public Task<string> GetPreSignedUrlAsync(string fileKey, int expirationMinutes = 60)
    {
        return Task.FromResult(_s3Client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = fileKey,
            Expires = DateTime.UtcNow.AddMinutes(expirationMinutes)
        }));
    }
}
