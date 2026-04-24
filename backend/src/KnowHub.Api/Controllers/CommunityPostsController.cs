using KnowHub.Application.Contracts;
using KnowHub.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowHub.Api.Controllers;

[ApiController]
[Route("api/communities/{communityId:guid}/posts")]
[Authorize]
public class CommunityPostsController : ControllerBase
{
    private readonly ICommunityPostService _postService;
    private readonly IPostReactionService _reactionService;
    private readonly IPostCommentService _commentService;
    private readonly IPostBookmarkService _bookmarkService;
    private readonly IContentModerationService _moderationService;

    public CommunityPostsController(
        ICommunityPostService postService,
        IPostReactionService reactionService,
        IPostCommentService commentService,
        IPostBookmarkService bookmarkService,
        IContentModerationService moderationService)
    {
        _postService        = postService;
        _reactionService    = reactionService;
        _commentService     = commentService;
        _bookmarkService    = bookmarkService;
        _moderationService  = moderationService;
    }

    // ─── Posts ────────────────────────────────────────────────────────────────

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPosts(Guid communityId, [FromQuery] GetPostsRequest request, CancellationToken ct)
    {
        var result = await _postService.GetPostsAsync(communityId, request, ct);
        return Ok(result);
    }

    [HttpGet("{postId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPost(Guid communityId, Guid postId, CancellationToken ct)
    {
        var result = await _postService.GetPostAsync(communityId, postId, ct);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CommunityPostDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreatePost(Guid communityId, [FromBody] CreatePostRequest request, CancellationToken ct)
    {
        var result = await _postService.CreatePostAsync(communityId, request, ct);
        return CreatedAtAction(nameof(GetPost), new { communityId, postId = result.Id }, result);
    }

    [HttpPut("{postId:guid}")]
    [ProducesResponseType(typeof(CommunityPostDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePost(Guid communityId, Guid postId, [FromBody] UpdatePostRequest request, CancellationToken ct)
    {
        var result = await _postService.UpdatePostAsync(communityId, postId, request, ct);
        return Ok(result);
    }

    [HttpDelete("{postId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePost(Guid communityId, Guid postId, CancellationToken ct)
    {
        await _postService.DeletePostAsync(communityId, postId, ct);
        return NoContent();
    }

    [HttpPost("{postId:guid}/pin")]
    [Authorize(Policy = "AdminOrAbove")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TogglePin(Guid communityId, Guid postId, CancellationToken ct)
    {
        await _postService.TogglePinAsync(communityId, postId, ct);
        return NoContent();
    }

    [HttpPatch("{postId:guid}/draft")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaveDraft(Guid communityId, Guid postId, [FromBody] DraftPostRequest request, CancellationToken ct)
    {
        await _postService.SaveDraftAsync(communityId, postId, request, ct);
        return NoContent();
    }

    // ─── Reactions ────────────────────────────────────────────────────────────

    [HttpGet("{postId:guid}/reactions")]
    [ProducesResponseType(typeof(PostReactionResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReactions(Guid communityId, Guid postId, CancellationToken ct)
    {
        var result = await _reactionService.GetReactionsAsync(postId, ct);
        return Ok(result);
    }

    [HttpPost("{postId:guid}/reactions")]
    [ProducesResponseType(typeof(PostReactionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleReaction(Guid communityId, Guid postId, [FromBody] ToggleReactionRequest request, CancellationToken ct)
    {
        var result = await _reactionService.ToggleReactionAsync(postId, request.ReactionType, ct);
        return Ok(result);
    }

    // ─── Comments ─────────────────────────────────────────────────────────────

    [HttpGet("{postId:guid}/comments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetComments(Guid communityId, Guid postId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _commentService.GetCommentsAsync(postId, pageNumber, pageSize, ct);
        return Ok(result);
    }

    [HttpPost("{postId:guid}/comments")]
    [ProducesResponseType(typeof(PostCommentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddComment(Guid communityId, Guid postId, [FromBody] AddCommentRequest request, CancellationToken ct)
    {
        var result = await _commentService.AddCommentAsync(postId, request, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpDelete("{postId:guid}/comments/{commentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteComment(Guid communityId, Guid postId, Guid commentId, CancellationToken ct)
    {
        await _commentService.DeleteCommentAsync(commentId, ct);
        return NoContent();
    }

    // ─── Bookmarks ────────────────────────────────────────────────────────────

    [HttpPost("{postId:guid}/bookmark")]
    [ProducesResponseType(typeof(PostBookmarkToggleResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleBookmark(Guid communityId, Guid postId, CancellationToken ct)
    {
        var result = await _bookmarkService.ToggleBookmarkAsync(postId, ct);
        return Ok(result);
    }

    // ─── Reporting ────────────────────────────────────────────────────────────

    [HttpPost("{postId:guid}/report")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReportPost(Guid communityId, Guid postId, [FromBody] ReportContentRequest request, CancellationToken ct)
    {
        request.TargetPostId = postId;
        await _moderationService.ReportContentAsync(request, ct);
        return NoContent();
    }

    // ─── Full-text search (Phase 5) ───────────────────────────────────────────

    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(Guid communityId, [FromQuery] string q, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _postService.SearchPostsAsync(communityId, q, pageNumber, pageSize, ct);
        return Ok(result);
    }
}
