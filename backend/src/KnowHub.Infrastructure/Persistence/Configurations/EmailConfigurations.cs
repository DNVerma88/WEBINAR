using KnowHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowHub.Infrastructure.Persistence.Configurations;

public class EmailLogConfiguration : IEntityTypeConfiguration<EmailLog>
{
    public void Configure(EntityTypeBuilder<EmailLog> builder)
    {
        builder.ToTable("EmailLogs");
        builder.HasKey(e => e.Id).HasName("PK_EmailLogs");

        builder.Property(e => e.RecipientEmail).IsRequired().HasMaxLength(255);
        builder.Property(e => e.Subject).IsRequired().HasMaxLength(500);
        builder.Property(e => e.EmailType).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(50);
        builder.Property(e => e.ErrorMessage).HasMaxLength(4000);
        builder.Property(e => e.MessageId).HasMaxLength(500);

        builder.HasIndex(e => e.TenantId).HasDatabaseName("IX_EmailLogs_TenantId");
        builder.HasIndex(e => e.RecipientUserId).HasDatabaseName("IX_EmailLogs_RecipientUserId");
        builder.HasIndex(e => e.SentAt).HasDatabaseName("IX_EmailLogs_SentAt");

        builder.HasOne(e => e.RecipientUser)
            .WithMany()
            .HasForeignKey(e => e.RecipientUserId)
            .HasConstraintName("FK_EmailLogs_Users_RecipientUserId")
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
