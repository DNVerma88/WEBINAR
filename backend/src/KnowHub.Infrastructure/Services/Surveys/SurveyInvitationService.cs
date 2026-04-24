using System.Security.Cryptography;
using System.Text;
using KnowHub.Application.Contracts;
using KnowHub.Application.Contracts.Email;
using KnowHub.Application.Contracts.Surveys;
using KnowHub.Application.Models;
using KnowHub.Application.Models.Surveys;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using KnowHub.Infrastructure.Persistence.Extensions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KnowHub.Infrastructure.Services.Surveys;

public sealed class SurveyInvitationService : ISurveyInvitationService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SurveyInvitationService> _logger;

    public SurveyInvitationService(
        KnowHubDbContext db,
        ICurrentUserAccessor currentUser,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<SurveyInvitationService> logger)
    {
        _db            = db;
        _currentUser   = currentUser;
        _emailService  = emailService;
        _configuration = configuration;
        _logger        = logger;
    }

    public async Task<PagedResult<SurveyInvitationDto>> GetInvitationsAsync(
        Guid surveyId, GetInvitationsRequest request, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only admins can view invitations.");

        var query = _db.SurveyInvitations
            .Where(i => i.SurveyId == surveyId && i.TenantId == _currentUser.TenantId)
            .Include(i => i.User)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<SurveyInvitationStatus>(request.Status, true, out var statusFilter))
            query = query.Where(i => i.Status == statusFilter);

        var (data, total) = await query
            .OrderBy(i => i.User.FullName)
            .Select(i => new SurveyInvitationDto(
                i.Id, i.UserId, i.User.FullName, i.User.Email,
                i.Status.ToString(), i.SentAt, i.ExpiresAt, i.SubmittedAt, i.ResendCount))
            .ToPagedListAsync(request.PageNumber, request.PageSize, ct);

        return new PagedResult<SurveyInvitationDto>
        {
            Data       = data,
            TotalCount = total,
            PageNumber = request.PageNumber,
            PageSize   = request.PageSize
        };
    }

    public async Task CreateInvitationsAndSendAsync(Guid surveyId, CancellationToken ct)
    {
        var survey = await _db.Surveys
            .Where(s => s.Id == surveyId)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        if (survey is null)
        {
            _logger.LogWarning("SurveyLaunchJob: survey {SurveyId} not found", surveyId);
            return;
        }

        var employees = await _db.Users
            .Where(u => u.TenantId == survey.TenantId
                     && u.IsActive)
            .Select(u => new { u.Id, u.Email, u.FullName })
            .ToListAsync(ct);

        if (!employees.Any()) return;

        var frontendBaseUrl = _configuration["Survey:FrontendBaseUrl"]
            ?? _configuration["Cors:FrontendOrigin"]
            ?? "http://localhost:5173";

        var expiresAt = survey.EndsAt
            ?? (survey.LaunchedAt ?? DateTime.UtcNow).AddDays(survey.TokenExpiryDays);

        // Generate tokens in memory, bulk insert, then send emails
        var emailQueue = new List<(string Email, string Name, string PlainToken, Guid InvitationId)>();

        foreach (var employee in employees)
        {
            var (plainToken, tokenHash) = GenerateToken();
            var invitation = new SurveyInvitation
            {
                TenantId   = survey.TenantId,
                SurveyId   = survey.Id,
                UserId     = employee.Id,
                TokenHash  = tokenHash,
                Status     = SurveyInvitationStatus.Pending,
                ExpiresAt  = expiresAt,
                CreatedBy  = survey.CreatedBy,
                ModifiedBy = survey.CreatedBy,
            };
            _db.SurveyInvitations.Add(invitation);
            emailQueue.Add((employee.Email, employee.FullName, plainToken, invitation.Id));
        }

        // Persist all invitations BEFORE sending any emails
        await _db.SaveChangesAsync(ct);

        // Send emails and update status — each user is processed independently so one
        // failure (email OR DB) never prevents the remaining users from being invited.
        foreach (var (email, name, plainToken, invitationId) in emailQueue)
        {
            ct.ThrowIfCancellationRequested();

            var surveyUrl = $"{frontendBaseUrl.TrimEnd('/')}/survey/{plainToken}";
            var sent = false;
            try
            {
                await _emailService.SendSurveyInvitationAsync(new SurveyInvitationEmailData(
                    email, name, survey.Title, survey.WelcomeMessage, surveyUrl, expiresAt), ct);
                sent = true;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send survey invitation to {Email} for survey {SurveyId}", email, surveyId);
            }

            try
            {
                if (sent)
                    await _db.SurveyInvitations
                        .Where(i => i.Id == invitationId)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(i => i.Status, SurveyInvitationStatus.Sent)
                            .SetProperty(i => i.SentAt, DateTime.UtcNow)
                            .SetProperty(i => i.ModifiedOn, DateTime.UtcNow), ct);
                else
                    await _db.SurveyInvitations
                        .Where(i => i.Id == invitationId)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(i => i.Status, SurveyInvitationStatus.Failed)
                            .SetProperty(i => i.ModifiedOn, DateTime.UtcNow), ct);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx, "Failed to update invitation status for {InvitationId}", invitationId);
            }
        }
    }

    public async Task ResendToUserAsync(Guid surveyId, Guid userId, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only admins can resend invitations.");

        var survey = await _db.Surveys
            .Where(s => s.Id == surveyId && s.TenantId == _currentUser.TenantId)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        if (survey is null) throw new NotFoundException("Survey", surveyId);
        if (survey.Status != SurveyStatus.Active)
            throw new BusinessRuleException("Invitations can only be resent for Active surveys.");

        var invitation = await _db.SurveyInvitations
            .Where(i => i.SurveyId == surveyId
                     && i.UserId == userId
                     && i.TenantId == _currentUser.TenantId)
            .FirstOrDefaultAsync(ct);

        if (invitation is null) throw new NotFoundException("SurveyInvitation", userId);
        if (invitation.Status == SurveyInvitationStatus.Submitted)
            throw new BusinessRuleException("Cannot resend to a user who has already submitted the survey.");

        await DoResendAsync(invitation, survey, ct);
    }

    public async Task ResendBulkAsync(Guid surveyId, ResendInvitationsRequest request, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only admins can resend invitations.");

        if (request.UserIds.Count > 500)
            throw new BusinessRuleException("Bulk resend is limited to 500 users per request.");

        var survey = await _db.Surveys
            .Where(s => s.Id == surveyId && s.TenantId == _currentUser.TenantId)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        if (survey is null) throw new NotFoundException("Survey", surveyId);
        if (survey.Status != SurveyStatus.Active)
            throw new BusinessRuleException("Invitations can only be resent for Active surveys.");

        var invitations = await _db.SurveyInvitations
            .Where(i => i.SurveyId == surveyId
                     && i.TenantId == _currentUser.TenantId
                     && request.UserIds.Contains(i.UserId)
                     && i.Status != SurveyInvitationStatus.Submitted)
            .ToListAsync(ct);

        foreach (var invitation in invitations)
        {
            ct.ThrowIfCancellationRequested();
            try { await DoResendAsync(invitation, survey, ct); }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error resending invitation {InvitationId}", invitation.Id);
            }
        }
    }

    public async Task ResendAllPendingAsync(Guid surveyId, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only admins can resend invitations.");

        var survey = await _db.Surveys
            .Where(s => s.Id == surveyId && s.TenantId == _currentUser.TenantId)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        if (survey is null) throw new NotFoundException("Survey", surveyId);
        if (survey.Status != SurveyStatus.Active)
            throw new BusinessRuleException("Invitations can only be resent for Active surveys.");

        var now = DateTime.UtcNow;
        var invitations = await _db.SurveyInvitations
            .Where(i => i.SurveyId == surveyId
                     && i.TenantId == _currentUser.TenantId
                     && i.Status == SurveyInvitationStatus.Sent
                     && i.ExpiresAt < now)
            .ToListAsync(ct);

        foreach (var invitation in invitations)
        {
            ct.ThrowIfCancellationRequested();
            try { await DoResendAsync(invitation, survey, ct); }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error resending invitation {InvitationId}", invitation.Id);
            }
        }
    }

    public async Task MarkExpiredAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        await _db.SurveyInvitations
            .Where(i => i.Status == SurveyInvitationStatus.Sent && i.ExpiresAt < now)
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.Status, SurveyInvitationStatus.Expired)
                .SetProperty(i => i.ModifiedOn, DateTime.UtcNow), ct);
    }

    // -- Private helpers ----------------------------------------------------

    private async Task DoResendAsync(SurveyInvitation invitation, Survey survey, CancellationToken ct)
    {
        var user = await _db.Users
            .Where(u => u.Id == invitation.UserId)
            .Select(u => new { u.Email, u.FullName })
            .FirstOrDefaultAsync(ct);

        if (user is null) return;

        var (plainToken, tokenHash) = GenerateToken();
        var frontendBaseUrl = _configuration["Survey:FrontendBaseUrl"]
            ?? _configuration["Cors:FrontendOrigin"]
            ?? "http://localhost:5173";

        var newExpiresAt = survey.EndsAt ?? DateTime.UtcNow.AddDays(survey.TokenExpiryDays);
        var surveyUrl    = $"{frontendBaseUrl.TrimEnd('/')}/survey/{plainToken}";

        // Invalidate old token and regenerate
        invitation.TokenHash    = tokenHash;
        invitation.Status       = SurveyInvitationStatus.Pending;
        invitation.ExpiresAt    = newExpiresAt;
        invitation.ResendCount++;
        invitation.ModifiedBy   = invitation.UserId;
        invitation.ModifiedOn   = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        var sent = false;
        try
        {
            await _emailService.SendSurveyInvitationAsync(
                new SurveyInvitationEmailData(
                    user.Email, user.FullName, survey.Title, survey.WelcomeMessage, surveyUrl, newExpiresAt), ct);
            sent = true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resend survey invitation to {Email}", user.Email);
        }

        try
        {
            if (sent)
            {
                invitation.Status     = SurveyInvitationStatus.Sent;
                invitation.SentAt     = DateTime.UtcNow;
                invitation.ModifiedOn = DateTime.UtcNow;
            }
            else
            {
                invitation.Status     = SurveyInvitationStatus.Failed;
                invitation.ModifiedOn = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync(ct);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception dbEx)
        {
            _logger.LogError(dbEx, "Failed to persist resend status for invitation {InvitationId}", invitation.Id);
        }
    }

    /// <summary>Generates a cryptographically secure one-time token and its SHA-256 hash.</summary>
    private static (string plainToken, string tokenHash) GenerateToken()
    {
        var bytes      = RandomNumberGenerator.GetBytes(32);
        var plainToken = Base64UrlTextEncoder.Encode(bytes);
        var hash       = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(plainToken)));
        return (plainToken, hash.ToLowerInvariant());
    }
}
