using KnowHub.Domain.Entities.Talent;
using KnowHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowHub.Infrastructure.Persistence.Configurations;

public class ResumeProfileConfiguration : IEntityTypeConfiguration<ResumeProfile>
{
    public void Configure(EntityTypeBuilder<ResumeProfile> builder)
    {
        builder.ToTable("ResumeProfiles");
        builder.HasKey(r => r.Id).HasName("PK_ResumeProfiles");

        builder.Property(r => r.Template).IsRequired().HasMaxLength(50);
        builder.Property(r => r.PersonalInfo).IsRequired().HasColumnType("jsonb");
        builder.Property(r => r.Summary).HasColumnType("text");
        builder.Property(r => r.WorkExperience).IsRequired().HasColumnType("jsonb");
        builder.Property(r => r.Education).IsRequired().HasColumnType("jsonb");
        builder.Property(r => r.Skills).IsRequired().HasColumnType("jsonb");
        builder.Property(r => r.Certifications).IsRequired().HasColumnType("jsonb");
        builder.Property(r => r.Projects).IsRequired().HasColumnType("jsonb");
        builder.Property(r => r.Languages).IsRequired().HasColumnType("jsonb");
        builder.Property(r => r.Publications).IsRequired().HasColumnType("jsonb");
        builder.Property(r => r.Achievements).IsRequired().HasColumnType("jsonb");
        builder.Property(r => r.CreatedAt).HasColumnType("timestamptz");
        builder.Property(r => r.UpdatedAt).HasColumnType("timestamptz");

        builder.HasIndex(r => new { r.TenantId, r.UserId }).IsUnique()
            .HasDatabaseName("IX_ResumeProfiles_TenantId_UserId");
        builder.HasIndex(r => r.TenantId)
            .HasDatabaseName("IX_ResumeProfiles_TenantId");
    }
}

public class ScreeningJobConfiguration : IEntityTypeConfiguration<ScreeningJob>
{
    public void Configure(EntityTypeBuilder<ScreeningJob> builder)
    {
        builder.ToTable("ScreeningJobs");
        builder.HasKey(j => j.Id).HasName("PK_ScreeningJobs");

        builder.Property(j => j.JobTitle).IsRequired().HasMaxLength(300);
        builder.Property(j => j.JdText).HasColumnType("text");
        builder.Property(j => j.JdFileReference).HasColumnType("jsonb");
        builder.Property(j => j.JdEmbedding).HasColumnType("jsonb");
        builder.Property(j => j.ErrorMessage).HasColumnType("text");
        builder.Property(j => j.CreatedAt).HasColumnType("timestamptz");
        builder.Property(j => j.StartedAt).HasColumnType("timestamptz");
        builder.Property(j => j.CompletedAt).HasColumnType("timestamptz");
        builder.Property(j => j.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(j => j.TenantId)
            .HasDatabaseName("IX_ScreeningJobs_TenantId");
        builder.HasIndex(j => j.CreatedByUserId)
            .HasDatabaseName("IX_ScreeningJobs_CreatedByUserId");
        builder.HasIndex(j => j.Status)
            .HasDatabaseName("IX_ScreeningJobs_Status");

        builder.HasMany(j => j.Candidates)
            .WithOne(c => c.ScreeningJob)
            .HasForeignKey(c => c.ScreeningJobId)
            .HasConstraintName("FK_ScreeningCandidates_ScreeningJobs_ScreeningJobId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ScreeningCandidateConfiguration : IEntityTypeConfiguration<ScreeningCandidate>
{
    public void Configure(EntityTypeBuilder<ScreeningCandidate> builder)
    {
        builder.ToTable("ScreeningCandidates");
        builder.HasKey(c => c.Id).HasName("PK_ScreeningCandidates");

        builder.Property(c => c.FileName).IsRequired().HasMaxLength(500);
        builder.Property(c => c.StorageProviderType).IsRequired().HasMaxLength(50);
        builder.Property(c => c.FileReference).IsRequired().HasColumnType("text");
        builder.Property(c => c.ErrorMessage).HasColumnType("text");
        builder.Property(c => c.ExtractedText).HasColumnType("text");
        builder.Property(c => c.CandidateName).HasMaxLength(300);
        builder.Property(c => c.Email).HasMaxLength(300);
        builder.Property(c => c.Phone).HasMaxLength(100);
        builder.Property(c => c.SemanticSimilarityScore).HasColumnType("decimal(5,4)");
        builder.Property(c => c.SkillsDepthScore).HasColumnType("decimal(5,2)");
        builder.Property(c => c.LegitimacyScore).HasColumnType("decimal(5,2)");
        builder.Property(c => c.OverallScore).HasColumnType("decimal(5,2)");
        builder.Property(c => c.Recommendation).HasMaxLength(50);
        builder.Property(c => c.ScoreSummary).HasColumnType("text");
        builder.Property(c => c.SkillsMatched).HasColumnType("jsonb");
        builder.Property(c => c.SkillsGap).HasColumnType("jsonb");
        builder.Property(c => c.RedFlags).HasColumnType("jsonb");
        builder.Property(c => c.CreatedAt).HasColumnType("timestamptz");
        builder.Property(c => c.ScoredAt).HasColumnType("timestamptz");
        builder.Property(c => c.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(c => c.ScreeningJobId)
            .HasDatabaseName("IX_ScreeningCandidates_JobId");
        builder.HasIndex(c => new { c.ScreeningJobId, c.OverallScore })
            .HasDatabaseName("IX_ScreeningCandidates_JobId_Score");
    }
}
