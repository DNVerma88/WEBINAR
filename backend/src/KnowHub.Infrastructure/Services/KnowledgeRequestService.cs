using KnowHub.Application.Contracts;
using KnowHub.Application.Models;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using KnowHub.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace KnowHub.Infrastructure.Services;

public class KnowledgeRequestService : IKnowledgeRequestService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly INotificationService _notificationService;
    private readonly IXpService _xpService;

    public KnowledgeRequestService(
        KnowHubDbContext db,
        ICurrentUserAccessor currentUser,
        INotificationService notificationService,
        IXpService xpService)
    {
        _db = db;
        _currentUser = currentUser;
        _notificationService = notificationService;
        _xpService = xpService;
    }

    public async Task<PagedResult<KnowledgeRequestDto>> GetRequestsAsync(GetKnowledgeRequestsRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var query = _db.KnowledgeRequests
            .Where(r => r.TenantId == _currentUser.TenantId)
            .AsNoTracking();

        if (request.Status.HasValue) query = query.Where(r => r.Status == request.Status.Value);
        if (request.CategoryId.HasValue) query = query.Where(r => r.CategoryId == request.CategoryId.Value);
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            query = query.Where(r => r.Title.Contains(request.SearchTerm) || r.Description.Contains(request.SearchTerm));

        var (data, total) = await query
            .OrderByDescending(r => r.UpvoteCount)
            .ThenByDescending(r => r.CreatedDate)
            .Select(r => new KnowledgeRequestDto
            {
                Id = r.Id,
                RequesterId = r.RequesterId,
                RequesterName = r.Requester != null ? r.Requester.FullName : string.Empty,
                Title = r.Title,
                Description = r.Description,
                CategoryId = r.CategoryId,
                CategoryName = r.Category != null ? r.Category.Name : null,
                UpvoteCount = r.UpvoteCount,
                IsAddressed = r.IsAddressed,
                Status = r.Status,
                BountyXp = r.BountyXp,
                CreatedDate = r.CreatedDate,
                HasUpvoted = r.Likes.Any(l => l.UserId == userId && l.KnowledgeRequestId == r.Id),
                ClaimedByUserId = r.ClaimedByUserId,
                ClaimedByName = r.ClaimedByUser != null ? r.ClaimedByUser.FullName : null,
                AddressedBySessionId = r.AddressedBySessionId,
            })
            .ToPagedListAsync(request.PageNumber, request.PageSize, cancellationToken);

        return new PagedResult<KnowledgeRequestDto> { Data = data, TotalCount = total, PageNumber = request.PageNumber, PageSize = request.PageSize };
    }

    public async Task<KnowledgeRequestDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var dto = await _db.KnowledgeRequests
            .Where(r => r.Id == id && r.TenantId == _currentUser.TenantId)
            .AsNoTracking()
            .Select(r => new KnowledgeRequestDto
            {
                Id = r.Id,
                RequesterId = r.RequesterId,
                RequesterName = r.Requester != null ? r.Requester.FullName : string.Empty,
                Title = r.Title,
                Description = r.Description,
                CategoryId = r.CategoryId,
                CategoryName = r.Category != null ? r.Category.Name : null,
                UpvoteCount = r.UpvoteCount,
                IsAddressed = r.IsAddressed,
                Status = r.Status,
                BountyXp = r.BountyXp,
                CreatedDate = r.CreatedDate,
                HasUpvoted = r.Likes.Any(l => l.UserId == userId && l.KnowledgeRequestId == r.Id),
                ClaimedByUserId = r.ClaimedByUserId,
                ClaimedByName = r.ClaimedByUser != null ? r.ClaimedByUser.FullName : null,
                AddressedBySessionId = r.AddressedBySessionId,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (dto is null) throw new NotFoundException("KnowledgeRequest", id);
        return dto;
    }

    public async Task<KnowledgeRequestDto> CreateAsync(CreateKnowledgeRequestRequest request, CancellationToken cancellationToken)
    {
        if (request.CategoryId.HasValue)
        {
            var categoryExists = await _db.Categories.AnyAsync(c => c.Id == request.CategoryId.Value && c.TenantId == _currentUser.TenantId && c.IsActive, cancellationToken);
            if (!categoryExists) throw new NotFoundException("Category", request.CategoryId.Value);
        }

        if (request.BountyXp < 0) throw new BusinessRuleException("BountyXp cannot be negative.");

        var knowledgeRequest = new KnowledgeRequest
        {
            TenantId = _currentUser.TenantId,
            RequesterId = _currentUser.UserId,
            Title = request.Title,
            Description = request.Description,
            CategoryId = request.CategoryId,
            BountyXp = request.BountyXp,
            UpvoteCount = 0,
            IsAddressed = false,
            Status = KnowledgeRequestStatus.Open,
            CreatedBy = _currentUser.UserId,
            ModifiedBy = _currentUser.UserId
        };
        _db.KnowledgeRequests.Add(knowledgeRequest);
        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(knowledgeRequest.Id, cancellationToken);
    }

    public async Task<KnowledgeRequestDto> UpvoteAsync(Guid id, CancellationToken cancellationToken)
    {
        var knowledgeRequest = await _db.KnowledgeRequests
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == _currentUser.TenantId, cancellationToken);
        if (knowledgeRequest is null) throw new NotFoundException("KnowledgeRequest", id);

        var alreadyUpvoted = await _db.Likes
            .AnyAsync(l => l.UserId == _currentUser.UserId && l.KnowledgeRequestId == id, cancellationToken);

        if (alreadyUpvoted) throw new ConflictException("You have already upvoted this knowledge request.");

        var like = new Like
        {
            TenantId = _currentUser.TenantId,
            UserId = _currentUser.UserId,
            KnowledgeRequestId = id,
            CreatedBy = _currentUser.UserId,
            ModifiedBy = _currentUser.UserId
        };
        _db.Likes.Add(like);

        knowledgeRequest.UpvoteCount++;
        knowledgeRequest.ModifiedOn = DateTime.UtcNow;
        knowledgeRequest.RecordVersion++;
        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<KnowledgeRequestDto> ClaimAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsInRole(UserRole.Contributor) && !_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only Contributors may claim knowledge requests.");

        var knowledgeRequest = await _db.KnowledgeRequests
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == _currentUser.TenantId, cancellationToken);
        if (knowledgeRequest is null) throw new NotFoundException("KnowledgeRequest", id);
        if (knowledgeRequest.Status != KnowledgeRequestStatus.Open)
            throw new ConflictException("Only open knowledge requests can be claimed.");

        knowledgeRequest.ClaimedByUserId = _currentUser.UserId;
        knowledgeRequest.Status = KnowledgeRequestStatus.InProgress;
        knowledgeRequest.ModifiedBy = _currentUser.UserId;
        knowledgeRequest.ModifiedOn = DateTime.UtcNow;
        knowledgeRequest.RecordVersion++;

        await _db.SaveChangesAsync(cancellationToken);

        await _notificationService.SendAsync(
            knowledgeRequest.RequesterId, _currentUser.TenantId,
            NotificationType.KnowledgeRequestClaimed,
            "Your Knowledge Request Has Been Claimed",
            $"A contributor has claimed your knowledge request: {knowledgeRequest.Title}",
            "KnowledgeRequest", knowledgeRequest.Id, cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<KnowledgeRequestDto> CloseAsync(Guid id, CloseKnowledgeRequestRequest request, CancellationToken cancellationToken)
    {
        var knowledgeRequest = await _db.KnowledgeRequests
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == _currentUser.TenantId, cancellationToken);
        if (knowledgeRequest is null) throw new NotFoundException("KnowledgeRequest", id);

        if (knowledgeRequest.Status == KnowledgeRequestStatus.Closed)
            throw new ConflictException("This knowledge request is already closed.");

        // Only the requester, the claimer, Admin, or KnowledgeTeam can close
        var canClose = _currentUser.IsAdminOrAbove
            || _currentUser.IsInRole(UserRole.KnowledgeTeam)
            || knowledgeRequest.RequesterId == _currentUser.UserId
            || knowledgeRequest.ClaimedByUserId == _currentUser.UserId;

        if (!canClose)
            throw new ForbiddenException("Only the requester, claimer, or an Admin may close this request.");

        knowledgeRequest.Status = KnowledgeRequestStatus.Closed;
        knowledgeRequest.ModifiedBy = _currentUser.UserId;
        knowledgeRequest.ModifiedOn = DateTime.UtcNow;
        knowledgeRequest.RecordVersion++;

        await _db.SaveChangesAsync(cancellationToken);

        // Award XP to the claimer for answering the knowledge request
        if (knowledgeRequest.ClaimedByUserId.HasValue)
        {
            await _xpService.AwardXpAsync(new AwardXpRequest
            {
                UserId = knowledgeRequest.ClaimedByUserId.Value,
                TenantId = _currentUser.TenantId,
                EventType = XpEventType.AnswerKnowledgeRequest,
                RelatedEntityType = "KnowledgeRequest",
                RelatedEntityId = knowledgeRequest.Id
            }, cancellationToken);
        }

        await _notificationService.SendAsync(
            knowledgeRequest.RequesterId, _currentUser.TenantId,
            NotificationType.General,
            "Knowledge Request Closed",
            $"Your knowledge request \"{ knowledgeRequest.Title}\" has been closed.",
            "KnowledgeRequest", knowledgeRequest.Id, cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<KnowledgeRequestDto> AddressAsync(Guid id, AddressKnowledgeRequestRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAdminOrAbove && !_currentUser.IsInRole(UserRole.KnowledgeTeam))
            throw new ForbiddenException("Only Admin or KnowledgeTeam can mark a request as addressed.");

        var knowledgeRequest = await _db.KnowledgeRequests
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == _currentUser.TenantId, cancellationToken);
        if (knowledgeRequest is null) throw new NotFoundException("KnowledgeRequest", id);

        if (knowledgeRequest.Status == KnowledgeRequestStatus.Closed)
            throw new ConflictException("Cannot address a closed knowledge request.");

        var sessionExists = await _db.Sessions
            .AnyAsync(s => s.Id == request.SessionId && s.TenantId == _currentUser.TenantId, cancellationToken);
        if (!sessionExists) throw new NotFoundException("Session", request.SessionId);

        knowledgeRequest.Status = KnowledgeRequestStatus.Addressed;
        knowledgeRequest.IsAddressed = true;
        knowledgeRequest.AddressedBySessionId = request.SessionId;
        knowledgeRequest.ModifiedBy = _currentUser.UserId;
        knowledgeRequest.ModifiedOn = DateTime.UtcNow;
        knowledgeRequest.RecordVersion++;

        await _db.SaveChangesAsync(cancellationToken);

        // Award XP to the claimer (if any) for fulfilling the knowledge request
        if (knowledgeRequest.ClaimedByUserId.HasValue)
        {
            await _xpService.AwardXpAsync(new AwardXpRequest
            {
                UserId = knowledgeRequest.ClaimedByUserId.Value,
                TenantId = _currentUser.TenantId,
                EventType = XpEventType.AnswerKnowledgeRequest,
                RelatedEntityType = "KnowledgeRequest",
                RelatedEntityId = knowledgeRequest.Id
            }, cancellationToken);
        }

        await _notificationService.SendAsync(
            knowledgeRequest.RequesterId, _currentUser.TenantId,
            NotificationType.General,
            "Knowledge Request Addressed",
            $"Your knowledge request \"{ knowledgeRequest.Title}\" has been addressed by a session.",
            "KnowledgeRequest", knowledgeRequest.Id, cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }
}
