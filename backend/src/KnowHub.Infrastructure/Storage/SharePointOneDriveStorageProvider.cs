using System.Net.Http.Headers;
using KnowHub.Application.Contracts.Talent;
using Microsoft.Graph;
using Microsoft.Extensions.Options;

namespace KnowHub.Infrastructure.Storage;

public sealed class SharePointOneDriveStorageProvider : IStorageProvider
{
    private readonly MicrosoftGraphConfig _config;
    private readonly TalentModuleConfiguration _talentConfig;

    public SharePointOneDriveStorageProvider(
        IOptions<StorageConfiguration> storageOptions,
        IOptions<TalentModuleConfiguration> talentOptions)
    {
        _config = storageOptions.Value.MicrosoftGraph;
        _talentConfig = talentOptions.Value;
    }

    public string ProviderType => "OneDrive";

    public async Task<StorageDownloadResult> DownloadFileAsync(StorageFileReference fileRef, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(fileRef.AccessToken);

        ValidateDomain(fileRef.ContainerOrDrive);

        // FileId format: "{driveId}:{itemId}"
        var separatorIndex = fileRef.FileId.IndexOf(':', StringComparison.Ordinal);
        if (separatorIndex < 0)
            throw new ArgumentException("FileId must be in '{driveId}:{itemId}' format.", nameof(fileRef));

        var driveId = fileRef.FileId[..separatorIndex];
        var itemId  = fileRef.FileId[(separatorIndex + 1)..];

        var graphClient = CreateGraphClient(fileRef.AccessToken);
        var stream = await graphClient.Drives[driveId].Items[itemId].Content.GetAsync(cancellationToken: ct);

        if (stream is null)
            throw new InvalidOperationException($"Graph API returned no content for item '{itemId}'.");

        var contentType = GetContentType(Path.GetExtension(fileRef.FileName));
        return new StorageDownloadResult(stream, contentType, fileRef.FileName);
    }

    /// <summary>Listing is not supported; selection is handled by the File Picker v8 in the browser.</summary>
    public Task<IEnumerable<StorageFileItem>> ListFilesAsync(string path, CancellationToken ct = default)
        => Task.FromResult(Enumerable.Empty<StorageFileItem>());

    private void ValidateDomain(string? endpoint)
    {
        if (_talentConfig.AllowedSharePointDomains.Length == 0)
            return;

        if (string.IsNullOrEmpty(endpoint))
            throw new InvalidOperationException("SharePoint endpoint domain is required.");

        var domain = new Uri(endpoint).Host;
        if (!_talentConfig.AllowedSharePointDomains.Any(d => d.Equals(domain, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"SharePoint domain '{domain}' is not in the allowed list.");
    }

    private static GraphServiceClient CreateGraphClient(string accessToken)
    {
        var httpClient = GraphClientFactory.Create();
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
        return new GraphServiceClient(httpClient);
    }

    private static string GetContentType(string extension) => extension.ToLowerInvariant() switch
    {
        ".pdf"  => "application/pdf",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        _       => "application/octet-stream"
    };
}
