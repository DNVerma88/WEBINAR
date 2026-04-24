using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowHub.Infrastructure.Persistence.Configurations;

public class UserXpEventConfiguration : IEntityTypeConfiguration<UserXpEvent>
{
    public void Configure(EntityTypeBuilder<UserXpEvent> builder)
    {
        builder.ToTable("UserXpEvents");
        builder.HasKey(x => x.Id).HasName("PK_UserXpEvents");
        builder.Property(x => x.EventType).HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.RelatedEntityType).HasMaxLength(50);

        builder.HasIndex(x => x.TenantId).HasDatabaseName("IX_UserXpEvents_TenantId");
        builder.HasIndex(x => new { x.TenantId, x.UserId }).HasDatabaseName("IX_UserXpEvents_TenantId_UserId");
        builder.HasIndex(x => x.EarnedAt).HasDatabaseName("IX_UserXpEvents_EarnedAt");

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId)
            .HasConstraintName("FK_UserXpEvents_Users_UserId").OnDelete(DeleteBehavior.Restrict);
    }
}

public class UserLearningStreakConfiguration : IEntityTypeConfiguration<UserLearningStreak>
{
    public void Configure(EntityTypeBuilder<UserLearningStreak> builder)
    {
        builder.ToTable("UserLearningStreaks");
        builder.HasKey(s => s.Id).HasName("PK_UserLearningStreaks");

        builder.HasIndex(s => new { s.TenantId, s.UserId }).IsUnique().HasDatabaseName("IX_UserLearningStreaks_TenantId_UserId");
        builder.HasIndex(s => s.TenantId).HasDatabaseName("IX_UserLearningStreaks_TenantId");

        builder.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId)
            .HasConstraintName("FK_UserLearningStreaks_Users_UserId").OnDelete(DeleteBehavior.Restrict);
    }
}

public class LeaderboardSnapshotConfiguration : IEntityTypeConfiguration<LeaderboardSnapshot>
{
    public void Configure(EntityTypeBuilder<LeaderboardSnapshot> builder)
    {
        builder.ToTable("LeaderboardSnapshots");
        builder.HasKey(s => s.Id).HasName("PK_LeaderboardSnapshots");
        builder.Property(s => s.LeaderboardType).HasConversion<string>().IsRequired().HasMaxLength(30);

        builder.HasIndex(s => new { s.TenantId, s.SnapshotMonth, s.SnapshotYear, s.LeaderboardType }).IsUnique()
            .HasDatabaseName("IX_LeaderboardSnapshots_Unique");
        builder.HasIndex(s => s.TenantId).HasDatabaseName("IX_LeaderboardSnapshots_TenantId");
    }
}
