using KnowHub.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowHub.Api.Controllers;

[ApiController]
[Route("api/tags")]
[Authorize]
public class TagsController : ControllerBase
{
    private readonly ITagService _tagService;

    public TagsController(ITagService tagService) => _tagService = tagService;

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTags([FromQuery] GetTagsRequest request, CancellationToken cancellationToken)
    {
        var result = await _tagService.GetTagsAsync(request, cancellationToken);
        return Ok(result);
    }

    // B14: taxonomy write actions require KnowledgeTeam or above
    [HttpPost]
    [Authorize(Policy = "KnowledgeTeamOrAbove")]
    [ProducesResponseType(typeof(TagDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateTag([FromBody] CreateTagRequest request, CancellationToken cancellationToken)
    {
        var result = await _tagService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetTags), new { }, result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "KnowledgeTeamOrAbove")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTag(Guid id, CancellationToken cancellationToken)
    {
        await _tagService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{slug}/posts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPostsByTag(string slug, [FromQuery] GetPostsRequest request, CancellationToken cancellationToken)
    {
        var result = await _tagService.GetPostsByTagAsync(slug, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{slug}/follow")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleFollowTag(string slug, CancellationToken cancellationToken)
    {
        var isFollowing = await _tagService.ToggleFollowTagAsync(slug, cancellationToken);
        return Ok(new { isFollowing });
    }
}
