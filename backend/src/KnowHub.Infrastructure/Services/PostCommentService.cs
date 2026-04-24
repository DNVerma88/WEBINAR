using FluentValidation;
using KnowHub.Application.Contracts;
using KnowHub.Application.Models;
using KnowHub.Application.Utilities;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using KnowHub.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace KnowHub.Infrastructure.Services;

public class PostCommentService : IPostCommentService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IValidator<AddCommentRequest> _commentValidator;
    private readonly INotificationService _notifications;

    public PostCommentService(
        KnowHubDbContext db,
        ICurrentUserAccessor currentUser,
        IValidator<AddCommentRequest> commentValidator,
        INotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _commentValidator = commentValidator;
        _notifications = notifications;
    }

    private static void ThrowIfInvalid(FluentValidation.Results.ValidationResult result)
    {
        if (result.IsValid) return;
        var errors = result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        throw new KnowHub.Domain.Exceptions.ValidationException(errors);
    }

    public async Task<PagedResult<PostCommentDto>> GetCommentsAsync(Guid postId, int pageNumber, int pageSize, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId;

        // Only top-level comments (replies are nested)
        var (data, total) = await _db.PostComments
            .Where(c => c.PostId == postId && c.TenantId == tenantId && c.ParentCommentId == null)
            .AsNoTracking()
            .OrderBy(c => c.CreatedDate)
            .Select(c => new PostCommentDto
            {
                Id = c.Id,
                PostId = c.PostId,
                AuthorId = c.AuthorId,
                AuthorName = c.Author.FullName,
                AuthorAvatarUrl = c.Author.ProfilePhotoUrl,
                ParentCommentId = c.ParentCommentId,
                BodyMarkdown = c.IsDeleted ? "[deleted]" : c.BodyMarkdown,
                IsDeleted = c.IsDeleted,
                LikeCount = c.LikeCount,
                CreatedDate = c.CreatedDate,
                Replies = c.Replies
                    .OrderBy(r => r.CreatedDate)
                    .Select(r => new PostCommentDto
                    {
                        Id = r.Id,
                        PostId = r.PostId,
                        AuthorId = r.AuthorId,
                        AuthorName = r.Author.FullName,
                        AuthorAvatarUrl = r.Author.ProfilePhotoUrl,
                        ParentCommentId = r.ParentCommentId,
                        BodyMarkdown = r.IsDeleted ? "[deleted]" : r.BodyMarkdown,
                        IsDeleted = r.IsDeleted,
                        LikeCount = r.LikeCount,
                        CreatedDate = r.CreatedDate,
                        Replies = new List<PostCommentDto>()
                    })
                    .ToList()
            })
            .ToPagedListAsync(pageNumber, pageSize, ct);

        return new PagedResult<PostCommentDto> { Data = data, TotalCount = total, PageNumber = pageNumber, PageSize = pageSize };
    }

    public async Task<PostCommentDto> AddCommentAsync(Guid postId, AddCommentRequest request, CancellationToken ct)
    {
        ThrowIfInvalid(await _commentValidator.ValidateAsync(request, ct));

        var tenantId = _currentUser.TenantId;
        var userId = _currentUser.UserId;

        var post = await _db.CommunityPosts
            .Where(p => p.Id == postId && p.TenantId == tenantId)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Post", postId);

        // Enforce max 2 nesting levels
        if (request.ParentCommentId.HasValue)
        {
            var parent = await _db.PostComments
                .Where(c => c.Id == request.ParentCommentId.Value && c.TenantId == tenantId)
                .FirstOrDefaultAsync(ct)
                ?? throw new NotFoundException("Comment", request.ParentCommentId.Value);

            if (parent.ParentCommentId.HasValue)
                throw new BusinessRuleException("Comments can only be nested one level deep.");
        }

        var comment = new PostComment
        {
            TenantId = tenantId,
            PostId = postId,
            AuthorId = userId,
            ParentCommentId = request.ParentCommentId,
            BodyMarkdown = MarkdownSanitizer.Sanitize(request.BodyMarkdown),
            CreatedBy = userId,
            ModifiedBy = userId
        };

        post.CommentCount++;
        post.ModifiedOn = DateTime.UtcNow;

        _db.PostComments.Add(comment);
        await _db.SaveChangesAsync(ct);

        // Phase 4 — @mention parsing: notify each mentioned user
        await DispatchMentionNotificationsAsync(comment, ct);

        var author = await _db.Users
            .Where(u => u.Id == userId)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        return new PostCommentDto
        {
            Id = comment.Id,
            PostId = comment.PostId,
            AuthorId = comment.AuthorId,
            AuthorName = author?.FullName ?? string.Empty,
            AuthorAvatarUrl = author?.ProfilePhotoUrl,
            ParentCommentId = comment.ParentCommentId,
            BodyMarkdown = comment.BodyMarkdown,
            IsDeleted = false,
            LikeCount = 0,
            CreatedDate = comment.CreatedDate,
            Replies = new List<PostCommentDto>()
        };
    }

    public async Task DeleteCommentAsync(Guid commentId, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId;
        var userId = _currentUser.UserId;

        var comment = await _db.PostComments
            .Where(c => c.Id == commentId && c.TenantId == tenantId)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Comment", commentId);

        if (comment.AuthorId != userId && !_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("You can only delete your own comments.");

        comment.IsDeleted = true;
        comment.BodyMarkdown = string.Empty;
        comment.ModifiedOn = DateTime.UtcNow;
        comment.ModifiedBy = userId;
        comment.RecordVersion++;

        await _db.SaveChangesAsync(ct);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Parses @handles from a comment body and fires a mention notification for each resolved user.</summary>
    private async Task DispatchMentionNotificationsAsync(PostComment comment, CancellationToken ct)
    {
        var body = comment.BodyMarkdown;
        if (string.IsNullOrWhiteSpace(body)) return;

        var handles = Regex.Matches(body, @"@([A-Za-z0-9_.+-]+)")
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (handles.Count == 0) return;

        var tenantId = comment.TenantId;

        // Match by full name (case-insensitive, replace spaces with underscore for @mentions)
        foreach (var handle in handles)
        {
            var searchName = handle.Replace('_', ' ');
            var user = await _db.Users
                .Where(u => u.TenantId == tenantId
                            && EF.Functions.ILike(u.FullName, $"%{searchName}%")
                            && u.Id != comment.AuthorId)
                .AsNoTracking()
                .Select(u => new { u.Id })
                .FirstOrDefaultAsync(ct);

            if (user is null) continue;

            await _notifications.SendAsync(
                userId: user.Id,
                tenantId: tenantId,
                type: Domain.Enums.NotificationType.MentionInComment,
                title: "You were mentioned in a comment",
                body: $"@{handle} mentioned you in a post comment.",
                relatedEntityType: "PostComment",
                relatedEntityId: comment.Id,
                cancellationToken: ct);
        }
    }
}
