namespace gymappAPI.Configuration;

/// <summary>The "AWS" section, used for profile pictures in S3.</summary>
public class AwsSettings
{
    public const string SectionName = "AWS";

    public string Region { get; set; } = "eu-central-1";
    public string BucketName { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
}
