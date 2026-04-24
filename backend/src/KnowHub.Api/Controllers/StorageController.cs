using KnowHub.Application.Contracts;
using KnowHub.Application.Contracts.Talent;
using KnowHub.Infrastructure.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace KnowHub.Api.Controllers;

[ApiController]
[Route("api/talent/storage")]
[Authorize]
public class StorageController : ControllerBase
{
    private readonly StorageProviderFactory _factory;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly StorageConfiguration _storageConfig;

    public StorageController(
        StorageProviderFactory factory,
        ICurrentUserAccessor currentUser,
        IOptions<StorageConfiguration> storageConfig)
    {
        _factory = factory;
        _currentUser = currentUser;
        _storageConfig = storageConfig.Value;
    }

    /// <summary>Returns the list of configured storage provider names.</summary>
    [HttpGet("providers")]
    [ProducesResponseType(typeof(string[]), StatusCodes.Status200OK)]
    public IActionResult GetProviders()
    {
        var providers = new List<string> { "Local" };
        if (!string.IsNullOrWhiteSpace(_storageConfig.AzureBlob.ConnectionString))
            providers.Add("AzureBlob");
        providers.Add("S3");
        providers.Add("OneDrive");
        providers.Add("SharePoint");
        return Ok(providers);
    }

    /// <summary>
    /// Lists files in the given storage path.
    /// Restricted to Manager-level or above (HR).
    /// </summary>
    [HttpGet("list")]
    [Authorize(Policy = "ManagerOrAbove")]
    [ProducesResponseType(typeof(IEnumerable<StorageFileItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ListFiles(
        [FromQuery] string provider,
        [FromQuery] string path = "",
        CancellationToken cancellationToken = default)
    {
        // Prevent path traversal
        if (path.Contains("..") || Path.IsPathRooted(path))
            return BadRequest("Invalid path.");

        var storageProvider = _factory.GetProvider(provider);
        var items = await storageProvider.ListFilesAsync(path, cancellationToken);
        return Ok(items);
    }

    /// <summary>
    /// Verifies that a storage file reference is accessible.
    /// Returns { accessible: true/false }.
    /// </summary>
    [HttpPost("verify-reference")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyReference(
        [FromBody] StorageFileReference fileRef,
        CancellationToken cancellationToken)
    {
        // Prevent local path traversal
        if (string.Equals(fileRef.ProviderType, "Local", StringComparison.OrdinalIgnoreCase))
        {
            if (fileRef.FileId.Contains("..") || Path.IsPathRooted(fileRef.FileId))
                return BadRequest("Invalid file reference.");
        }

        try
        {
            var storageProvider = _factory.GetProvider(fileRef.ProviderType);
            var result = await storageProvider.DownloadFileAsync(fileRef, cancellationToken);
            await result.Content.DisposeAsync();
            return Ok(new { accessible = true });
        }
        catch
        {
            return Ok(new { accessible = false });
        }
    }
}
