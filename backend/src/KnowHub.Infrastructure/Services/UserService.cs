using KnowHub.Application.Contracts;
using KnowHub.Application.Models;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using KnowHub.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace KnowHub.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public UserService(KnowHubDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<UserDto>> GetUsersAsync(GetUsersRequest request, CancellationToken cancellationToken)
    {
        var query = _db.Users
            .Where(u => u.TenantId == _currentUser.TenantId)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            query = query.Where(u => u.FullName.Contains(request.SearchTerm) || u.Email.Contains(request.SearchTerm));

        if (!string.IsNullOrWhiteSpace(request.Department))
            query = query.Where(u => u.Department == request.Department);

        if (request.Role.HasValue)
            query = query.Where(u => (u.Role & request.Role.Value) == request.Role.Value);

        if (request.IsActive.HasValue)
            query = query.Where(u => u.IsActive == request.IsActive.Value);

        var (data, total) = await query
            .OrderBy(u => u.FullName)
            .Select(u => new UserDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Department = u.Department,
                Designation = u.Designation,
                YearsOfExperience = u.YearsOfExperience,
                Location = u.Location,
                ProfilePhotoUrl = u.ProfilePhotoUrl,
                Role = u.Role,
                IsActive = u.IsActive,
                RecordVersion = u.RecordVersion
            })
            .ToPagedListAsync(request.PageNumber, request.PageSize, cancellationToken);

        return new PagedResult<UserDto> { Data = data, TotalCount = total, PageNumber = request.PageNumber, PageSize = request.PageSize };
    }

    public async Task<UserDto> GetUserByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .Where(u => u.Id == id && u.TenantId == _currentUser.TenantId)
            .AsNoTracking()
            .Select(u => new UserDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Department = u.Department,
                Designation = u.Designation,
                YearsOfExperience = u.YearsOfExperience,
                Location = u.Location,
                ProfilePhotoUrl = u.ProfilePhotoUrl,
                Role = u.Role,
                IsActive = u.IsActive,
                RecordVersion = u.RecordVersion
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null) throw new NotFoundException("User", id);
        return user;
    }

    public async Task<UserDto> UpdateUserAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId != id && !_currentUser.IsAdminOrAbove)
            throw new ForbiddenException();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == _currentUser.TenantId, cancellationToken);
        if (user is null) throw new NotFoundException("User", id);
        if (user.RecordVersion != request.RecordVersion) throw new ConflictException("The user was modified by another session. Please refresh and try again.");

        user.FullName = request.FullName;
        user.Department = request.Department;
        user.Designation = request.Designation;
        user.YearsOfExperience = request.YearsOfExperience;
        user.Location = request.Location;
        user.ProfilePhotoUrl = request.ProfilePhotoUrl;
        user.ModifiedOn = DateTime.UtcNow;
        user.ModifiedBy = _currentUser.UserId;
        user.RecordVersion++;

        await _db.SaveChangesAsync(cancellationToken);
        return await GetUserByIdAsync(id, cancellationToken);
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException();

        var emailExists = await _db.Users.AnyAsync(u => u.TenantId == _currentUser.TenantId && u.Email == request.Email, cancellationToken);
        if (emailExists)
            throw new ConflictException($"An account with email '{request.Email}' already exists.");

        var user = new User
        {
            TenantId = _currentUser.TenantId,
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            Department = request.Department,
            Designation = request.Designation,
            YearsOfExperience = request.YearsOfExperience,
            Location = request.Location,
            IsActive = true,
            CreatedBy = _currentUser.UserId,
            ModifiedBy = _currentUser.UserId,
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);
        return await GetUserByIdAsync(user.Id, cancellationToken);
    }

    public async Task<UserDto> AdminUpdateUserAsync(Guid id, AdminUpdateUserRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == _currentUser.TenantId, cancellationToken);
        if (user is null) throw new NotFoundException("User", id);
        if (user.RecordVersion != request.RecordVersion) throw new ConflictException("The user was modified by another session. Please refresh and try again.");

        var emailTaken = await _db.Users.AnyAsync(u => u.TenantId == _currentUser.TenantId && u.Email == request.Email && u.Id != id, cancellationToken);
        if (emailTaken) throw new ConflictException($"Email '{request.Email}' is already used by another account.");

        user.FullName = request.FullName;
        user.Email = request.Email;
        user.Role = request.Role;
        user.IsActive = request.IsActive;
        user.Department = request.Department;
        user.Designation = request.Designation;
        user.YearsOfExperience = request.YearsOfExperience;
        user.Location = request.Location;
        user.ModifiedOn = DateTime.UtcNow;
        user.ModifiedBy = _currentUser.UserId;
        user.RecordVersion++;

        await _db.SaveChangesAsync(cancellationToken);
        return await GetUserByIdAsync(id, cancellationToken);
    }

    public async Task DeactivateUserAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException();

        if (_currentUser.UserId == id)
            throw new BusinessRuleException("You cannot deactivate your own account.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == _currentUser.TenantId, cancellationToken);
        if (user is null) throw new NotFoundException("User", id);

        user.IsActive = false;
        user.ModifiedOn = DateTime.UtcNow;
        user.ModifiedBy = _currentUser.UserId;
        user.RecordVersion++;

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task FollowUserAsync(Guid targetUserId, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == targetUserId)
            throw new BusinessRuleException("You cannot follow yourself.");

        var targetExists = await _db.Users.AnyAsync(u => u.Id == targetUserId && u.TenantId == _currentUser.TenantId, cancellationToken);
        if (!targetExists) throw new NotFoundException("User", targetUserId);

        var alreadyFollowing = await _db.UserFollowers.AnyAsync(
            f => f.FollowerId == _currentUser.UserId && f.FollowedId == targetUserId && f.TenantId == _currentUser.TenantId,
            cancellationToken);

        if (alreadyFollowing) throw new ConflictException("You are already following this user.");

        var follow = new Domain.Entities.UserFollower
        {
            TenantId = _currentUser.TenantId,
            FollowerId = _currentUser.UserId,
            FollowedId = targetUserId,
            CreatedBy = _currentUser.UserId,
            ModifiedBy = _currentUser.UserId
        };
        _db.UserFollowers.Add(follow);

        var profile = await _db.ContributorProfiles
            .FirstOrDefaultAsync(p => p.UserId == targetUserId && p.TenantId == _currentUser.TenantId, cancellationToken);
        if (profile is not null)
        {
            profile.FollowerCount++;
            profile.ModifiedOn = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UnfollowUserAsync(Guid targetUserId, CancellationToken cancellationToken)
    {
        var follow = await _db.UserFollowers.FirstOrDefaultAsync(
            f => f.FollowerId == _currentUser.UserId && f.FollowedId == targetUserId && f.TenantId == _currentUser.TenantId,
            cancellationToken);

        if (follow is null) throw new NotFoundException("Follow relationship", targetUserId);

        _db.UserFollowers.Remove(follow);

        var profile = await _db.ContributorProfiles
            .FirstOrDefaultAsync(p => p.UserId == targetUserId && p.TenantId == _currentUser.TenantId, cancellationToken);
        if (profile is not null)
        {
            profile.FollowerCount = Math.Max(0, profile.FollowerCount - 1);
            profile.ModifiedOn = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ContributorProfileDto> GetContributorProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var profile = await _db.ContributorProfiles
            .Where(p => p.UserId == userId && p.TenantId == _currentUser.TenantId)
            .AsNoTracking()
            .Select(p => new ContributorProfileDto
            {
                Id = p.Id,
                UserId = p.UserId,
                UserFullName = p.User.FullName,
                ProfilePhotoUrl = p.User.ProfilePhotoUrl,
                AreasOfExpertise = p.AreasOfExpertise,
                TechnologiesKnown = p.TechnologiesKnown,
                Bio = p.Bio,
                AverageRating = p.AverageRating,
                TotalSessionsDelivered = p.TotalSessionsDelivered,
                FollowerCount = p.FollowerCount,
                EndorsementScore = p.EndorsementScore,
                IsKnowledgeBroker = p.IsKnowledgeBroker,
                AvailableForMentoring = p.AvailableForMentoring,
                RecordVersion = p.RecordVersion
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (profile is null) throw new NotFoundException("ContributorProfile for user", userId);
        return profile;
    }

    public async Task<ContributorProfileDto> UpdateContributorProfileAsync(Guid userId, UpdateContributorProfileRequest request, CancellationToken cancellationToken)
    {
        var canManage = _currentUser.UserId == userId
            || _currentUser.IsAdminOrAbove
            || _currentUser.IsInRole(UserRole.KnowledgeTeam);
        if (!canManage)
            throw new ForbiddenException();

        var profile = await _db.ContributorProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId && p.TenantId == _currentUser.TenantId, cancellationToken);

        if (profile is null)
        {
            var userExists = await _db.Users.AnyAsync(u => u.Id == userId && u.TenantId == _currentUser.TenantId, cancellationToken);
            if (!userExists) throw new NotFoundException("User", userId);

            profile = new ContributorProfile
            {
                TenantId = _currentUser.TenantId,
                UserId = userId,
                AreasOfExpertise = request.AreasOfExpertise,
                TechnologiesKnown = request.TechnologiesKnown,
                Bio = request.Bio,
                AvailableForMentoring = request.AvailableForMentoring,
                CreatedBy = _currentUser.UserId,
                ModifiedBy = _currentUser.UserId
            };
            _db.ContributorProfiles.Add(profile);
        }
        else
        {
            if (profile.RecordVersion != request.RecordVersion) throw new ConflictException("Profile was modified by another session.");

            profile.AreasOfExpertise = request.AreasOfExpertise;
            profile.TechnologiesKnown = request.TechnologiesKnown;
            profile.Bio = request.Bio;
            profile.AvailableForMentoring = request.AvailableForMentoring;
            profile.ModifiedOn = DateTime.UtcNow;
            profile.ModifiedBy = _currentUser.UserId;
            profile.RecordVersion++;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await GetContributorProfileAsync(userId, cancellationToken);
    }
}
