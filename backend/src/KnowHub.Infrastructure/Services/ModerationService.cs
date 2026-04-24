using KnowHub.Application.Contracts;
using KnowHub.Application.Contracts.Moderation;
using KnowHub.Application.Models;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowHub.Infrastructure.Services;

public class ModerationService : IModerationService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public ModerationService(KnowHubDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ContentFlagDto> FlagContentAsync(FlagContentRequest request, CancellationToken cancellationToken)
    {
        var flag = new ContentFlag
        {
            TenantId = _currentUser.TenantId,
            FlaggedByUserId = _currentUser.UserId,
            ContentType = request.ContentType,
            ContentId = request.ContentId,
            Reason = request.Reason,
            Notes = request.Notes,
            Status = FlagStatus.Pending,
            CreatedBy = _currentUser.UserId,
            ModifiedBy = _currentUser.UserId
        };

        _db.ContentFlags.Add(flag);
        await _db.SaveChangesAsync(cancellationToken);

        var flaggerName = await GetUserNameAsync(flag.FlaggedByUserId, cancellationToken);

        return MapToDto(flag, flaggerName, null);
    }

    public async Task<PagedResult<ContentFlagDto>> GetContentFlagsAsync(
        GetContentFlagsRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only administrators can view content flags.");

        var query = _db.ContentFlags
            .Where(f => f.TenantId == _currentUser.TenantId)
            .AsQueryable();

        if (request.Status.HasValue)
            query = query.Where(f => f.Status == request.Status.Value);

        if (request.ContentType.HasValue)
            query = query.Where(f => f.ContentType == request.ContentType.Value);

        var total = await query.CountAsync(cancellationToken);

        var flags = await query
            .OrderByDescending(f => f.CreatedDate)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .AsNoTracking()
            .Select(f => new
            {
                Flag = f,
                FlaggerName = _db.Users.Where(u => u.Id == f.FlaggedByUserId)
                    .Select(u => u.FullName).FirstOrDefault() ?? "Unknown",
                ReviewerName = f.ReviewedByUserId.HasValue
                    ? _db.Users.Where(u => u.Id == f.ReviewedByUserId.Value)
                        .Select(u => u.FullName).FirstOrDefault()
                    : null
            })
            .ToListAsync(cancellationToken);

        var dtos = flags.Select(x => MapToDto(x.Flag, x.FlaggerName, x.ReviewerName)).ToList();

        return new PagedResult<ContentFlagDto> { Data = dtos, TotalCount = total, PageNumber = request.PageNumber, PageSize = request.PageSize };
    }

    public async Task<ContentFlagDto> ReviewFlagAsync(
        Guid flagId, ReviewFlagRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only administrators can review content flags.");

        var flag = await _db.ContentFlags
            .FirstOrDefaultAsync(f => f.Id == flagId && f.TenantId == _currentUser.TenantId, cancellationToken);

        if (flag is null) throw new NotFoundException("ContentFlag", flagId);

        flag.Status = request.Status;
        flag.ReviewedByUserId = _currentUser.UserId;
        flag.ReviewedAt = DateTime.UtcNow;
        flag.ReviewNotes = request.ReviewNotes;
        flag.ModifiedBy = _currentUser.UserId;
        flag.ModifiedOn = DateTime.UtcNow;
        flag.RecordVersion++;

        await _db.SaveChangesAsync(cancellationToken);

        var flaggerName = await GetUserNameAsync(flag.FlaggedByUserId, cancellationToken);
        var reviewerName = await GetUserNameAsync(_currentUser.UserId, cancellationToken);

        return MapToDto(flag, flaggerName, reviewerName);
    }

    public async Task<UserSuspensionDto> SuspendUserAsync(
        SuspendUserRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only administrators can suspend users.");

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId && u.TenantId == _currentUser.TenantId, cancellationToken);

        if (user is null) throw new NotFoundException("User", request.UserId);

        if (request.UserId == _currentUser.UserId)
            throw new BusinessRuleException("Administrators cannot suspend their own account.");

        var suspension = new UserSuspension
        {
            TenantId = _currentUser.TenantId,
            UserId = request.UserId,
            SuspendedByUserId = _currentUser.UserId,
            Reason = request.Reason,
            SuspendedAt = DateTime.UtcNow,
            ExpiresAt = request.ExpiresAt,
            IsActive = true,
            CreatedBy = _currentUser.UserId,
            ModifiedBy = _currentUser.UserId
        };

        user.IsActive = false;
        // Revoke active refresh token so suspended user cannot obtain new access tokens
        user.RefreshTokenHash = null;
        user.RefreshTokenExpiresAt = null;
        _db.UserSuspensions.Add(suspension);
        await _db.SaveChangesAsync(cancellationToken);

        var userName = user.FullName;
        var suspenderName = await GetUserNameAsync(_currentUser.UserId, cancellationToken);

        return MapSuspensionToDto(suspension, userName, suspenderName);
    }

    public async Task<UserSuspensionDto> LiftSuspensionAsync(
        Guid suspensionId, LiftSuspensionRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only administrators can lift suspensions.");

        var suspension = await _db.UserSuspensions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == suspensionId && s.TenantId == _currentUser.TenantId, cancellationToken);

        if (suspension is null) throw new NotFoundException("UserSuspension", suspensionId);
        if (!suspension.IsActive) throw new BusinessRuleException("This suspension is already inactive.");

        suspension.IsActive = false;
        suspension.LiftedByUserId = _currentUser.UserId;
        suspension.LiftedAt = DateTime.UtcNow;
        suspension.LiftReason = request.LiftReason;
        suspension.ModifiedBy = _currentUser.UserId;
        suspension.ModifiedOn = DateTime.UtcNow;
        suspension.RecordVersion++;

        if (suspension.User is not null)
            suspension.User.IsActive = true;

        await _db.SaveChangesAsync(cancellationToken);

        var userName = suspension.User?.FullName ?? "Unknown";
        var suspenderName = await GetUserNameAsync(suspension.SuspendedByUserId, cancellationToken);

        return MapSuspensionToDto(suspension, userName, suspenderName);
    }

    public async Task<PagedResult<UserSuspensionDto>> GetActiveSuspensionsAsync(
        int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only administrators can view active suspensions.");

        var query = _db.UserSuspensions
            .Where(s => s.TenantId == _currentUser.TenantId && s.IsActive)
            .AsQueryable();

        var total = await query.CountAsync(cancellationToken);

        var suspensions = await query
            .OrderByDescending(s => s.SuspendedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var userIds = suspensions.Select(s => s.UserId)
            .Concat(suspensions.Select(s => s.SuspendedByUserId))
            .Distinct()
            .ToList();

        var userNames = await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .AsNoTracking()
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);

        var dtos = suspensions.Select(s =>
            MapSuspensionToDto(s,
                userNames.GetValueOrDefault(s.UserId, "Unknown"),
                userNames.GetValueOrDefault(s.SuspendedByUserId, "Unknown")))
            .ToList();

        return new PagedResult<UserSuspensionDto>
        {
            Data = dtos, TotalCount = total, PageNumber = pageNumber, PageSize = pageSize
        };
    }

    public async Task<List<UserSuspensionDto>> GetUserSuspensionsAsync(
        Guid userId, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only administrators can view suspension history.");

        var suspensions = await _db.UserSuspensions
            .Where(s => s.UserId == userId && s.TenantId == _currentUser.TenantId)
            .OrderByDescending(s => s.SuspendedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var userName = await GetUserNameAsync(userId, cancellationToken);
        var suspenderIds = suspensions.Select(s => s.SuspendedByUserId).Distinct().ToList();
        var suspenderNames = await _db.Users
            .Where(u => suspenderIds.Contains(u.Id))
            .AsNoTracking()
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);

        return suspensions.Select(s =>
            MapSuspensionToDto(s, userName,
                suspenderNames.GetValueOrDefault(s.SuspendedByUserId, "Unknown")))
            .ToList();
    }

    public async Task BulkUpdateSessionStatusAsync(
        BulkSessionStatusRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only administrators can perform bulk session operations.");

        if (!Enum.TryParse<SessionStatus>(request.NewStatus, true, out var newStatus))
            throw new ValidationException("NewStatus", $"'{request.NewStatus}' is not a valid session status.");

        var sessions = await _db.Sessions
            .Where(s => request.SessionIds.Contains(s.Id) && s.TenantId == _currentUser.TenantId)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
        {
            session.Status = newStatus;
            session.ModifiedBy = _currentUser.UserId;
            session.ModifiedOn = DateTime.UtcNow;
            session.RecordVersion++;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> GetUserNameAsync(Guid userId, CancellationToken cancellationToken) =>
        await _db.Users
            .Where(u => u.Id == userId)
            .AsNoTracking()
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(cancellationToken) ?? "Unknown";

    private static ContentFlagDto MapToDto(ContentFlag flag, string flaggerName, string? reviewerName) =>
        new(flag.Id, flag.ContentType, flag.ContentId, flag.Reason, flag.Status,
            flag.Notes, flaggerName, reviewerName, flag.ReviewedAt, flag.ReviewNotes, flag.CreatedDate);

    private static UserSuspensionDto MapSuspensionToDto(
        UserSuspension s, string userName, string suspenderName) =>
        new(s.Id, s.UserId, userName, suspenderName, s.Reason, s.SuspendedAt,
            s.ExpiresAt, s.IsActive, s.LiftReason, s.LiftedAt);
}
