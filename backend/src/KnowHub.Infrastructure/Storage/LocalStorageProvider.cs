using KnowHub.Application.Contracts.Talent;
using Microsoft.Extensions.Options;

namespace KnowHub.Infrastructure.Storage;

public sealed class LocalStorageProvider : IStorageProvider
{
    private readonly LocalStorageConfig _config;

    public LocalStorageProvider(IOptions<StorageConfiguration> options)
    {
        _config = options.Value.Local;
        Directory.CreateDirectory(_config.UploadPath);
    }

    public string ProviderType => "Local";

    public Task<StorageDownloadResult> DownloadFileAsync(StorageFileReference fileRef, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_config.UploadPath, fileRef.FileId);
        var stream = File.OpenRead(fullPath);
        var contentType = GetContentType(Path.GetExtension(fileRef.FileName));
        return Task.FromResult(new StorageDownloadResult(stream, contentType, fileRef.FileName));
    }

    public Task<IEnumerable<StorageFileItem>> ListFilesAsync(string path, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_config.UploadPath, path);
        if (!Directory.Exists(fullPath))
            return Task.FromResult(Enumerable.Empty<StorageFileItem>());

        var items = Directory.EnumerateFiles(fullPath, "*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
                     || f.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            .Select(f => new FileInfo(f))
            .Select(fi => new StorageFileItem(
                FileId: Path.GetRelativePath(_config.UploadPath, fi.FullName),
                FileName: fi.Name,
                SizeBytes: fi.Length,
                MimeType: GetContentType(fi.Extension),
                LastModified: fi.LastWriteTimeUtc
            ));

        return Task.FromResult(items);
    }

    private static string GetContentType(string extension) => extension.ToLowerInvariant() switch
    {
        ".pdf"  => "application/pdf",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        _       => "application/octet-stream"
    };
}
