using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowHub.Infrastructure.Persistence.Configurations;

public class LearningPathConfiguration : IEntityTypeConfiguration<LearningPath>
{
    public void Configure(EntityTypeBuilder<LearningPath> builder)
    {
        builder.ToTable("LearningPaths");
        builder.HasKey(p => p.Id).HasName("PK_LearningPaths");
        builder.Property(p => p.Title).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Slug).IsRequired().HasMaxLength(200);
        builder.Property(p => p.DifficultyLevel).HasConversion<string>().IsRequired().HasMaxLength(20);

        builder.HasIndex(p => new { p.TenantId, p.Slug }).IsUnique().HasDatabaseName("IX_LearningPaths_TenantId_Slug");
        builder.HasIndex(p => p.TenantId).HasDatabaseName("IX_LearningPaths_TenantId");
        builder.HasIndex(p => new { p.TenantId, p.CategoryId }).HasDatabaseName("IX_LearningPaths_TenantId_CategoryId");

        builder.HasOne(p => p.Category).WithMany().HasForeignKey(p => p.CategoryId)
            .HasConstraintName("FK_LearningPaths_Categories_CategoryId").OnDelete(DeleteBehavior.Restrict);
    }
}

public class LearningPathItemConfiguration : IEntityTypeConfiguration<LearningPathItem>
{
    public void Configure(EntityTypeBuilder<LearningPathItem> builder)
    {
        builder.ToTable("LearningPathItems");
        builder.HasKey(i => i.Id).HasName("PK_LearningPathItems");
        builder.Property(i => i.ItemType).HasConversion<string>().IsRequired().HasMaxLength(20);

        builder.HasIndex(i => i.TenantId).HasDatabaseName("IX_LearningPathItems_TenantId");
        builder.HasIndex(i => new { i.TenantId, i.LearningPathId }).HasDatabaseName("IX_LearningPathItems_TenantId_LearningPathId");

        builder.HasOne(i => i.LearningPath).WithMany(p => p.Items).HasForeignKey(i => i.LearningPathId)
            .HasConstraintName("FK_LearningPathItems_LearningPaths_LearningPathId").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(i => i.Session).WithMany().HasForeignKey(i => i.SessionId)
            .HasConstraintName("FK_LearningPathItems_Sessions_SessionId").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.KnowledgeAsset).WithMany().HasForeignKey(i => i.KnowledgeAssetId)
            .HasConstraintName("FK_LearningPathItems_KnowledgeAssets_KnowledgeAssetId").OnDelete(DeleteBehavior.Restrict);
    }
}

public class UserLearningPathEnrollmentConfiguration : IEntityTypeConfiguration<UserLearningPathEnrollment>
{
    public void Configure(EntityTypeBuilder<UserLearningPathEnrollment> builder)
    {
        builder.ToTable("UserLearningPathEnrollments");
        builder.HasKey(e => e.Id).HasName("PK_UserLearningPathEnrollments");
        builder.Property(e => e.EnrolmentType).HasConversion<string>().IsRequired().HasMaxLength(30);
        builder.Property(e => e.ProgressPercentage).HasColumnType("decimal(5,2)");

        builder.HasIndex(e => new { e.TenantId, e.UserId, e.LearningPathId }).IsUnique()
            .HasDatabaseName("IX_UserLearningPathEnrollments_TenantId_UserId_PathId");
        builder.HasIndex(e => e.TenantId).HasDatabaseName("IX_UserLearningPathEnrollments_TenantId");
        builder.HasIndex(e => new { e.TenantId, e.UserId }).HasDatabaseName("IX_UserLearningPathEnrollments_TenantId_UserId");

        builder.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId)
            .HasConstraintName("FK_UserLearningPathEnrollments_Users_UserId").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.LearningPath).WithMany(p => p.Enrollments).HasForeignKey(e => e.LearningPathId)
            .HasConstraintName("FK_UserLearningPathEnrollments_LearningPaths_LearningPathId").OnDelete(DeleteBehavior.Restrict);
    }
}

public class LearningPathCertificateConfiguration : IEntityTypeConfiguration<LearningPathCertificate>
{
    public void Configure(EntityTypeBuilder<LearningPathCertificate> builder)
    {
        builder.ToTable("LearningPathCertificates");
        builder.HasKey(c => c.Id).HasName("PK_LearningPathCertificates");
        builder.Property(c => c.CertificateNumber).IsRequired().HasMaxLength(64);
        builder.Property(c => c.CertificateUrl).IsRequired().HasMaxLength(500);

        builder.HasIndex(c => c.CertificateNumber).IsUnique().HasDatabaseName("IX_LearningPathCertificates_CertificateNumber");
        builder.HasIndex(c => c.TenantId).HasDatabaseName("IX_LearningPathCertificates_TenantId");
        builder.HasIndex(c => new { c.TenantId, c.UserId }).HasDatabaseName("IX_LearningPathCertificates_TenantId_UserId");

        builder.HasOne(c => c.User).WithMany().HasForeignKey(c => c.UserId)
            .HasConstraintName("FK_LearningPathCertificates_Users_UserId").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.LearningPath).WithMany(p => p.Certificates).HasForeignKey(c => c.LearningPathId)
            .HasConstraintName("FK_LearningPathCertificates_LearningPaths_LearningPathId").OnDelete(DeleteBehavior.Restrict);
    }
}

public class LearningPathCohortConfiguration : IEntityTypeConfiguration<LearningPathCohort>
{
    public void Configure(EntityTypeBuilder<LearningPathCohort> builder)
    {
        builder.ToTable("LearningPathCohorts");
        builder.HasKey(c => c.Id).HasName("PK_LearningPathCohorts");
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Status).HasConversion<string>().IsRequired().HasMaxLength(20);

        builder.HasIndex(c => c.TenantId).HasDatabaseName("IX_LearningPathCohorts_TenantId");
        builder.HasIndex(c => new { c.TenantId, c.LearningPathId }).HasDatabaseName("IX_LearningPathCohorts_TenantId_LearningPathId");

        builder.HasOne(c => c.LearningPath).WithMany(p => p.Cohorts).HasForeignKey(c => c.LearningPathId)
            .HasConstraintName("FK_LearningPathCohorts_LearningPaths_LearningPathId").OnDelete(DeleteBehavior.Cascade);
    }
}
