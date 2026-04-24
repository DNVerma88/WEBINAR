using KnowHub.Application.Contracts.Assessment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowHub.Api.Controllers;

[ApiController]
[Route("api/assessment/groups")]
[Authorize]
public class AssessmentGroupsController : ControllerBase
{
    private readonly IAssessmentGroupService _service;
    public AssessmentGroupsController(IAssessmentGroupService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetGroups([FromQuery] AssessmentGroupFilter filter, CancellationToken ct)
        => Ok(await _service.GetGroupsAsync(filter, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetGroup(Guid id, CancellationToken ct)
        => Ok(await _service.GetGroupByIdAsync(id, ct));

    [HttpPost]
    [Authorize(Policy = "ManagerOrAbove")]
    public async Task<IActionResult> CreateGroup([FromBody] CreateAssessmentGroupRequest request, CancellationToken ct)
    {
        var result = await _service.CreateGroupAsync(request, ct);
        return CreatedAtAction(nameof(GetGroup), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "ManagerOrAbove")]
    public async Task<IActionResult> UpdateGroup(Guid id, [FromBody] UpdateAssessmentGroupRequest request, CancellationToken ct)
        => Ok(await _service.UpdateGroupAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<IActionResult> DeactivateGroup(Guid id, CancellationToken ct)
    {
        await _service.DeactivateGroupAsync(id, ct);
        return NoContent();
    }

    // -- Members sub-resource --------------------------------------------------

    [HttpGet("{groupId:guid}/members")]
    public async Task<IActionResult> GetMembers(Guid groupId, CancellationToken ct)
        => Ok(await _service.GetGroupMembersAsync(groupId, ct));

    [HttpPost("{groupId:guid}/members")]
    [Authorize(Policy = "ManagerOrAbove")]
    public async Task<IActionResult> AddMember(Guid groupId, [FromBody] AssignGroupMemberRequest request, CancellationToken ct)
    {
        await _service.AddMemberToGroupAsync(groupId, request, ct);
        return NoContent();
    }

    [HttpDelete("{groupId:guid}/members/{userId:guid}")]
    [Authorize(Policy = "ManagerOrAbove")]
    public async Task<IActionResult> RemoveMember(Guid groupId, Guid userId, CancellationToken ct)
    {
        await _service.RemoveMemberFromGroupAsync(groupId, userId, ct);
        return NoContent();
    }

    // -- Co-Lead sub-resource --------------------------------------------------

    [HttpGet("{groupId:guid}/co-leads")]
    public async Task<IActionResult> GetCoLeads(Guid groupId, CancellationToken ct)
        => Ok(await _service.GetGroupCoLeadsAsync(groupId, ct));

    [HttpPost("{groupId:guid}/co-leads")]
    [Authorize(Policy = "ManagerOrAbove")]
    public async Task<IActionResult> AssignCoLead(Guid groupId, [FromBody] AssignGroupMemberRequest request, CancellationToken ct)
    {
        await _service.AssignCoLeadToGroupAsync(groupId, request, ct);
        return NoContent();
    }

    [HttpDelete("{groupId:guid}/co-leads/{userId:guid}")]
    [Authorize(Policy = "ManagerOrAbove")]
    public async Task<IActionResult> RemoveCoLead(Guid groupId, Guid userId, CancellationToken ct)
    {
        await _service.RemoveCoLeadFromGroupAsync(groupId, userId, ct);
        return NoContent();
    }
}
