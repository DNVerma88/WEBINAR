using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowHub.Infrastructure.Persistence.Configurations;

public class ContentFlagConfiguration : IEntityTypeConfiguration<ContentFlag>
{
    public void Configure(EntityTypeBuilder<ContentFlag> builder)
    {
        builder.ToTable("ContentFlags");
        builder.HasKey(f => f.Id).HasName("PK_ContentFlags");

        builder.Property(f => f.ContentType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(f => f.Reason)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(f => f.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(f => f.Notes).HasMaxLength(2000);
        builder.Property(f => f.ReviewNotes).HasMaxLength(2000);

        builder.HasIndex(f => f.TenantId).HasDatabaseName("IX_ContentFlags_TenantId");
        builder.HasIndex(f => new { f.TenantId, f.Status }).HasDatabaseName("IX_ContentFlags_TenantId_Status");
        builder.HasIndex(f => f.FlaggedByUserId).HasDatabaseName("IX_ContentFlags_FlaggedByUserId");

        builder.HasOne(f => f.FlaggedBy)
            .WithMany()
            .HasForeignKey(f => f.FlaggedByUserId)
            .HasConstraintName("FK_ContentFlags_Users_FlaggedByUserId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.ReviewedBy)
            .WithMany()
            .HasForeignKey(f => f.ReviewedByUserId)
            .HasConstraintName("FK_ContentFlags_Users_ReviewedByUserId")
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}

public class UserSuspensionConfiguration : IEntityTypeConfiguration<UserSuspension>
{
    public void Configure(EntityTypeBuilder<UserSuspension> builder)
    {
        builder.ToTable("UserSuspensions");
        builder.HasKey(s => s.Id).HasName("PK_UserSuspensions");

        builder.Property(s => s.Reason).IsRequired().HasMaxLength(2000);
        builder.Property(s => s.LiftReason).HasMaxLength(2000);

        builder.HasIndex(s => s.TenantId).HasDatabaseName("IX_UserSuspensions_TenantId");
        builder.HasIndex(s => new { s.TenantId, s.UserId }).HasDatabaseName("IX_UserSuspensions_TenantId_UserId");

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .HasConstraintName("FK_UserSuspensions_Users_UserId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.SuspendedBy)
            .WithMany()
            .HasForeignKey(s => s.SuspendedByUserId)
            .HasConstraintName("FK_UserSuspensions_Users_SuspendedByUserId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.LiftedBy)
            .WithMany()
            .HasForeignKey(s => s.LiftedByUserId)
            .HasConstraintName("FK_UserSuspensions_Users_LiftedByUserId")
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ContentReportConfiguration : IEntityTypeConfiguration<ContentReport>
{
    public void Configure(EntityTypeBuilder<ContentReport> builder)
    {
        builder.ToTable("ContentReports");
        builder.HasKey(r => r.Id).HasName("PK_ContentReports");

        builder.Property(r => r.Description).HasColumnType("text");
        builder.Property(r => r.ReasonCode).HasConversion<short>();
        builder.Property(r => r.Status).HasConversion<short>();

        builder.HasIndex(r => new { r.TenantId, r.Status })
            .HasDatabaseName("IX_ContentReports_TenantId_Status");
        builder.HasIndex(r => r.TargetPostId)
            .HasDatabaseName("IX_ContentReports_TargetPostId");

        builder.HasOne(r => r.Reporter).WithMany()
            .HasForeignKey(r => r.ReporterId)
            .HasConstraintName("FK_ContentReports_Reporter").OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.TargetPost).WithMany()
            .HasForeignKey(r => r.TargetPostId)
            .HasConstraintName("FK_ContentReports_TargetPost").OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        builder.HasOne(r => r.TargetComment).WithMany()
            .HasForeignKey(r => r.TargetCommentId)
            .HasConstraintName("FK_ContentReports_TargetComment").OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        builder.HasOne(r => r.Resolver).WithMany()
            .HasForeignKey(r => r.ResolvedBy)
            .HasConstraintName("FK_ContentReports_Resolver").OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
