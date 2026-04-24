using Amazon.S3;
using Amazon.S3.Model;
using KnowHub.Application.Contracts.Talent;
using Microsoft.Extensions.Options;

namespace KnowHub.Infrastructure.Storage;

public sealed class AwsS3StorageProvider : IStorageProvider
{
    private readonly IAmazonS3 _s3Client;
    private readonly AwsS3Config _config;

    public AwsS3StorageProvider(IOptions<StorageConfiguration> options, IAmazonS3 s3Client)
    {
        _config = options.Value.AwsS3;
        _s3Client = s3Client;
    }

    public string ProviderType => "S3";

    public async Task<StorageDownloadResult> DownloadFileAsync(StorageFileReference fileRef, CancellationToken ct = default)
    {
        var response = await _s3Client.GetObjectAsync(_config.BucketName, fileRef.FileId, ct);
        var contentType = GetContentType(Path.GetExtension(fileRef.FileName));
        return new StorageDownloadResult(response.ResponseStream, contentType, fileRef.FileName);
    }

    public async Task<IEnumerable<StorageFileItem>> ListFilesAsync(string path, CancellationToken ct = default)
    {
        var request = new ListObjectsV2Request
        {
            BucketName = _config.BucketName,
            Prefix = path
        };

        var response = await _s3Client.ListObjectsV2Async(request, ct);

        return response.S3Objects
            .Where(o =>
            {
                var ext = Path.GetExtension(o.Key).ToLowerInvariant();
                return ext == ".pdf" || ext == ".docx";
            })
            .Select(o => new StorageFileItem(
                FileId: o.Key,
                FileName: Path.GetFileName(o.Key),
                SizeBytes: o.Size,
                MimeType: GetContentType(Path.GetExtension(o.Key)),
                LastModified: o.LastModified
            ));
    }

    private static string GetContentType(string extension) => extension.ToLowerInvariant() switch
    {
        ".pdf"  => "application/pdf",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        _       => "application/octet-stream"
    };
}
