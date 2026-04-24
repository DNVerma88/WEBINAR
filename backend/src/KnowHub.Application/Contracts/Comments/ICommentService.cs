using KnowHub.Application.Models;

namespace KnowHub.Application.Contracts;

public class CommentDto
{
    public Guid Id { get; set; }
    public Guid? SessionId { get; set; }
    public Guid? KnowledgeAssetId { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Guid? ParentCommentId { get; set; }
    public int LikeCount { get; set; }
    public bool HasLiked { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedDate { get; set; }
    public List<CommentDto> Replies { get; set; } = new();
}

public class CreateCommentRequest
{
    public string Content { get; set; } = string.Empty;
    public Guid? ParentCommentId { get; set; }
}

public class LikeToggleResult
{
    public bool Liked { get; set; }
    public int LikeCount { get; set; }
}

public interface ICommentService
{
    Task<PagedResult<CommentDto>> GetSessionCommentsAsync(Guid sessionId, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<CommentDto> AddSessionCommentAsync(Guid sessionId, CreateCommentRequest request, CancellationToken cancellationToken);
    Task DeleteCommentAsync(Guid commentId, CancellationToken cancellationToken);
    Task<LikeToggleResult> ToggleCommentLikeAsync(Guid commentId, CancellationToken cancellationToken);
    Task<LikeToggleResult> ToggleSessionLikeAsync(Guid sessionId, CancellationToken cancellationToken);
}
