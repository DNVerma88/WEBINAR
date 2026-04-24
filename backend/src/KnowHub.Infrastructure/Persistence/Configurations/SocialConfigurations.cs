using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowHub.Infrastructure.Persistence.Configurations;

public class SkillEndorsementConfiguration : IEntityTypeConfiguration<SkillEndorsement>
{
    public void Configure(EntityTypeBuilder<SkillEndorsement> builder)
    {
        builder.ToTable("SkillEndorsements");
        builder.HasKey(e => e.Id).HasName("PK_SkillEndorsements");

        builder.HasIndex(e => new { e.TenantId, e.EndorserId, e.EndorseeId, e.TagId, e.SessionId }).IsUnique()
            .HasDatabaseName("IX_SkillEndorsements_Unique");
        builder.HasIndex(e => e.TenantId).HasDatabaseName("IX_SkillEndorsements_TenantId");
        builder.HasIndex(e => new { e.TenantId, e.EndorseeId }).HasDatabaseName("IX_SkillEndorsements_TenantId_EndorseeId");

        builder.HasOne(e => e.Endorser).WithMany().HasForeignKey(e => e.EndorserId)
            .HasConstraintName("FK_SkillEndorsements_Users_EndorserId").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Endorsee).WithMany().HasForeignKey(e => e.EndorseeId)
            .HasConstraintName("FK_SkillEndorsements_Users_EndorseeId").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Tag).WithMany().HasForeignKey(e => e.TagId)
            .HasConstraintName("FK_SkillEndorsements_Tags_TagId").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Session).WithMany().HasForeignKey(e => e.SessionId)
            .HasConstraintName("FK_SkillEndorsements_Sessions_SessionId").OnDelete(DeleteBehavior.Restrict);
    }
}

public class MentorMenteeConfiguration : IEntityTypeConfiguration<MentorMentee>
{
    public void Configure(EntityTypeBuilder<MentorMentee> builder)
    {
        builder.ToTable("MentorMentees");
        builder.HasKey(m => m.Id).HasName("PK_MentorMentees");
        builder.Property(m => m.Status).HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(m => m.MatchReason).HasMaxLength(500);

        builder.HasIndex(m => m.TenantId).HasDatabaseName("IX_MentorMentees_TenantId");
        builder.HasIndex(m => new { m.TenantId, m.MentorId }).HasDatabaseName("IX_MentorMentees_TenantId_MentorId");
        builder.HasIndex(m => new { m.TenantId, m.MenteeId }).HasDatabaseName("IX_MentorMentees_TenantId_MenteeId");

        builder.HasOne(m => m.Mentor).WithMany().HasForeignKey(m => m.MentorId)
            .HasConstraintName("FK_MentorMentees_Users_MentorId").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.Mentee).WithMany().HasForeignKey(m => m.MenteeId)
            .HasConstraintName("FK_MentorMentees_Users_MenteeId").OnDelete(DeleteBehavior.Restrict);
    }
}

public class CommunityWikiPageConfiguration : IEntityTypeConfiguration<CommunityWikiPage>
{
    public void Configure(EntityTypeBuilder<CommunityWikiPage> builder)
    {
        builder.ToTable("CommunityWikiPages");
        builder.HasKey(p => p.Id).HasName("PK_CommunityWikiPages");
        builder.Property(p => p.Title).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Slug).IsRequired().HasMaxLength(200);

        builder.HasIndex(p => new { p.TenantId, p.CommunityId, p.Slug }).IsUnique()
            .HasDatabaseName("IX_CommunityWikiPages_TenantId_CommunityId_Slug");
        builder.HasIndex(p => p.TenantId).HasDatabaseName("IX_CommunityWikiPages_TenantId");
        builder.HasIndex(p => new { p.TenantId, p.CommunityId }).HasDatabaseName("IX_CommunityWikiPages_TenantId_CommunityId");

        builder.HasOne(p => p.Community).WithMany().HasForeignKey(p => p.CommunityId)
            .HasConstraintName("FK_CommunityWikiPages_Communities_CommunityId").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(p => p.Author).WithMany().HasForeignKey(p => p.AuthorId)
            .HasConstraintName("FK_CommunityWikiPages_Users_AuthorId").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.ParentPage).WithMany(p => p.ChildPages).HasForeignKey(p => p.ParentPageId)
            .HasConstraintName("FK_CommunityWikiPages_CommunityWikiPages_ParentPageId").OnDelete(DeleteBehavior.Restrict);
    }
}

public class KnowledgeBundleConfiguration : IEntityTypeConfiguration<KnowledgeBundle>
{
    public void Configure(EntityTypeBuilder<KnowledgeBundle> builder)
    {
        builder.ToTable("KnowledgeBundles");
        builder.HasKey(b => b.Id).HasName("PK_KnowledgeBundles");
        builder.Property(b => b.Title).IsRequired().HasMaxLength(200);
        builder.Property(b => b.CoverImageUrl).HasMaxLength(500);

        builder.HasIndex(b => b.TenantId).HasDatabaseName("IX_KnowledgeBundles_TenantId");
        builder.HasIndex(b => new { b.TenantId, b.CategoryId }).HasDatabaseName("IX_KnowledgeBundles_TenantId_CategoryId");

        builder.HasOne(b => b.CreatedByUser).WithMany().HasForeignKey(b => b.CreatedByUserId)
            .HasConstraintName("FK_KnowledgeBundles_Users_CreatedByUserId").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(b => b.Category).WithMany().HasForeignKey(b => b.CategoryId)
            .HasConstraintName("FK_KnowledgeBundles_Categories_CategoryId").OnDelete(DeleteBehavior.Restrict);
    }
}

public class KnowledgeBundleItemConfiguration : IEntityTypeConfiguration<KnowledgeBundleItem>
{
    public void Configure(EntityTypeBuilder<KnowledgeBundleItem> builder)
    {
        builder.ToTable("KnowledgeBundleItems");
        builder.HasKey(i => i.Id).HasName("PK_KnowledgeBundleItems");
        builder.Property(i => i.Notes).HasMaxLength(500);

        builder.HasIndex(i => new { i.TenantId, i.BundleId, i.KnowledgeAssetId }).IsUnique()
            .HasDatabaseName("IX_KnowledgeBundleItems_TenantId_BundleId_AssetId");
        builder.HasIndex(i => i.TenantId).HasDatabaseName("IX_KnowledgeBundleItems_TenantId");
        builder.HasIndex(i => new { i.TenantId, i.BundleId }).HasDatabaseName("IX_KnowledgeBundleItems_TenantId_BundleId");

        builder.HasOne(i => i.Bundle).WithMany(b => b.Items).HasForeignKey(i => i.BundleId)
            .HasConstraintName("FK_KnowledgeBundleItems_KnowledgeBundles_BundleId").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(i => i.KnowledgeAsset).WithMany().HasForeignKey(i => i.KnowledgeAssetId)
            .HasConstraintName("FK_KnowledgeBundleItems_KnowledgeAssets_KnowledgeAssetId").OnDelete(DeleteBehavior.Restrict);
    }
}

