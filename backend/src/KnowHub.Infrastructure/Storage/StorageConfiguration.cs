namespace KnowHub.Infrastructure.Storage;

public class StorageConfiguration
{
    public const string SectionName = "Storage";
    public string DefaultProvider { get; set; } = "Local";
    public AzureBlobConfig AzureBlob { get; set; } = new();
    public AwsS3Config AwsS3 { get; set; } = new();
    public MicrosoftGraphConfig MicrosoftGraph { get; set; } = new();
    public LocalStorageConfig Local { get; set; } = new();
}

public class AzureBlobConfig
{
    public string ConnectionString { get; set; } = "";
    public string ContainerName { get; set; } = "resumes";
}

public class AwsS3Config
{
    public string BucketName { get; set; } = "";
    public string Region { get; set; } = "us-east-1";
}

public class MicrosoftGraphConfig
{
    public string TenantId { get; set; } = "";
    public string ClientId { get; set; } = "";
    // ClientSecret: only needed for app-only flow; delegated flow uses per-request tokens
}

public class LocalStorageConfig
{
    public string UploadPath { get; set; } = "uploads/resumes";
}
