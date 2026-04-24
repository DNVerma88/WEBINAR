using KnowHub.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowHub.Api.Controllers;

[ApiController]
[Route("api/comments")]
[Authorize]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _commentService;

    public CommentsController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteComment(Guid id, CancellationToken cancellationToken)
    {
        await _commentService.DeleteCommentAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/like")]
    [ProducesResponseType(typeof(LikeToggleResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LikeComment(Guid id, CancellationToken cancellationToken)
    {
        var result = await _commentService.ToggleCommentLikeAsync(id, cancellationToken);
        return Ok(result);
    }
}
