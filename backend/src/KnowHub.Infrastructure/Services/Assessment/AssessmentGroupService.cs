using KnowHub.Application.Contracts;
using KnowHub.Application.Contracts.Assessment;
using KnowHub.Application.Models;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using KnowHub.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace KnowHub.Infrastructure.Services.Assessment;

public class AssessmentGroupService : IAssessmentGroupService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public AssessmentGroupService(KnowHubDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<AssessmentGroupDto>> GetGroupsAsync(AssessmentGroupFilter filter, CancellationToken ct)
    {
        var query = _db.AssessmentGroups
            .Where(g => g.TenantId == _currentUser.TenantId)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            query = query.Where(g => g.GroupName.Contains(filter.SearchTerm) || g.GroupCode.Contains(filter.SearchTerm));

        if (filter.IsActive.HasValue)
            query = query.Where(g => g.IsActive == filter.IsActive.Value);

        if (filter.PrimaryLeadId.HasValue)
            query = query.Where(g => g.PrimaryLeadUserId == filter.PrimaryLeadId.Value);

        if (filter.CoLeadId.HasValue)
            query = query.Where(g => g.GroupCoLeads.Any(c => c.UserId == filter.CoLeadId.Value && c.IsActive));

        var (data, total) = await query
            .OrderBy(g => g.GroupName)
            .Select(g => new AssessmentGroupDto
            {
                Id                  = g.Id,
                GroupName           = g.GroupName,
                GroupCode           = g.GroupCode,
                Description         = g.Description,
                PrimaryLeadUserId      = g.PrimaryLeadUserId,
                PrimaryLeadName        = g.PrimaryLead.FullName,
                CoLeadUserId           = g.GroupCoLeads.Where(c => c.IsActive).Select(c => (Guid?)c.UserId).FirstOrDefault(),
                CoLeadName             = g.GroupCoLeads.Where(c => c.IsActive).Select(c => c.User.FullName).FirstOrDefault(),
                ActiveEmployeeCount = g.GroupMembers.Count(e => e.IsActive),
                IsActive            = g.IsActive,
                CreatedDate         = g.CreatedDate,
                RecordVersion       = g.RecordVersion
            })
            .ToPagedListAsync(filter.PageNumber, filter.PageSize, ct);

        return new PagedResult<AssessmentGroupDto> { Data = data, TotalCount = total, PageNumber = filter.PageNumber, PageSize = filter.PageSize };
    }

    public async Task<AssessmentGroupDto> GetGroupByIdAsync(Guid id, CancellationToken ct)
    {
        var group = await _db.AssessmentGroups
            .Where(g => g.Id == id && g.TenantId == _currentUser.TenantId)
            .AsNoTracking()
            .Select(g => new AssessmentGroupDto
            {
                Id                  = g.Id,
                GroupName           = g.GroupName,
                GroupCode           = g.GroupCode,
                Description         = g.Description,
                PrimaryLeadUserId      = g.PrimaryLeadUserId,
                PrimaryLeadName        = g.PrimaryLead.FullName,
                CoLeadUserId           = g.GroupCoLeads.Where(c => c.IsActive).Select(c => (Guid?)c.UserId).FirstOrDefault(),
                CoLeadName             = g.GroupCoLeads.Where(c => c.IsActive).Select(c => c.User.FullName).FirstOrDefault(),
                ActiveEmployeeCount = g.GroupMembers.Count(e => e.IsActive),
                IsActive            = g.IsActive,
                CreatedDate         = g.CreatedDate,
                RecordVersion       = g.RecordVersion
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("AssessmentGroup", id);

        return group;
    }

    public async Task<AssessmentGroupDto> CreateGroupAsync(CreateAssessmentGroupRequest request, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only Admin or SuperAdmin can create assessment groups.");

        var codeExists = await _db.AssessmentGroups.AnyAsync(
            g => g.GroupCode == request.GroupCode && g.TenantId == _currentUser.TenantId, ct);
        if (codeExists)
            throw new ConflictException($"A group with code '{request.GroupCode}' already exists.");

        var championConflict = await _db.AssessmentGroups.AnyAsync(
            g => g.PrimaryLeadUserId == request.PrimaryLeadUserId && g.IsActive && g.TenantId == _currentUser.TenantId, ct);
        if (championConflict)
            throw new ConflictException("This user is already an active primary lead in another group.");

        var entity = new AssessmentGroup
        {
            TenantId        = _currentUser.TenantId,
            GroupName       = request.GroupName,
            GroupCode       = request.GroupCode,
            Description     = request.Description,
            PrimaryLeadUserId  = request.PrimaryLeadUserId,
            IsActive        = true,
            CreatedBy       = _currentUser.UserId,
            ModifiedBy      = _currentUser.UserId
        };
        _db.AssessmentGroups.Add(entity);

        if (request.CoLeadUserId.HasValue)
        {
            _db.AssessmentGroupCoLeads.Add(new AssessmentGroupCoLead
            {
                TenantId      = _currentUser.TenantId,
                GroupId       = entity.Id,
                UserId        = request.CoLeadUserId.Value,
                EffectiveFrom = DateTime.UtcNow,
                IsActive      = true,
                CreatedBy     = _currentUser.UserId,
                ModifiedBy    = _currentUser.UserId
            });
        }

        await WriteAuditAsync(null, entity.Id, "AssessmentGroup", AssessmentActionType.Created, null, null, ct);
        await _db.SaveChangesAsync(ct);

        return await GetGroupByIdAsync(entity.Id, ct);
    }

    public async Task<AssessmentGroupDto> UpdateGroupAsync(Guid id, UpdateAssessmentGroupRequest request, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only Admin or SuperAdmin can update assessment groups.");

        var entity = await _db.AssessmentGroups
            .FirstOrDefaultAsync(g => g.Id == id && g.TenantId == _currentUser.TenantId, ct)
            ?? throw new NotFoundException("AssessmentGroup", id);

        if (entity.RecordVersion != request.RecordVersion)
            throw new ConflictException("The group has been modified by another user. Please refresh and try again.");

        if (entity.PrimaryLeadUserId != request.PrimaryLeadUserId)
        {
            var championConflict = await _db.AssessmentGroups.AnyAsync(
                g => g.PrimaryLeadUserId == request.PrimaryLeadUserId && g.IsActive && g.TenantId == _currentUser.TenantId && g.Id != id, ct);
            if (championConflict)
                throw new ConflictException("This user is already an active primary lead in another group.");
        }

        entity.GroupName      = request.GroupName;
        entity.Description    = request.Description;
        entity.PrimaryLeadUserId = request.PrimaryLeadUserId;
        entity.IsActive       = request.IsActive;
        entity.ModifiedOn     = DateTime.UtcNow;
        entity.ModifiedBy     = _currentUser.UserId;
        entity.RecordVersion++;

        // Handle CoE change
        if (request.CoLeadUserId.HasValue)
        {
            var existingCoE = await _db.AssessmentGroupCoLeads
                .Where(c => c.GroupId == id && c.IsActive)
                .ToListAsync(ct);
            foreach (var old in existingCoE.Where(c => c.UserId != request.CoLeadUserId.Value))
            {
                old.IsActive   = false;
                old.EffectiveTo = DateTime.UtcNow;
                old.ModifiedOn = DateTime.UtcNow;
                old.ModifiedBy = _currentUser.UserId;
            }
            var alreadyActive = existingCoE.Any(c => c.UserId == request.CoLeadUserId.Value);
            if (!alreadyActive)
            {
                _db.AssessmentGroupCoLeads.Add(new AssessmentGroupCoLead
                {
                    TenantId      = _currentUser.TenantId,
                    GroupId       = id,
                    UserId        = request.CoLeadUserId.Value,
                    EffectiveFrom = DateTime.UtcNow,
                    IsActive      = true,
                    CreatedBy     = _currentUser.UserId,
                    ModifiedBy    = _currentUser.UserId
                });
            }
        }

        await WriteAuditAsync(null, entity.Id, "AssessmentGroup", AssessmentActionType.Updated, null, null, ct);
        await _db.SaveChangesAsync(ct);

        return await GetGroupByIdAsync(entity.Id, ct);
    }

    public async Task DeactivateGroupAsync(Guid id, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only Admin or SuperAdmin can deactivate assessment groups.");

        var entity = await _db.AssessmentGroups
            .FirstOrDefaultAsync(g => g.Id == id && g.TenantId == _currentUser.TenantId, ct)
            ?? throw new NotFoundException("AssessmentGroup", id);

        entity.IsActive   = false;
        entity.ModifiedOn = DateTime.UtcNow;
        entity.ModifiedBy = _currentUser.UserId;
        entity.RecordVersion++;

        await WriteAuditAsync(null, entity.Id, "AssessmentGroup", AssessmentActionType.Updated, null, null, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<GroupMemberDto>> GetGroupMembersAsync(Guid groupId, CancellationToken ct)
    {
        return await _db.AssessmentGroupMembers
            .Where(e => e.GroupId == groupId && e.Group.TenantId == _currentUser.TenantId)
            .AsNoTracking()
            .OrderBy(e => e.User.FullName)
            .Select(e => new GroupMemberDto
            {
                Id            = e.Id,
                UserId        = e.UserId,
                FullName      = e.User.FullName,
                Designation   = e.User.Designation,
                Department    = e.User.Department,
                Email         = e.User.Email,
                WorkRoleId    = e.WorkRoleId,
                WorkRoleCode  = e.WorkRole != null ? e.WorkRole.Code : null,
                WorkRoleName  = e.WorkRole != null ? e.WorkRole.Name : null,
                EffectiveFrom = e.EffectiveFrom,
                EffectiveTo   = e.EffectiveTo,
                IsActive      = e.IsActive
            })
            .ToListAsync(ct);
    }

    public async Task AddMemberToGroupAsync(Guid groupId, AssignGroupMemberRequest request, CancellationToken ct)
    {
        var group = await _db.AssessmentGroups
            .FirstOrDefaultAsync(g => g.Id == groupId && g.TenantId == _currentUser.TenantId, ct)
            ?? throw new NotFoundException("AssessmentGroup", groupId);

        if (!_currentUser.IsAdminOrAbove && _currentUser.UserId != group.PrimaryLeadUserId)
            throw new ForbiddenException("Only Admin or the group's Champion can add employees.");

        var alreadyActive = await _db.AssessmentGroupMembers.AnyAsync(
            e => e.GroupId == groupId && e.UserId == request.UserId && e.IsActive, ct);
        if (alreadyActive)
            throw new ConflictException("User is already an active member of this group.");

        var member = new AssessmentGroupMember
        {
            TenantId      = _currentUser.TenantId,
            GroupId       = groupId,
            UserId        = request.UserId,
            WorkRoleId    = request.WorkRoleId,
            EffectiveFrom = DateTime.UtcNow,
            IsActive      = true,
            CreatedBy     = _currentUser.UserId,
            ModifiedBy    = _currentUser.UserId
        };
        _db.AssessmentGroupMembers.Add(member);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveMemberFromGroupAsync(Guid groupId, Guid userId, CancellationToken ct)
    {
        var group = await _db.AssessmentGroups
            .FirstOrDefaultAsync(g => g.Id == groupId && g.TenantId == _currentUser.TenantId, ct)
            ?? throw new NotFoundException("AssessmentGroup", groupId);

        if (!_currentUser.IsAdminOrAbove && _currentUser.UserId != group.PrimaryLeadUserId)
            throw new ForbiddenException("Only Admin or the group's Champion can remove employees.");

        var member = await _db.AssessmentGroupMembers
            .FirstOrDefaultAsync(e => e.GroupId == groupId && e.UserId == userId && e.IsActive, ct)
            ?? throw new NotFoundException("GroupMember", userId);

        member.IsActive   = false;
        member.EffectiveTo = DateTime.UtcNow;
        member.ModifiedOn = DateTime.UtcNow;
        member.ModifiedBy = _currentUser.UserId;
        member.RecordVersion++;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<GroupMemberDto>> GetGroupCoLeadsAsync(Guid groupId, CancellationToken ct)
    {
        return await _db.AssessmentGroupCoLeads
            .Where(c => c.GroupId == groupId && c.Group.TenantId == _currentUser.TenantId)
            .AsNoTracking()
            .OrderBy(c => c.User.FullName)
            .Select(c => new GroupMemberDto
            {
                Id            = c.Id,
                UserId        = c.UserId,
                FullName      = c.User.FullName,
                Designation   = c.User.Designation,
                Department    = c.User.Department,
                Email         = c.User.Email,
                EffectiveFrom = c.EffectiveFrom,
                EffectiveTo   = c.EffectiveTo,
                IsActive      = c.IsActive
            })
            .ToListAsync(ct);
    }

    public async Task AssignCoLeadToGroupAsync(Guid groupId, AssignGroupMemberRequest request, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only Admin or SuperAdmin can assign Co-Lead members.");

        var group = await _db.AssessmentGroups
            .FirstOrDefaultAsync(g => g.Id == groupId && g.TenantId == _currentUser.TenantId, ct)
            ?? throw new NotFoundException("AssessmentGroup", groupId);

        var alreadyActive = await _db.AssessmentGroupCoLeads.AnyAsync(
            c => c.GroupId == groupId && c.UserId == request.UserId && c.IsActive, ct);
        if (alreadyActive)
            throw new ConflictException("User is already an active Co-Lead member for this group.");

        var coe = new AssessmentGroupCoLead
        {
            TenantId  = _currentUser.TenantId,
            GroupId   = groupId,
            UserId    = request.UserId,
            EffectiveFrom = DateTime.UtcNow,
            IsActive      = true,
            CreatedBy     = _currentUser.UserId,
            ModifiedBy    = _currentUser.UserId
        };
        _db.AssessmentGroupCoLeads.Add(coe);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveCoLeadFromGroupAsync(Guid groupId, Guid userId, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only Admin or SuperAdmin can remove Co-Lead members.");

        var coe = await _db.AssessmentGroupCoLeads
            .FirstOrDefaultAsync(c => c.GroupId == groupId && c.UserId == userId && c.IsActive
                                   && c.Group.TenantId == _currentUser.TenantId, ct)
            ?? throw new NotFoundException("GroupCoLead", userId);

        coe.IsActive    = false;
        coe.EffectiveTo = DateTime.UtcNow;
        coe.ModifiedOn  = DateTime.UtcNow;
        coe.ModifiedBy  = _currentUser.UserId;
        coe.RecordVersion++;
        await _db.SaveChangesAsync(ct);
    }

    // -- helpers --------------------------------------------------------------

    private async Task WriteAuditAsync(Guid? assessmentId, Guid entityId, string entityType,
        AssessmentActionType action, string? oldJson, string? newJson, CancellationToken ct)
    {
        var log = new AssessmentAuditLog
        {
            TenantId              = _currentUser.TenantId,
            EmployeeAssessmentId  = assessmentId,
            RelatedEntityType     = entityType,
            RelatedEntityId       = entityId,
            ActionType            = action,
            OldValueJson          = oldJson,
            NewValueJson          = newJson,
            ChangedBy             = _currentUser.UserId,
            ChangedOn             = DateTime.UtcNow,
            CreatedBy             = _currentUser.UserId,
            ModifiedBy            = _currentUser.UserId
        };
        _db.AssessmentAuditLogs.Add(log);
        // Saved by caller via SaveChangesAsync
        await Task.CompletedTask;
    }
}
