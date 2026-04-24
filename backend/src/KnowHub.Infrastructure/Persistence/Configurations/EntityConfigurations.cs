using KnowHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowHub.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");
        builder.HasKey(t => t.Id).HasName("PK_Tenants");
        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Slug).IsRequired().HasMaxLength(100);
        builder.HasIndex(t => t.Slug).IsUnique().HasDatabaseName("IX_Tenants_Slug");
    }
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id).HasName("PK_Users");
        builder.Property(u => u.FullName).IsRequired().HasMaxLength(200);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(500);
        builder.Property(u => u.Department).HasMaxLength(150);
        builder.Property(u => u.Designation).HasMaxLength(150);
        builder.Property(u => u.Location).HasMaxLength(150);
        builder.Property(u => u.ProfilePhotoUrl).HasMaxLength(500);
        builder.Property(u => u.RefreshTokenHash).HasMaxLength(500);

        builder.HasIndex(u => new { u.TenantId, u.Email }).IsUnique().HasDatabaseName("IX_Users_TenantId_Email");
        builder.HasIndex(u => u.TenantId).HasDatabaseName("IX_Users_TenantId");

        builder.HasOne<Tenant>().WithMany(t => t.Users).HasForeignKey(u => u.TenantId)
            .HasConstraintName("FK_Users_Tenants_TenantId").OnDelete(DeleteBehavior.Restrict);
    }
}

public class ContributorProfileConfiguration : IEntityTypeConfiguration<ContributorProfile>
{
    public void Configure(EntityTypeBuilder<ContributorProfile> builder)
    {
        builder.ToTable("ContributorProfiles");
        builder.HasKey(c => c.Id).HasName("PK_ContributorProfiles");
        builder.Property(c => c.AverageRating).HasColumnType("decimal(3,2)");
        builder.Property(c => c.EndorsementScore).HasColumnType("decimal(10,2)");

        builder.HasIndex(c => new { c.TenantId, c.UserId }).IsUnique().HasDatabaseName("IX_ContributorProfiles_TenantId_UserId");
        builder.HasOne(c => c.User).WithOne(u => u.ContributorProfile)
            .HasForeignKey<ContributorProfile>(c => c.UserId)
            .HasConstraintName("FK_ContributorProfiles_Users_UserId").OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserSkillConfiguration : IEntityTypeConfiguration<UserSkill>
{
    public void Configure(EntityTypeBuilder<UserSkill> builder)
    {
        builder.ToTable("UserSkills");
        builder.HasKey(s => s.Id).HasName("PK_UserSkills");
        builder.HasIndex(s => new { s.TenantId, s.UserId, s.TagId }).IsUnique().HasDatabaseName("IX_UserSkills_TenantId_UserId_TagId");
        builder.HasOne(s => s.User).WithMany(u => u.Skills).HasForeignKey(s => s.UserId)
            .HasConstraintName("FK_UserSkills_Users_UserId").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(s => s.Tag).WithMany(t => t.UserSkills).HasForeignKey(s => s.TagId)
            .HasConstraintName("FK_UserSkills_Tags_TagId").OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserFollowerConfiguration : IEntityTypeConfiguration<UserFollower>
{
    public void Configure(EntityTypeBuilder<UserFollower> builder)
    {
        builder.ToTable("UserFollowers");
        builder.HasKey(f => f.Id).HasName("PK_UserFollowers");
        builder.HasIndex(f => new { f.TenantId, f.FollowerId, f.FollowedId }).IsUnique().HasDatabaseName("IX_UserFollowers_TenantId_FollowerId_FollowedId");
        builder.HasOne(f => f.Follower).WithMany(u => u.Following).HasForeignKey(f => f.FollowerId)
            .HasConstraintName("FK_UserFollowers_Users_FollowerId").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(f => f.Followed).WithMany(u => u.Followers).HasForeignKey(f => f.FollowedId)
            .HasConstraintName("FK_UserFollowers_Users_FollowedId").OnDelete(DeleteBehavior.Restrict);
    }
}

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(c => c.Id).HasName("PK_Categories");
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Description).HasMaxLength(500);
        builder.Property(c => c.IconName).HasMaxLength(100);
        builder.HasIndex(c => new { c.TenantId, c.Name }).IsUnique().HasDatabaseName("IX_Categories_TenantId_Name");
        builder.HasIndex(c => c.TenantId).HasDatabaseName("IX_Categories_TenantId");
    }
}

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags");
        builder.HasKey(t => t.Id).HasName("PK_Tags");
        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);
        builder.Property(t => t.Slug).IsRequired().HasMaxLength(120);
        builder.HasIndex(t => new { t.TenantId, t.Slug }).IsUnique().HasDatabaseName("IX_Tags_TenantId_Slug");
        builder.HasIndex(t => t.TenantId).HasDatabaseName("IX_Tags_TenantId");
    }
}

public class SessionProposalConfiguration : IEntityTypeConfiguration<SessionProposal>
{
    public void Configure(EntityTypeBuilder<SessionProposal> builder)
    {
        builder.ToTable("SessionProposals");
        builder.HasKey(p => p.Id).HasName("PK_SessionProposals");
        builder.Property(p => p.Title).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Topic).IsRequired().HasMaxLength(300);
        builder.Property(p => p.Description).IsRequired().HasMaxLength(5000);
        builder.Property(p => p.LearningOutcomes).HasMaxLength(2000);
        builder.Property(p => p.TargetAudience).HasMaxLength(500);
        builder.Property(p => p.DepartmentRelevance).HasMaxLength(300);
        builder.Property(p => p.RelatedProject).HasMaxLength(200);

        builder.HasIndex(p => new { p.TenantId, p.Status }).HasDatabaseName("IX_SessionProposals_TenantId_Status");
        builder.HasIndex(p => new { p.TenantId, p.ProposerId }).HasDatabaseName("IX_SessionProposals_TenantId_ProposerId");
        builder.HasIndex(p => p.TenantId).HasDatabaseName("IX_SessionProposals_TenantId");

        builder.HasOne(p => p.Proposer).WithMany(u => u.Proposals).HasForeignKey(p => p.ProposerId)
            .HasConstraintName("FK_SessionProposals_Users_ProposerId").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.Category).WithMany(c => c.Proposals).HasForeignKey(p => p.CategoryId)
            .HasConstraintName("FK_SessionProposals_Categories_CategoryId").OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProposalApprovalConfiguration : IEntityTypeConfiguration<ProposalApproval>
{
    public void Configure(EntityTypeBuilder<ProposalApproval> builder)
    {
        builder.ToTable("ProposalApprovals");
        builder.HasKey(a => a.Id).HasName("PK_ProposalApprovals");
        builder.Property(a => a.Comment).HasMaxLength(1000);

        builder.HasIndex(a => new { a.TenantId, a.ProposalId }).HasDatabaseName("IX_ProposalApprovals_TenantId_ProposalId");
        builder.HasOne(a => a.Proposal).WithMany(p => p.Approvals).HasForeignKey(a => a.ProposalId)
            .HasConstraintName("FK_ProposalApprovals_SessionProposals_ProposalId").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(a => a.Approver).WithMany().HasForeignKey(a => a.ApproverId)
            .HasConstraintName("FK_ProposalApprovals_Users_ApproverId").OnDelete(DeleteBehavior.Restrict);
    }
}

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("Sessions");
        builder.HasKey(s => s.Id).HasName("PK_Sessions");
        builder.Property(s => s.Title).IsRequired().HasMaxLength(200);
        builder.Property(s => s.MeetingLink).IsRequired().HasMaxLength(500);
        builder.Property(s => s.RecordingUrl).HasMaxLength(500);
        builder.Property(s => s.Description).HasMaxLength(5000);

        builder.HasIndex(s => new { s.TenantId, s.Status }).HasDatabaseName("IX_Sessions_TenantId_Status");
        builder.HasIndex(s => new { s.TenantId, s.ScheduledAt }).HasDatabaseName("IX_Sessions_TenantId_ScheduledAt");
        builder.HasIndex(s => new { s.TenantId, s.SpeakerId }).HasDatabaseName("IX_Sessions_TenantId_SpeakerId");
        builder.HasIndex(s => new { s.TenantId, s.CategoryId }).HasDatabaseName("IX_Sessions_TenantId_CategoryId");
        builder.HasIndex(s => s.TenantId).HasDatabaseName("IX_Sessions_TenantId");

        builder.HasOne(s => s.Proposal).WithOne(p => p.Session).HasForeignKey<Session>(s => s.ProposalId)
            .HasConstraintName("FK_Sessions_SessionProposals_ProposalId").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Speaker).WithMany().HasForeignKey(s => s.SpeakerId)
            .HasConstraintName("FK_Sessions_Users_SpeakerId").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Category).WithMany(c => c.Sessions).HasForeignKey(s => s.CategoryId)
            .HasConstraintName("FK_Sessions_Categories_CategoryId").OnDelete(DeleteBehavior.Restrict);
    }
}

public class SessionTagConfiguration : IEntityTypeConfiguration<SessionTag>
{
    public void Configure(EntityTypeBuilder<SessionTag> builder)
    {
        builder.ToTable("SessionTags");
        builder.HasKey(st => st.Id).HasName("PK_SessionTags");
        builder.HasIndex(st => new { st.TenantId, st.SessionId, st.TagId }).IsUnique().HasDatabaseName("IX_SessionTags_TenantId_SessionId_TagId");
        builder.HasOne(st => st.Session).WithMany(s => s.SessionTags).HasForeignKey(st => st.SessionId)
            .HasConstraintName("FK_SessionTags_Sessions_SessionId").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(st => st.Tag).WithMany(t => t.SessionTags).HasForeignKey(st => st.TagId)
            .HasConstraintName("FK_SessionTags_Tags_TagId").OnDelete(DeleteBehavior.Cascade);
    }
}

public class SessionMaterialConfiguration : IEntityTypeConfiguration<SessionMaterial>
{
    public void Configure(EntityTypeBuilder<SessionMaterial> builder)
    {
        builder.ToTable("SessionMaterials");
        builder.HasKey(m => m.Id).HasName("PK_SessionMaterials");
        builder.Property(m => m.Title).IsRequired().HasMaxLength(200);
        builder.Property(m => m.Url).IsRequired().HasMaxLength(500);

        builder.HasIndex(m => new { m.TenantId, m.SessionId }).HasDatabaseName("IX_SessionMaterials_TenantId_SessionId");
        builder.HasOne(m => m.Session).WithMany(s => s.Materials).HasForeignKey(m => m.SessionId)
            .HasConstraintName("FK_SessionMaterials_Sessions_SessionId").OnDelete(DeleteBehavior.Cascade).IsRequired(false);
        builder.HasOne(m => m.Proposal).WithMany(p => p.Materials).HasForeignKey(m => m.ProposalId)
            .HasConstraintName("FK_SessionMaterials_SessionProposals_ProposalId").OnDelete(DeleteBehavior.Cascade).IsRequired(false);
    }
}

public class SessionRegistrationConfiguration : IEntityTypeConfiguration<SessionRegistration>
{
    public void Configure(EntityTypeBuilder<SessionRegistration> builder)
    {
        builder.ToTable("SessionRegistrations");
        builder.HasKey(r => r.Id).HasName("PK_SessionRegistrations");
        builder.HasIndex(r => new { r.TenantId, r.SessionId, r.ParticipantId }).IsUnique().HasDatabaseName("IX_SessionRegistrations_TenantId_SessionId_ParticipantId");
        builder.HasIndex(r => new { r.TenantId, r.SessionId }).HasDatabaseName("IX_SessionRegistrations_TenantId_SessionId");
        builder.HasOne(r => r.Session).WithMany(s => s.Registrations).HasForeignKey(r => r.SessionId)
            .HasConstraintName("FK_SessionRegistrations_Sessions_SessionId").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(r => r.Participant).WithMany(u => u.Registrations).HasForeignKey(r => r.ParticipantId)
            .HasConstraintName("FK_SessionRegistrations_Users_ParticipantId").OnDelete(DeleteBehavior.Restrict);
    }
}

public class KnowledgeAssetConfiguration : IEntityTypeConfiguration<KnowledgeAsset>
{
    public void Configure(EntityTypeBuilder<KnowledgeAsset> builder)
    {
        builder.ToTable("KnowledgeAssets");
        builder.HasKey(a => a.Id).HasName("PK_KnowledgeAssets");
        builder.Property(a => a.Title).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Url).IsRequired().HasMaxLength(500);
        builder.Property(a => a.Description).HasMaxLength(2000);

        builder.HasIndex(a => new { a.TenantId, a.SessionId }).HasDatabaseName("IX_KnowledgeAssets_TenantId_SessionId");
        builder.HasOne(a => a.Session).WithMany(s => s.KnowledgeAssets).HasForeignKey(a => a.SessionId)
            .HasConstraintName("FK_KnowledgeAssets_Sessions_SessionId").OnDelete(DeleteBehavior.SetNull).IsRequired(false);
    }
}

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("Comments");
        builder.HasKey(c => c.Id).HasName("PK_Comments");
        builder.Property(c => c.Content).IsRequired().HasMaxLength(2000);

        builder.HasIndex(c => c.TenantId).HasDatabaseName("IX_Comments_TenantId");
        builder.HasIndex(c => new { c.TenantId, c.SessionId }).HasDatabaseName("IX_Comments_TenantId_SessionId");
        builder.HasIndex(c => new { c.TenantId, c.KnowledgeAssetId }).HasDatabaseName("IX_Comments_TenantId_AssetId");

        builder.HasOne(c => c.Author).WithMany().HasForeignKey(c => c.AuthorId)
            .HasConstraintName("FK_Comments_Users_AuthorId").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.Session).WithMany(s => s.Comments).HasForeignKey(c => c.SessionId)
            .HasConstraintName("FK_Comments_Sessions_SessionId").OnDelete(DeleteBehavior.Cascade).IsRequired(false);
        builder.HasOne(c => c.KnowledgeAsset).WithMany().HasForeignKey(c => c.KnowledgeAssetId)
            .HasConstraintName("FK_Comments_KnowledgeAssets_KnowledgeAssetId").OnDelete(DeleteBehavior.Cascade).IsRequired(false);
        builder.HasOne(c => c.ParentComment).WithMany(c => c.Replies).HasForeignKey(c => c.ParentCommentId)
            .HasConstraintName("FK_Comments_Comments_ParentCommentId").OnDelete(DeleteBehavior.Restrict).IsRequired(false);
    }
}

public class LikeConfiguration : IEntityTypeConfiguration<Like>
{
    public void Configure(EntityTypeBuilder<Like> builder)
    {
        builder.ToTable("Likes");
        builder.HasKey(l => l.Id).HasName("PK_Likes");

        builder.HasIndex(l => l.TenantId).HasDatabaseName("IX_Likes_TenantId");
        builder.HasIndex(l => new { l.TenantId, l.UserId, l.SessionId })
            .HasDatabaseName("IX_Likes_TenantId_UserId_SessionId");
        builder.HasIndex(l => new { l.TenantId, l.UserId, l.CommentId })
            .HasDatabaseName("IX_Likes_TenantId_UserId_CommentId");

        builder.HasOne(l => l.User).WithMany().HasForeignKey(l => l.UserId)
            .HasConstraintName("FK_Likes_Users_UserId").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(l => l.Session).WithMany().HasForeignKey(l => l.SessionId)
            .HasConstraintName("FK_Likes_Sessions_SessionId").OnDelete(DeleteBehavior.Cascade).IsRequired(false);
        builder.HasOne(l => l.KnowledgeAsset).WithMany().HasForeignKey(l => l.KnowledgeAssetId)
            .HasConstraintName("FK_Likes_KnowledgeAssets_KnowledgeAssetId").OnDelete(DeleteBehavior.Cascade).IsRequired(false);
        builder.HasOne(l => l.Comment).WithMany(c => c.Likes).HasForeignKey(l => l.CommentId)
            .HasConstraintName("FK_Likes_Comments_CommentId").OnDelete(DeleteBehavior.Cascade).IsRequired(false);
        builder.HasOne(l => l.KnowledgeRequest).WithMany(k => k.Likes).HasForeignKey(l => l.KnowledgeRequestId)
            .HasConstraintName("FK_Likes_KnowledgeRequests_KnowledgeRequestId").OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);
    }
}

public class SessionRatingConfiguration : IEntityTypeConfiguration<SessionRating>
{
    public void Configure(EntityTypeBuilder<SessionRating> builder)
    {
        builder.ToTable("SessionRatings");
        builder.HasKey(r => r.Id).HasName("PK_SessionRatings");
        builder.Property(r => r.FeedbackText).HasMaxLength(2000);
        builder.Property(r => r.NextSessionSuggestion).HasMaxLength(500);

        builder.HasIndex(r => new { r.TenantId, r.SessionId, r.RaterId }).IsUnique().HasDatabaseName("IX_SessionRatings_TenantId_SessionId_RaterId");
        builder.HasOne(r => r.Session).WithMany(s => s.Ratings).HasForeignKey(r => r.SessionId)
            .HasConstraintName("FK_SessionRatings_Sessions_SessionId").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(r => r.Rater).WithMany().HasForeignKey(r => r.RaterId)
            .HasConstraintName("FK_SessionRatings_Users_RaterId").OnDelete(DeleteBehavior.Restrict);
    }
}

public class BadgeConfiguration : IEntityTypeConfiguration<Badge>
{
    public void Configure(EntityTypeBuilder<Badge> builder)
    {
        builder.ToTable("Badges");
        builder.HasKey(b => b.Id).HasName("PK_Badges");
        builder.Property(b => b.Name).IsRequired().HasMaxLength(100);
        builder.Property(b => b.Description).IsRequired().HasMaxLength(500);
        builder.Property(b => b.Criteria).HasMaxLength(500);
        builder.Property(b => b.BadgeCategory).IsRequired().HasMaxLength(50);
        builder.HasIndex(b => b.TenantId).HasDatabaseName("IX_Badges_TenantId");
    }
}

public class UserBadgeConfiguration : IEntityTypeConfiguration<UserBadge>
{
    public void Configure(EntityTypeBuilder<UserBadge> builder)
    {
        builder.ToTable("UserBadges");
        builder.HasKey(ub => ub.Id).HasName("PK_UserBadges");
        builder.Property(ub => ub.AwardReason).HasMaxLength(500);

        builder.HasIndex(ub => new { ub.TenantId, ub.UserId }).HasDatabaseName("IX_UserBadges_TenantId_UserId");
        builder.HasOne(ub => ub.User).WithMany(u => u.Badges).HasForeignKey(ub => ub.UserId)
            .HasConstraintName("FK_UserBadges_Users_UserId").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(ub => ub.Badge).WithMany(b => b.UserBadges).HasForeignKey(ub => ub.BadgeId)
            .HasConstraintName("FK_UserBadges_Badges_BadgeId").OnDelete(DeleteBehavior.Restrict);
    }
}

public class CommunityConfiguration : IEntityTypeConfiguration<Community>
{
    public void Configure(EntityTypeBuilder<Community> builder)
    {
        builder.ToTable("Communities");
        builder.HasKey(c => c.Id).HasName("PK_Communities");
        builder.Property(c => c.Name).IsRequired().HasMaxLength(150);
        builder.Property(c => c.Slug).IsRequired().HasMaxLength(180);
        builder.Property(c => c.Description).HasMaxLength(1000);
        builder.HasIndex(c => new { c.TenantId, c.Slug }).IsUnique().HasDatabaseName("IX_Communities_TenantId_Slug");
        builder.HasIndex(c => c.TenantId).HasDatabaseName("IX_Communities_TenantId");
    }
}

public class CommunityMemberConfiguration : IEntityTypeConfiguration<CommunityMember>
{
    public void Configure(EntityTypeBuilder<CommunityMember> builder)
    {
        builder.ToTable("CommunityMembers");
        builder.HasKey(m => m.Id).HasName("PK_CommunityMembers");
        builder.HasIndex(m => new { m.TenantId, m.CommunityId, m.UserId }).IsUnique().HasDatabaseName("IX_CommunityMembers_TenantId_CommunityId_UserId");
        builder.HasOne(m => m.Community).WithMany(c => c.Members).HasForeignKey(m => m.CommunityId)
            .HasConstraintName("FK_CommunityMembers_Communities_CommunityId").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(m => m.User).WithMany(u => u.CommunityMemberships).HasForeignKey(m => m.UserId)
            .HasConstraintName("FK_CommunityMembers_Users_UserId").OnDelete(DeleteBehavior.Cascade);
    }
}

public class KnowledgeRequestConfiguration : IEntityTypeConfiguration<KnowledgeRequest>
{
    public void Configure(EntityTypeBuilder<KnowledgeRequest> builder)
    {
        builder.ToTable("KnowledgeRequests");
        builder.HasKey(r => r.Id).HasName("PK_KnowledgeRequests");
        builder.Property(r => r.Title).IsRequired().HasMaxLength(200);
        builder.Property(r => r.Description).IsRequired().HasMaxLength(2000);

        builder.HasIndex(r => new { r.TenantId, r.Status }).HasDatabaseName("IX_KnowledgeRequests_TenantId_Status");
        builder.HasOne(r => r.Requester).WithMany().HasForeignKey(r => r.RequesterId)
            .HasConstraintName("FK_KnowledgeRequests_Users_RequesterId").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.Category).WithMany().HasForeignKey(r => r.CategoryId)
            .HasConstraintName("FK_KnowledgeRequests_Categories_CategoryId").OnDelete(DeleteBehavior.SetNull).IsRequired(false);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(n => n.Id).HasName("PK_Notifications");
        builder.Property(n => n.Title).IsRequired().HasMaxLength(200);
        builder.Property(n => n.Body).IsRequired().HasMaxLength(1000);
        builder.Property(n => n.RelatedEntityType).HasMaxLength(50);

        builder.HasIndex(n => new { n.TenantId, n.UserId, n.IsRead }).HasDatabaseName("IX_Notifications_TenantId_UserId_IsRead");
        builder.HasOne(n => n.User).WithMany(u => u.Notifications).HasForeignKey(n => n.UserId)
            .HasConstraintName("FK_Notifications_Users_UserId").OnDelete(DeleteBehavior.Cascade);
    }
}
