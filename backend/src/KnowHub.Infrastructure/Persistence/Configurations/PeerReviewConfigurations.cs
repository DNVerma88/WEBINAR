using KnowHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowHub.Infrastructure.Persistence.Configurations;

public class KnowledgeAssetReviewConfiguration : IEntityTypeConfiguration<KnowledgeAssetReview>
{
    public void Configure(EntityTypeBuilder<KnowledgeAssetReview> builder)
    {
        builder.ToTable("KnowledgeAssetReviews");
        builder.HasKey(r => r.Id).HasName("PK_KnowledgeAssetReviews");

        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(r => r.Comments).HasMaxLength(4000);

        builder.HasIndex(r => r.TenantId).HasDatabaseName("IX_KnowledgeAssetReviews_TenantId");
        builder.HasIndex(r => new { r.TenantId, r.KnowledgeAssetId })
            .HasDatabaseName("IX_KnowledgeAssetReviews_TenantId_KnowledgeAssetId");
        builder.HasIndex(r => r.ReviewerId).HasDatabaseName("IX_KnowledgeAssetReviews_ReviewerId");

        builder.HasOne(r => r.KnowledgeAsset)
            .WithMany()
            .HasForeignKey(r => r.KnowledgeAssetId)
            .HasConstraintName("FK_KnowledgeAssetReviews_KnowledgeAssets")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Reviewer)
            .WithMany()
            .HasForeignKey(r => r.ReviewerId)
            .HasConstraintName("FK_KnowledgeAssetReviews_Users_ReviewerId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.NominatedBy)
            .WithMany()
            .HasForeignKey(r => r.NominatedByUserId)
            .HasConstraintName("FK_KnowledgeAssetReviews_Users_NominatedByUserId")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
