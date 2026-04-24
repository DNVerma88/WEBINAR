using KnowHub.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowHub.Api.Controllers;

[ApiController]
[Route("api/communities")]
[Authorize]
public class CommunitiesController : ControllerBase
{
    private readonly ICommunityService _communityService;
    private readonly ICommunityWikiService _wikiService;

    public CommunitiesController(ICommunityService communityService, ICommunityWikiService wikiService)
    {
        _communityService = communityService;
        _wikiService = wikiService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCommunities([FromQuery] GetCommunitiesRequest request, CancellationToken cancellationToken)
    {
        var result = await _communityService.GetCommunitiesAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CommunityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCommunity(Guid id, CancellationToken cancellationToken)
    {
        var result = await _communityService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CommunityDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCommunity([FromBody] CreateCommunityRequest request, CancellationToken cancellationToken)
    {
        var result = await _communityService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetCommunity), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CommunityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCommunity(Guid id, [FromBody] UpdateCommunityRequest request, CancellationToken cancellationToken)
    {
        var result = await _communityService.UpdateAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCommunity(Guid id, CancellationToken cancellationToken)
    {
        await _communityService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/join")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Join(Guid id, CancellationToken cancellationToken)
    {
        await _communityService.JoinAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/join")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Leave(Guid id, CancellationToken cancellationToken)
    {
        await _communityService.LeaveAsync(id, cancellationToken);
        return NoContent();
    }

    // ---- Wiki ----

    [HttpGet("{id:guid}/wiki")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWikiPages(Guid id, CancellationToken cancellationToken)
    {
        var result = await _wikiService.GetPagesAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/wiki/{pageId:guid}")]
    [ProducesResponseType(typeof(WikiPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWikiPage(Guid id, Guid pageId, CancellationToken cancellationToken)
    {
        var result = await _wikiService.GetPageAsync(id, pageId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/wiki")]
    [ProducesResponseType(typeof(WikiPageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateWikiPage(Guid id, [FromBody] CreateWikiPageRequest request, CancellationToken cancellationToken)
    {
        var result = await _wikiService.CreatePageAsync(id, request, cancellationToken);
        return CreatedAtAction(nameof(GetWikiPage), new { id, pageId = result.Id }, result);
    }

    [HttpPut("{id:guid}/wiki/{pageId:guid}")]
    [ProducesResponseType(typeof(WikiPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateWikiPage(Guid id, Guid pageId, [FromBody] UpdateWikiPageRequest request, CancellationToken cancellationToken)
    {
        var result = await _wikiService.UpdatePageAsync(id, pageId, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}/wiki/{pageId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteWikiPage(Guid id, Guid pageId, CancellationToken cancellationToken)
    {
        await _wikiService.DeletePageAsync(id, pageId, cancellationToken);
        return NoContent();
    }
}
