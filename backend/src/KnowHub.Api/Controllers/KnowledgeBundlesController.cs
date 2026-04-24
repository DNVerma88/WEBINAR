using KnowHub.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowHub.Api.Controllers;

[ApiController]
[Route("api/knowledge-bundles")]
[Authorize]
public class KnowledgeBundlesController : ControllerBase
{
    private readonly IKnowledgeBundleService _knowledgeBundleService;

    public KnowledgeBundlesController(IKnowledgeBundleService knowledgeBundleService)
        => _knowledgeBundleService = knowledgeBundleService;

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBundles([FromQuery] GetBundlesRequest request, CancellationToken cancellationToken)
    {
        var result = await _knowledgeBundleService.GetBundlesAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(KnowledgeBundleDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBundle(Guid id, CancellationToken cancellationToken)
    {
        var result = await _knowledgeBundleService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(KnowledgeBundleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateBundle([FromBody] CreateKnowledgeBundleRequest request, CancellationToken cancellationToken)
    {
        var result = await _knowledgeBundleService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetBundle), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(KnowledgeBundleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBundle(Guid id, [FromBody] UpdateKnowledgeBundleRequest request, CancellationToken cancellationToken)
    {
        var result = await _knowledgeBundleService.UpdateAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/items")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddItem(Guid id, [FromBody] AddBundleItemRequest request, CancellationToken cancellationToken)
    {
        await _knowledgeBundleService.AddItemAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/items/{assetId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveItem(Guid id, Guid assetId, CancellationToken cancellationToken)
    {
        await _knowledgeBundleService.RemoveItemAsync(id, assetId, cancellationToken);
        return NoContent();
    }
}
