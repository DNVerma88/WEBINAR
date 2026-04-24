using KnowHub.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowHub.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UserFollowsController : ControllerBase
{
    private readonly IUserFollowService _followService;

    public UserFollowsController(IUserFollowService followService)
    {
        _followService = followService;
    }

    /// <summary>Toggle follow/unfollow on a user. Returns the new following state.</summary>
    [HttpPost("{userId:guid}/follow")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleFollow(Guid userId, CancellationToken ct)
    {
        var isFollowing = await _followService.ToggleFollowUserAsync(userId, ct);
        return Ok(new { isFollowing });
    }

    [HttpGet("{userId:guid}/followers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFollowers(Guid userId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _followService.GetFollowersAsync(userId, pageNumber, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("{userId:guid}/following")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFollowing(Guid userId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _followService.GetFollowingAsync(userId, pageNumber, pageSize, ct);
        return Ok(result);
    }
}
