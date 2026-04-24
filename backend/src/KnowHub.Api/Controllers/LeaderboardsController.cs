using KnowHub.Application.Contracts;
using KnowHub.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowHub.Api.Controllers;

[ApiController]
[Route("api/leaderboards")]
[Authorize]
public class LeaderboardsController : ControllerBase
{
    private readonly ILeaderboardService _leaderboardService;

    public LeaderboardsController(ILeaderboardService leaderboardService)
        => _leaderboardService = leaderboardService;

    [HttpGet]
    [ProducesResponseType(typeof(LeaderboardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeaderboard(
        [FromQuery] LeaderboardType type = LeaderboardType.ByXp,
        [FromQuery] int? month = null,
        [FromQuery] int? year = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _leaderboardService.GetLeaderboardAsync(type, month, year, cancellationToken);
        return Ok(result);
    }
}
