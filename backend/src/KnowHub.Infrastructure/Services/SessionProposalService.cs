using KnowHub.Application.Contracts;
using KnowHub.Application.Models;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using KnowHub.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace KnowHub.Infrastructure.Services;

public class SessionProposalService : ISessionProposalService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly INotificationService _notificationService;

    public SessionProposalService(KnowHubDbContext db, ICurrentUserAccessor currentUser, INotificationService notificationService)
    {
        _db = db;
        _currentUser = currentUser;
        _notificationService = notificationService;
    }

    public async Task<PagedResult<SessionProposalDto>> GetProposalsAsync(GetSessionProposalsRequest request, CancellationToken cancellationToken)
    {
        var query = _db.SessionProposals
            .Where(p => p.TenantId == _currentUser.TenantId)
            .AsNoTracking();

        query = ApplyRoleFilter(query);

        if (request.Status.HasValue) query = query.Where(p => p.Status == request.Status.Value);
        if (request.CategoryId.HasValue) query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        if (request.ProposerId.HasValue) query = query.Where(p => p.ProposerId == request.ProposerId.Value);
        if (!string.IsNullOrWhiteSpace(request.SearchTerm)) query = query.Where(p => p.Title.Contains(request.SearchTerm) || p.Topic.Contains(request.SearchTerm));

        // API-25: use inline projection instead of Include(Proposer) + Include(Category) + in-memory Select(MapToDto)
        // EF Core converts navigation accesses in .Select() to SQL JOINs — no full entity rows loaded
        var (data, total) = await query
            .OrderByDescending(p => p.CreatedDate)
            .Select(p => new SessionProposalDto
            {
                Id                       = p.Id,
                ProposerId               = p.ProposerId,
                ProposerName             = p.Proposer != null ? p.Proposer.FullName : string.Empty,
                Title                    = p.Title,
                CategoryId               = p.CategoryId,
                CategoryName             = p.Category != null ? p.Category.Name : string.Empty,
                Topic                    = p.Topic,
                DepartmentRelevance      = p.DepartmentRelevance,
                Description              = p.Description,
                Prerequisites            = p.ProblemStatement,
                ExpectedOutcomes         = p.LearningOutcomes,
                TargetAudience           = p.TargetAudience,
                Format                   = p.Format,
                EstimatedDurationMinutes = p.Duration,
                PreferredDate            = p.PreferredDate,
                DifficultyLevel          = p.DifficultyLevel,
                RelatedProject           = p.RelatedProject,
                AllowRecording           = p.AllowRecording,
                Status                   = p.Status,
                SubmittedAt              = p.SubmittedAt,
                CreatedDate              = p.CreatedDate,
                RecordVersion            = p.RecordVersion
            })
            .ToPagedListAsync(request.PageNumber, request.PageSize, cancellationToken);

        return new PagedResult<SessionProposalDto> { Data = data, TotalCount = total, PageNumber = request.PageNumber, PageSize = request.PageSize };
    }

    public async Task<SessionProposalDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _db.SessionProposals
            .Include(p => p.Proposer)
            .Include(p => p.Category)
            .Where(p => p.Id == id && p.TenantId == _currentUser.TenantId)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null) throw new NotFoundException("SessionProposal", id);
        return MapToDto(entity);
    }

    public async Task<SessionProposalDto> CreateAsync(CreateSessionProposalRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsInRole(UserRole.Contributor) && !_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only Contributors, Admins, or Super Admins can submit session proposals.");

        var categoryExists = await _db.Categories.AnyAsync(c => c.Id == request.CategoryId && c.TenantId == _currentUser.TenantId && c.IsActive, cancellationToken);
        if (!categoryExists) throw new NotFoundException("Category", request.CategoryId);

        var proposal = new Domain.Entities.SessionProposal
        {
            TenantId = _currentUser.TenantId,
            ProposerId = _currentUser.UserId,
            Title = request.Title,
            CategoryId = request.CategoryId,
            Topic = request.Topic,
            DepartmentRelevance = request.DepartmentRelevance,
            Description = request.Description,
            ProblemStatement = request.Prerequisites,
            LearningOutcomes = request.ExpectedOutcomes,
            TargetAudience = request.TargetAudience,
            Format = request.Format,
            Duration = request.EstimatedDurationMinutes,
            PreferredDate = request.PreferredDate,
            PreferredTime = request.PreferredTime,
            DifficultyLevel = request.DifficultyLevel,
            RelatedProject = request.RelatedProject,
            AllowRecording = request.AllowRecording,
            Status = ProposalStatus.Draft,
            CreatedBy = _currentUser.UserId,
            ModifiedBy = _currentUser.UserId
        };

        _db.SessionProposals.Add(proposal);
        await _db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(proposal.Id, cancellationToken);
    }

    public async Task<SessionProposalDto> UpdateAsync(Guid id, UpdateSessionProposalRequest request, CancellationToken cancellationToken)
    {
        var proposal = await GetEditableProposalAsync(id, cancellationToken);
        if (proposal.RecordVersion != request.RecordVersion) throw new ConflictException("Proposal was modified by another session.");
        if (proposal.Status != ProposalStatus.Draft && proposal.Status != ProposalStatus.RevisionRequested)
            throw new BusinessRuleException("Only Draft or RevisionRequested proposals can be edited.");

        var entity = await _db.SessionProposals.FindAsync(new object[] { id }, cancellationToken);
        entity!.Title = request.Title;
        entity.CategoryId = request.CategoryId;
        entity.Topic = request.Topic;
        entity.DepartmentRelevance = request.DepartmentRelevance;
        entity.Description = request.Description;
        entity.ProblemStatement = request.Prerequisites;
        entity.LearningOutcomes = request.ExpectedOutcomes;
        entity.TargetAudience = request.TargetAudience;
        entity.Format = request.Format;
        entity.Duration = request.EstimatedDurationMinutes;
        entity.PreferredDate = request.PreferredDate;
        entity.DifficultyLevel = request.DifficultyLevel;
        entity.RelatedProject = request.RelatedProject;
        entity.AllowRecording = request.AllowRecording;
        entity.ModifiedOn = DateTime.UtcNow;
        entity.ModifiedBy = _currentUser.UserId;
        entity.RecordVersion++;
        await _db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var proposal = await _db.SessionProposals.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == _currentUser.TenantId, cancellationToken);
        if (proposal is null) throw new NotFoundException("SessionProposal", id);
        if (proposal.ProposerId != _currentUser.UserId && !_currentUser.IsAdminOrAbove) throw new ForbiddenException();
        if (proposal.Status != ProposalStatus.Draft && proposal.Status != ProposalStatus.RevisionRequested)
            throw new BusinessRuleException("Only Draft or RevisionRequested proposals can be deleted.");

        _db.SessionProposals.Remove(proposal);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<SessionProposalDto> SubmitAsync(Guid id, CancellationToken cancellationToken)
    {
        var proposal = await _db.SessionProposals.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == _currentUser.TenantId, cancellationToken);
        if (proposal is null) throw new NotFoundException("SessionProposal", id);
        if (proposal.ProposerId != _currentUser.UserId && !_currentUser.IsAdminOrAbove) throw new ForbiddenException();
        if (proposal.Status != ProposalStatus.Draft && proposal.Status != ProposalStatus.RevisionRequested)
            throw new BusinessRuleException("Only Draft or RevisionRequested proposals can be submitted.");

        proposal.Status = ProposalStatus.Submitted;
        proposal.SubmittedAt = DateTime.UtcNow;
        proposal.ModifiedOn = DateTime.UtcNow;
        proposal.ModifiedBy = _currentUser.UserId;
        proposal.RecordVersion++;
        await _db.SaveChangesAsync(cancellationToken);

        await _notificationService.SendAsync(
            proposal.ProposerId, proposal.TenantId,
            NotificationType.ProposalSubmitted,
            "Proposal Submitted",
            $"Your proposal '{proposal.Title}' has been submitted for review.",
            "SessionProposal", proposal.Id, cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<SessionProposalDto> ApproveAsync(Guid id, ApproveProposalRequest request, CancellationToken cancellationToken)
    {
        var proposal = await _db.SessionProposals.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == _currentUser.TenantId, cancellationToken);
        if (proposal is null) throw new NotFoundException("SessionProposal", id);

        var (newStatus, step) = GetApprovalTransition(proposal.Status);

        // B4: self-approval bypass prevention
        if (proposal.ProposerId == _currentUser.UserId)
            throw new BusinessRuleException("You cannot approve your own proposal.");

        var approval = new Domain.Entities.ProposalApproval
        {
            TenantId = _currentUser.TenantId,
            ProposalId = id,
            ApproverId = _currentUser.UserId,
            ApprovalStep = step,
            Decision = ApprovalDecision.Approved,
            Comment = request.Comment,
            DecidedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId,
            ModifiedBy = _currentUser.UserId
        };
        _db.ProposalApprovals.Add(approval);

        proposal.Status = newStatus;
        proposal.ModifiedOn = DateTime.UtcNow;
        proposal.ModifiedBy = _currentUser.UserId;
        proposal.RecordVersion++;
        await _db.SaveChangesAsync(cancellationToken);

        await _notificationService.SendAsync(
            proposal.ProposerId, proposal.TenantId,
            NotificationType.ProposalApproved,
            "Proposal Approved",
            $"Your proposal '{proposal.Title}' has been approved.",
            "SessionProposal", proposal.Id, cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<SessionProposalDto> RejectAsync(Guid id, RejectProposalRequest request, CancellationToken cancellationToken)
    {
        var proposal = await _db.SessionProposals.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == _currentUser.TenantId, cancellationToken);
        if (proposal is null) throw new NotFoundException("SessionProposal", id);
        EnsureCanReviewProposal(proposal.Status);

        var (_, step) = GetApprovalTransition(proposal.Status);
        var approval = new Domain.Entities.ProposalApproval
        {
            TenantId = _currentUser.TenantId,
            ProposalId = id,
            ApproverId = _currentUser.UserId,
            ApprovalStep = step,
            Decision = ApprovalDecision.Rejected,
            Comment = request.Comment,
            DecidedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId,
            ModifiedBy = _currentUser.UserId
        };
        _db.ProposalApprovals.Add(approval);

        proposal.Status = ProposalStatus.Rejected;
        proposal.ModifiedOn = DateTime.UtcNow;
        proposal.ModifiedBy = _currentUser.UserId;
        proposal.RecordVersion++;
        await _db.SaveChangesAsync(cancellationToken);

        await _notificationService.SendAsync(
            proposal.ProposerId, proposal.TenantId,
            NotificationType.ProposalRejected,
            "Proposal Rejected",
            $"Your proposal '{proposal.Title}' was not approved: {request.Comment}",
            "SessionProposal", proposal.Id, cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<SessionProposalDto> RequestRevisionAsync(Guid id, RequestRevisionRequest request, CancellationToken cancellationToken)
    {
        var proposal = await _db.SessionProposals.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == _currentUser.TenantId, cancellationToken);
        if (proposal is null) throw new NotFoundException("SessionProposal", id);
        EnsureCanReviewProposal(proposal.Status);

        var (_, step) = GetApprovalTransition(proposal.Status);
        var approval = new Domain.Entities.ProposalApproval
        {
            TenantId = _currentUser.TenantId,
            ProposalId = id,
            ApproverId = _currentUser.UserId,
            ApprovalStep = step,
            Decision = ApprovalDecision.RevisionRequested,
            Comment = request.Comment,
            DecidedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId,
            ModifiedBy = _currentUser.UserId
        };
        _db.ProposalApprovals.Add(approval);

        proposal.Status = ProposalStatus.RevisionRequested;
        proposal.ModifiedOn = DateTime.UtcNow;
        proposal.ModifiedBy = _currentUser.UserId;
        proposal.RecordVersion++;
        await _db.SaveChangesAsync(cancellationToken);

        await _notificationService.SendAsync(
            proposal.ProposerId, proposal.TenantId,
            NotificationType.ProposalRevisionRequested,
            "Revision Requested",
            $"Revisions requested for your proposal '{proposal.Title}': {request.Comment}",
            "SessionProposal", proposal.Id, cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    private async Task<SessionProposalDto> GetEditableProposalAsync(Guid id, CancellationToken cancellationToken)
    {
        var proposal = await GetByIdAsync(id, cancellationToken);
        // B18: only the author or an Admin can edit proposal content
        var canEdit = proposal.ProposerId == _currentUser.UserId || _currentUser.IsAdminOrAbove;
        if (!canEdit) throw new ForbiddenException();
        return proposal;
    }

    private (ProposalStatus NewStatus, ApprovalStep Step) GetApprovalTransition(ProposalStatus currentStatus)
    {
        return currentStatus switch
        {
            ProposalStatus.Submitted when _currentUser.IsInRole(UserRole.Manager) || _currentUser.IsAdminOrAbove =>
                (ProposalStatus.KnowledgeTeamReview, ApprovalStep.ManagerReview),
            ProposalStatus.ManagerReview when _currentUser.IsInRole(UserRole.Manager) || _currentUser.IsAdminOrAbove =>
                (ProposalStatus.KnowledgeTeamReview, ApprovalStep.ManagerReview),
            ProposalStatus.KnowledgeTeamReview when _currentUser.IsInRole(UserRole.KnowledgeTeam) || _currentUser.IsAdminOrAbove =>
                (ProposalStatus.Published, ApprovalStep.KnowledgeTeamReview),
            _ => throw new ForbiddenException("You do not have permission to approve this proposal at its current stage.")
        };
    }

    private static void EnsureCanReviewProposal(ProposalStatus status)
    {
        if (status != ProposalStatus.Submitted && status != ProposalStatus.ManagerReview && status != ProposalStatus.KnowledgeTeamReview)
            throw new BusinessRuleException("This proposal is not in a reviewable state.");
    }

    private static SessionProposalDto MapToDto(Domain.Entities.SessionProposal p) => new()
    {
        Id = p.Id,
        ProposerId = p.ProposerId,
        ProposerName = p.Proposer != null ? p.Proposer.FullName : string.Empty,
        Title = p.Title,
        CategoryId = p.CategoryId,
        CategoryName = p.Category != null ? p.Category.Name : string.Empty,
        Topic = p.Topic,
        DepartmentRelevance = p.DepartmentRelevance,
        Description = p.Description,
        Prerequisites = p.ProblemStatement,
        ExpectedOutcomes = p.LearningOutcomes,
        TargetAudience = p.TargetAudience,
        Format = p.Format,
        EstimatedDurationMinutes = p.Duration,
        PreferredDate = p.PreferredDate,
        DifficultyLevel = p.DifficultyLevel,
        RelatedProject = p.RelatedProject,
        AllowRecording = p.AllowRecording,
        Status = p.Status,
        SubmittedAt = p.SubmittedAt,
        CreatedDate = p.CreatedDate,
        RecordVersion = p.RecordVersion
    };

    private IQueryable<Domain.Entities.SessionProposal> ApplyRoleFilter(IQueryable<Domain.Entities.SessionProposal> query)
    {
        if (_currentUser.IsAdminOrAbove || _currentUser.IsInRole(UserRole.KnowledgeTeam))
            return query;
        if (_currentUser.IsInRole(UserRole.Manager))
            return query.Where(p => p.ProposerId == _currentUser.UserId || p.Status == ProposalStatus.Submitted || p.Status == ProposalStatus.ManagerReview);
        return query.Where(p => p.ProposerId == _currentUser.UserId);
    }
}
