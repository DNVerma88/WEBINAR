namespace KnowHub.Application.Contracts.Talent;

public record StorageFileReference(
    string ProviderType,
    string FileId,
    string FileName,
    long FileSizeBytes,
    string? ContainerOrDrive = null,
    string? AccessToken = null
);

public record StorageDownloadResult(Stream Content, string ContentType, string FileName);

public record StorageFileItem(string FileId, string FileName, long SizeBytes, string MimeType, DateTimeOffset LastModified);

public interface IStorageProvider
{
    string ProviderType { get; }
    Task<StorageDownloadResult> DownloadFileAsync(StorageFileReference fileRef, CancellationToken ct = default);
    Task<IEnumerable<StorageFileItem>> ListFilesAsync(string path, CancellationToken ct = default);
}
