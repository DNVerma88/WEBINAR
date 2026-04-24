using Azure.Storage.Blobs;
using KnowHub.Application.Contracts.Talent;
using Microsoft.Extensions.Options;

namespace KnowHub.Infrastructure.Storage;

public sealed class AzureBlobStorageProvider : IStorageProvider
{
    private readonly AzureBlobConfig _config;

    public AzureBlobStorageProvider(IOptions<StorageConfiguration> options)
    {
        _config = options.Value.AzureBlob;
    }

    public string ProviderType => "AzureBlob";

    public async Task<StorageDownloadResult> DownloadFileAsync(StorageFileReference fileRef, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_config.ConnectionString))
            throw new InvalidOperationException("Azure Blob Storage is not configured.");

        var containerClient = new BlobContainerClient(_config.ConnectionString, _config.ContainerName);
        var blobClient = containerClient.GetBlobClient(fileRef.FileId);
        var response = await blobClient.DownloadStreamingAsync(cancellationToken: ct);
        var contentType = GetContentType(Path.GetExtension(fileRef.FileName));
        return new StorageDownloadResult(response.Value.Content, contentType, fileRef.FileName);
    }

    public async Task<IEnumerable<StorageFileItem>> ListFilesAsync(string path, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_config.ConnectionString))
            return Enumerable.Empty<StorageFileItem>();

        var containerClient = new BlobContainerClient(_config.ConnectionString, _config.ContainerName);
        var items = new List<StorageFileItem>();

        await foreach (var blob in containerClient.GetBlobsAsync(prefix: path, cancellationToken: ct))
        {
            var ext = Path.GetExtension(blob.Name).ToLowerInvariant();
            if (ext != ".pdf" && ext != ".docx") continue;

            items.Add(new StorageFileItem(
                FileId: blob.Name,
                FileName: Path.GetFileName(blob.Name),
                SizeBytes: blob.Properties.ContentLength ?? 0,
                MimeType: GetContentType(ext),
                LastModified: blob.Properties.LastModified ?? DateTimeOffset.UtcNow
            ));
        }

        return items;
    }

    private static string GetContentType(string extension) => extension.ToLowerInvariant() switch
    {
        ".pdf"  => "application/pdf",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        _       => "application/octet-stream"
    };
}
