using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowHub.Infrastructure.Persistence.Configurations;

public class PostSeriesConfiguration : IEntityTypeConfiguration<PostSeries>
{
    public void Configure(EntityTypeBuilder<PostSeries> builder)
    {
        builder.ToTable("PostSeries");
        builder.HasKey(s => s.Id).HasName("PK_PostSeries");

        builder.Property(s => s.Title).IsRequired().HasMaxLength(300);
        builder.Property(s => s.Slug).IsRequired().HasMaxLength(300);
        builder.Property(s => s.Description).HasColumnType("text");

        builder.HasIndex(s => new { s.TenantId, s.CommunityId, s.Slug })
            .IsUnique().HasDatabaseName("IX_PostSeries_TenantId_CommunityId_Slug");

        builder.HasOne(s => s.Community).WithMany()
            .HasForeignKey(s => s.CommunityId)
            .HasConstraintName("FK_PostSeries_Communities").OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Author).WithMany()
            .HasForeignKey(s => s.AuthorId)
            .HasConstraintName("FK_PostSeries_Users_Author").OnDelete(DeleteBehavior.Restrict);
    }
}

public class CommunityPostConfiguration : IEntityTypeConfiguration<CommunityPost>
{
    public void Configure(EntityTypeBuilder<CommunityPost> builder)
    {
        builder.ToTable("CommunityPosts");
        builder.HasKey(p => p.Id).HasName("PK_CommunityPosts");

        builder.Property(p => p.Title).IsRequired().HasMaxLength(300);
        builder.Property(p => p.Slug).IsRequired().HasMaxLength(300);
        builder.Property(p => p.ContentMarkdown).IsRequired().HasColumnType("text");
        builder.Property(p => p.ContentHtml).IsRequired().HasColumnType("text");
        builder.Property(p => p.CoverImageUrl).HasMaxLength(500);
        builder.Property(p => p.CanonicalUrl).HasMaxLength(1000);
        builder.Property(p => p.PostType).HasConversion<short>();
        builder.Property(p => p.Status).HasConversion<short>();

        builder.HasIndex(p => new { p.TenantId, p.CommunityId, p.Slug })
            .IsUnique().HasDatabaseName("IX_CommunityPosts_TenantId_CommunityId_Slug");
        builder.HasIndex(p => new { p.TenantId, p.CommunityId, p.Status, p.PublishedAt })
            .HasDatabaseName("IX_CommunityPosts_TenantId_CommunityId_Status_PublishedAt");
        builder.HasIndex(p => new { p.TenantId, p.AuthorId })
            .HasDatabaseName("IX_CommunityPosts_TenantId_AuthorId");

        builder.HasOne(p => p.Community).WithMany()
            .HasForeignKey(p => p.CommunityId)
            .HasConstraintName("FK_CommunityPosts_Communities").OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Author).WithMany()
            .HasForeignKey(p => p.AuthorId)
            .HasConstraintName("FK_CommunityPosts_Users_Author").OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Series).WithMany(s => s.Posts)
            .HasForeignKey(p => p.SeriesId)
            .HasConstraintName("FK_CommunityPosts_PostSeries").OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }
}

public class CommunityPostTagConfiguration : IEntityTypeConfiguration<CommunityPostTag>
{
    public void Configure(EntityTypeBuilder<CommunityPostTag> builder)
    {
        builder.ToTable("CommunityPostTags");
        builder.HasKey(pt => new { pt.PostId, pt.TagId }).HasName("PK_CommunityPostTags");

        builder.HasIndex(pt => pt.TagId).HasDatabaseName("IX_CommunityPostTags_TagId");

        builder.HasOne(pt => pt.Post).WithMany(p => p.Tags)
            .HasForeignKey(pt => pt.PostId)
            .HasConstraintName("FK_CommunityPostTags_Post").OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pt => pt.Tag).WithMany(t => t.CommunityPostTags)
            .HasForeignKey(pt => pt.TagId)
            .HasConstraintName("FK_CommunityPostTags_Tag").OnDelete(DeleteBehavior.Cascade);
    }
}

public class PostReactionConfiguration : IEntityTypeConfiguration<PostReaction>
{
    public void Configure(EntityTypeBuilder<PostReaction> builder)
    {
        builder.ToTable("PostReactions");
        builder.HasKey(r => r.Id).HasName("PK_PostReactions");
        builder.Property(r => r.ReactionType).HasConversion<short>();

        builder.HasIndex(r => new { r.TenantId, r.PostId, r.UserId, r.ReactionType })
            .IsUnique().HasDatabaseName("UQ_PostReactions_User_Post_Type");
        builder.HasIndex(r => r.PostId).HasDatabaseName("IX_PostReactions_PostId");

        builder.HasOne(r => r.Post).WithMany(p => p.Reactions)
            .HasForeignKey(r => r.PostId)
            .HasConstraintName("FK_PostReactions_Post").OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.User).WithMany()
            .HasForeignKey(r => r.UserId)
            .HasConstraintName("FK_PostReactions_Users").OnDelete(DeleteBehavior.Restrict);
    }
}

public class PostCommentConfiguration : IEntityTypeConfiguration<PostComment>
{
    public void Configure(EntityTypeBuilder<PostComment> builder)
    {
        builder.ToTable("PostComments");
        builder.HasKey(c => c.Id).HasName("PK_PostComments");
        builder.Property(c => c.BodyMarkdown).IsRequired().HasColumnType("text");

        builder.HasIndex(c => new { c.PostId, c.CreatedDate })
            .HasDatabaseName("IX_PostComments_PostId_CreatedDate");

        builder.HasOne(c => c.Post).WithMany(p => p.Comments)
            .HasForeignKey(c => c.PostId)
            .HasConstraintName("FK_PostComments_Post").OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Author).WithMany()
            .HasForeignKey(c => c.AuthorId)
            .HasConstraintName("FK_PostComments_Users_Author").OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.ParentComment).WithMany(c => c.Replies)
            .HasForeignKey(c => c.ParentCommentId)
            .HasConstraintName("FK_PostComments_Parent").OnDelete(DeleteBehavior.ClientSetNull)
            .IsRequired(false);
    }
}

public class PostBookmarkConfiguration : IEntityTypeConfiguration<PostBookmark>
{
    public void Configure(EntityTypeBuilder<PostBookmark> builder)
    {
        builder.ToTable("PostBookmarks");
        builder.HasKey(b => b.Id).HasName("PK_PostBookmarks");

        builder.HasIndex(b => new { b.TenantId, b.UserId, b.PostId })
            .IsUnique().HasDatabaseName("UQ_PostBookmarks_User_Post");
        builder.HasIndex(b => b.UserId).HasDatabaseName("IX_PostBookmarks_UserId");

        builder.HasOne(b => b.Post).WithMany(p => p.Bookmarks)
            .HasForeignKey(b => b.PostId)
            .HasConstraintName("FK_PostBookmarks_Post").OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.User).WithMany()
            .HasForeignKey(b => b.UserId)
            .HasConstraintName("FK_PostBookmarks_Users").OnDelete(DeleteBehavior.Restrict);
    }
}

public class UserTagFollowConfiguration : IEntityTypeConfiguration<UserTagFollow>
{
    public void Configure(EntityTypeBuilder<UserTagFollow> builder)
    {
        builder.ToTable("UserTagFollows");
        builder.HasKey(f => f.Id).HasName("PK_UserTagFollows");

        builder.HasIndex(f => f.FollowerId).HasDatabaseName("IX_UserTagFollows_FollowerId");

        builder.HasOne(f => f.Follower).WithMany()
            .HasForeignKey(f => f.FollowerId)
            .HasConstraintName("FK_UserTagFollows_Follower").OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.FollowedUser).WithMany()
            .HasForeignKey(f => f.FollowedUserId)
            .HasConstraintName("FK_UserTagFollows_FollowedUser").OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        builder.HasOne(f => f.FollowedTag).WithMany()
            .HasForeignKey(f => f.FollowedTagId)
            .HasConstraintName("FK_UserTagFollows_FollowedTag").OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);
    }
}
