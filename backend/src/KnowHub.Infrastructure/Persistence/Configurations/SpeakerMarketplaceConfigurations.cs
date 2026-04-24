using KnowHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowHub.Infrastructure.Persistence.Configurations;

public class SpeakerAvailabilityConfiguration : IEntityTypeConfiguration<SpeakerAvailability>
{
    public void Configure(EntityTypeBuilder<SpeakerAvailability> builder)
    {
        builder.ToTable("SpeakerAvailability");
        builder.HasKey(a => a.Id).HasName("PK_SpeakerAvailability");

        builder.Property(a => a.RecurrencePattern).HasMaxLength(50);
        builder.Property(a => a.Notes).HasMaxLength(2000);

        builder.Property(a => a.Topics)
            .HasColumnType("jsonb")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>());

        builder.HasIndex(a => a.TenantId).HasDatabaseName("IX_SpeakerAvailability_TenantId");
        builder.HasIndex(a => new { a.TenantId, a.UserId }).HasDatabaseName("IX_SpeakerAvailability_TenantId_UserId");
        builder.HasIndex(a => a.AvailableFrom).HasDatabaseName("IX_SpeakerAvailability_AvailableFrom");

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .HasConstraintName("FK_SpeakerAvailability_Users_UserId")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SpeakerBookingConfiguration : IEntityTypeConfiguration<SpeakerBooking>
{
    public void Configure(EntityTypeBuilder<SpeakerBooking> builder)
    {
        builder.ToTable("SpeakerBookings");
        builder.HasKey(b => b.Id).HasName("PK_SpeakerBookings");

        builder.Property(b => b.Topic).IsRequired().HasMaxLength(500);
        builder.Property(b => b.Description).HasMaxLength(4000);
        builder.Property(b => b.ResponseNotes).HasMaxLength(2000);

        builder.Property(b => b.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasIndex(b => b.TenantId).HasDatabaseName("IX_SpeakerBookings_TenantId");
        builder.HasIndex(b => new { b.TenantId, b.SpeakerUserId })
            .HasDatabaseName("IX_SpeakerBookings_TenantId_SpeakerUserId");
        builder.HasIndex(b => new { b.TenantId, b.RequesterUserId })
            .HasDatabaseName("IX_SpeakerBookings_TenantId_RequesterUserId");

        builder.HasOne(b => b.SpeakerAvailability)
            .WithMany(a => a.Bookings)
            .HasForeignKey(b => b.SpeakerAvailabilityId)
            .HasConstraintName("FK_SpeakerBookings_SpeakerAvailability")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Speaker)
            .WithMany()
            .HasForeignKey(b => b.SpeakerUserId)
            .HasConstraintName("FK_SpeakerBookings_Users_SpeakerUserId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Requester)
            .WithMany()
            .HasForeignKey(b => b.RequesterUserId)
            .HasConstraintName("FK_SpeakerBookings_Users_RequesterUserId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.LinkedSession)
            .WithMany()
            .HasForeignKey(b => b.LinkedSessionId)
            .HasConstraintName("FK_SpeakerBookings_Sessions_LinkedSessionId")
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
