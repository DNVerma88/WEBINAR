using KnowHub.Application.Contracts;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowHub.Infrastructure.Services;

public class PostReactionService : IPostReactionService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public PostReactionService(KnowHubDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PostReactionResultDto> ToggleReactionAsync(Guid postId, ReactionType type, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId;
        var userId = _currentUser.UserId;

        var post = await _db.CommunityPosts
            .Where(p => p.Id == postId && p.TenantId == tenantId)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Post", postId);

        var existing = await _db.PostReactions
            .Where(r => r.PostId == postId && r.UserId == userId && r.ReactionType == type && r.TenantId == tenantId)
            .FirstOrDefaultAsync(ct);

        if (existing is null)
        {
            _db.PostReactions.Add(new PostReaction
            {
                TenantId = tenantId,
                PostId = postId,
                UserId = userId,
                ReactionType = type,
                CreatedBy = userId,
                ModifiedBy = userId
            });
            post.ReactionCount++;
        }
        else
        {
            _db.PostReactions.Remove(existing);
            post.ReactionCount = Math.Max(0, post.ReactionCount - 1);
        }

        post.ModifiedOn = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return await GetReactionsAsync(postId, ct);
    }

    public async Task<PostReactionResultDto> GetReactionsAsync(Guid postId, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId;
        var userId = _currentUser.UserId;

        var reactions = await _db.PostReactions
            .Where(r => r.PostId == postId && r.TenantId == tenantId)
            .AsNoTracking()
            .ToListAsync(ct);

        return new PostReactionResultDto
        {
            PostId = postId,
            ReactionCounts = reactions
                .GroupBy(r => r.ReactionType)
                .ToDictionary(g => g.Key, g => g.Count()),
            UserReactions = reactions
                .Where(r => r.UserId == userId)
                .Select(r => r.ReactionType)
                .ToList()
        };
    }
}
