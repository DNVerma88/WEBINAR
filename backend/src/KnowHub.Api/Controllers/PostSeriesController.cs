using KnowHub.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowHub.Api.Controllers;

[ApiController]
[Route("api/communities/{communityId:guid}/series")]
[Authorize]
public class PostSeriesController : ControllerBase
{
    private readonly IPostSeriesService _seriesService;

    public PostSeriesController(IPostSeriesService seriesService)
    {
        _seriesService = seriesService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSeries(Guid communityId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _seriesService.GetSeriesAsync(communityId, pageNumber, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("{seriesId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSeriesById(Guid communityId, Guid seriesId, CancellationToken ct)
    {
        var result = await _seriesService.GetSeriesByIdAsync(communityId, seriesId, ct);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PostSeriesDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSeries(Guid communityId, [FromBody] CreateSeriesRequest request, CancellationToken ct)
    {
        var result = await _seriesService.CreateSeriesAsync(communityId, request, ct);
        return CreatedAtAction(nameof(GetSeriesById), new { communityId, seriesId = result.Id }, result);
    }

    [HttpPut("{seriesId:guid}")]
    [ProducesResponseType(typeof(PostSeriesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSeries(Guid communityId, Guid seriesId, [FromBody] UpdateSeriesRequest request, CancellationToken ct)
    {
        var result = await _seriesService.UpdateSeriesAsync(communityId, seriesId, request, ct);
        return Ok(result);
    }

    [HttpDelete("{seriesId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSeries(Guid communityId, Guid seriesId, CancellationToken ct)
    {
        await _seriesService.DeleteSeriesAsync(communityId, seriesId, ct);
        return NoContent();
    }

    [HttpPost("{seriesId:guid}/posts/{postId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddPostToSeries(Guid communityId, Guid seriesId, Guid postId, [FromQuery] int order = 0, CancellationToken ct = default)
    {
        await _seriesService.AddPostToSeriesAsync(communityId, seriesId, postId, order, ct);
        return NoContent();
    }

    [HttpDelete("{seriesId:guid}/posts/{postId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePostFromSeries(Guid communityId, Guid seriesId, Guid postId, CancellationToken ct)
    {
        await _seriesService.RemovePostFromSeriesAsync(communityId, seriesId, postId, ct);
        return NoContent();
    }
}
