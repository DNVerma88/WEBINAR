using KnowHub.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowHub.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IXpService _xpService;
    private readonly IStreakService _streakService;
    private readonly ISkillEndorsementService _endorsementService;
    private readonly ILearningPathService _learningPathService;

    public UsersController(
        IUserService userService,
        IXpService xpService,
        IStreakService streakService,
        ISkillEndorsementService endorsementService,
        ILearningPathService learningPathService)
    {
        _userService = userService;
        _xpService = xpService;
        _streakService = streakService;
        _endorsementService = endorsementService;
        _learningPathService = learningPathService;
    }

    // B10: admin-only action — role check at controller level (defense-in-depth)
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await _userService.CreateUserAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetUser), new { id = result.Id }, result);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers([FromQuery] GetUsersRequest request, CancellationToken cancellationToken)
    {
        var result = await _userService.GetUsersAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken cancellationToken)
    {
        var result = await _userService.GetUserByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await _userService.UpdateUserAsync(id, request, cancellationToken);
        return Ok(result);
    }

    // B11: admin-only action
    [HttpPut("{id:guid}/admin")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AdminUpdateUser(Guid id, [FromBody] AdminUpdateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await _userService.AdminUpdateUserAsync(id, request, cancellationToken);
        return Ok(result);
    }

    // B11: admin-only action
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateUser(Guid id, CancellationToken cancellationToken)
    {
        await _userService.DeactivateUserAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/contributor-profile")]
    [ProducesResponseType(typeof(ContributorProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContributorProfile(Guid id, CancellationToken cancellationToken)
    {
        var result = await _userService.GetContributorProfileAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}/contributor-profile")]
    [ProducesResponseType(typeof(ContributorProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateContributorProfile(Guid id, [FromBody] UpdateContributorProfileRequest request, CancellationToken cancellationToken)
    {
        var result = await _userService.UpdateContributorProfileAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/follow")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Follow(Guid id, CancellationToken cancellationToken)
    {
        await _userService.FollowUserAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/follow")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unfollow(Guid id, CancellationToken cancellationToken)
    {
        await _userService.UnfollowUserAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/xp")]
    [ProducesResponseType(typeof(UserXpDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetXp(Guid id, CancellationToken cancellationToken)
    {
        var result = await _xpService.GetUserXpAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/streak")]
    [ProducesResponseType(typeof(UserStreakDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStreak(Guid id, CancellationToken cancellationToken)
    {
        var result = await _streakService.GetStreakAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/endorsements")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEndorsements(
        Guid id,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _endorsementService.GetEndorsementsForUserAsync(id, pageNumber, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/enrolments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEnrolments(Guid id, CancellationToken cancellationToken)
    {
        var result = await _learningPathService.GetUserEnrolmentsAsync(id, cancellationToken);
        return Ok(result);
    }
}
