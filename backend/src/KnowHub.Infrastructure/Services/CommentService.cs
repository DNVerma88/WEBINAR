using KnowHub.Application.Contracts;
using KnowHub.Application.Models;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowHub.Infrastructure.Services;

public class CommentService : ICommentService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public CommentService(KnowHubDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<CommentDto>> GetSessionCommentsAsync(
        Guid sessionId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        pageSize = Math.Min(pageSize, 50);
        var sessionExists = await _db.Sessions
            .AnyAsync(s => s.Id == sessionId && s.TenantId == _currentUser.TenantId, cancellationToken);
        if (!sessionExists) throw new NotFoundException("Session", sessionId);

        var baseQuery = _db.Comments
            .Where(c => c.SessionId == sessionId && c.TenantId == _currentUser.TenantId && c.ParentCommentId == null)
            .AsNoTracking();

        var totalCount = await baseQuery.CountAsync(cancellationToken);
        var comments = await baseQuery
            .Include(c => c.Author)
            .Include(c => c.Likes)
            .Include(c => c.Replies).ThenInclude(r => r.Author)
            .Include(c => c.Replies).ThenInclude(r => r.Likes)
            .OrderBy(c => c.CreatedDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<CommentDto>
        {
            Data = comments.Select(c => MapComment(c, _currentUser.UserId)).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<CommentDto> AddSessionCommentAsync(
        Guid sessionId, CreateCommentRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            throw new ValidationException("Content", "Content cannot be empty.");

        var sessionExists = await _db.Sessions
            .AnyAsync(s => s.Id == sessionId && s.TenantId == _currentUser.TenantId, cancellationToken);
        if (!sessionExists) throw new NotFoundException("Session", sessionId);

        if (request.ParentCommentId.HasValue)
        {
            var parentExists = await _db.Comments
                .AnyAsync(c => c.Id == request.ParentCommentId && c.TenantId == _currentUser.TenantId, cancellationToken);
            if (!parentExists) throw new NotFoundException("Comment", request.ParentCommentId.Value);
        }

        var comment = new Comment
        {
            TenantId = _currentUser.TenantId,
            SessionId = sessionId,
            AuthorId = _currentUser.UserId,
            Content = request.Content.Trim(),
            ParentCommentId = request.ParentCommentId,
            CreatedBy = _currentUser.UserId,
            ModifiedBy = _currentUser.UserId
        };

        _db.Comments.Add(comment);
        await _db.SaveChangesAsync(cancellationToken);

        var authorName = await _db.Users
            .Where(u => u.Id == _currentUser.UserId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        return new CommentDto
        {
            Id = comment.Id,
            SessionId = comment.SessionId,
            AuthorId = comment.AuthorId,
            AuthorName = authorName,
            Content = comment.Content,
            ParentCommentId = comment.ParentCommentId,
            CreatedDate = comment.CreatedDate,
        };
    }

    public async Task DeleteCommentAsync(Guid commentId, CancellationToken cancellationToken)
    {
        var comment = await _db.Comments
            .FirstOrDefaultAsync(c => c.Id == commentId && c.TenantId == _currentUser.TenantId, cancellationToken);
        if (comment is null) throw new NotFoundException("Comment", commentId);

        // Only the author, Admin or SuperAdmin can delete
        if (comment.AuthorId != _currentUser.UserId && !_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("You do not have permission to delete this comment.");

        // Soft-delete: preserve thread structure
        comment.IsDeleted = true;
        comment.ModifiedBy = _currentUser.UserId;
        comment.ModifiedOn = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<LikeToggleResult> ToggleCommentLikeAsync(Guid commentId, CancellationToken cancellationToken)
    {
        var comment = await _db.Comments
            .Include(c => c.Likes)
            .FirstOrDefaultAsync(c => c.Id == commentId && c.TenantId == _currentUser.TenantId, cancellationToken);
        if (comment is null) throw new NotFoundException("Comment", commentId);

        var existing = comment.Likes.FirstOrDefault(l => l.UserId == _currentUser.UserId);
        bool liked;
        if (existing is not null)
        {
            _db.Remove(existing);
            liked = false;
        }
        else
        {
            _db.Add(new Like
            {
                TenantId = _currentUser.TenantId,
                UserId = _currentUser.UserId,
                CommentId = commentId,
                CreatedBy = _currentUser.UserId,
                ModifiedBy = _currentUser.UserId
            });
            liked = true;
        }

        await _db.SaveChangesAsync(cancellationToken);
        var likeCount = await _db.Set<Like>().CountAsync(l => l.CommentId == commentId && l.TenantId == _currentUser.TenantId, cancellationToken);
        return new LikeToggleResult { Liked = liked, LikeCount = likeCount };
    }

    public async Task<LikeToggleResult> ToggleSessionLikeAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var sessionExists = await _db.Sessions
            .AnyAsync(s => s.Id == sessionId && s.TenantId == _currentUser.TenantId, cancellationToken);
        if (!sessionExists) throw new NotFoundException("Session", sessionId);

        var existing = await _db.Set<Like>()
            .FirstOrDefaultAsync(l => l.SessionId == sessionId && l.UserId == _currentUser.UserId && l.TenantId == _currentUser.TenantId, cancellationToken);

        bool liked;
        if (existing is not null)
        {
            _db.Remove(existing);
            liked = false;
        }
        else
        {
            _db.Add(new Like
            {
                TenantId = _currentUser.TenantId,
                UserId = _currentUser.UserId,
                SessionId = sessionId,
                CreatedBy = _currentUser.UserId,
                ModifiedBy = _currentUser.UserId
            });
            liked = true;
        }

        await _db.SaveChangesAsync(cancellationToken);
        var likeCount = await _db.Set<Like>().CountAsync(l => l.SessionId == sessionId && l.TenantId == _currentUser.TenantId, cancellationToken);
        return new LikeToggleResult { Liked = liked, LikeCount = likeCount };
    }

    // --- Helper --------------------------------------------------------------
    private static CommentDto MapComment(Comment c, Guid currentUserId)
    {
        return new CommentDto
        {
            Id = c.Id,
            SessionId = c.SessionId,
            KnowledgeAssetId = c.KnowledgeAssetId,
            AuthorId = c.AuthorId,
            AuthorName = c.Author?.FullName ?? string.Empty,
            Content = c.IsDeleted ? string.Empty : c.Content,
            ParentCommentId = c.ParentCommentId,
            LikeCount = c.Likes?.Count ?? 0,
            HasLiked = c.Likes?.Any(l => l.UserId == currentUserId) ?? false,
            IsDeleted = c.IsDeleted,
            CreatedDate = c.CreatedDate,
            Replies = c.Replies?.Select(r => MapComment(r, currentUserId)).ToList() ?? new(),
        };
    }
}
