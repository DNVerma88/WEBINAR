using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowHub.Infrastructure.Persistence.Configurations;

public class SurveyConfiguration : IEntityTypeConfiguration<Survey>
{
    public void Configure(EntityTypeBuilder<Survey> builder)
    {
        builder.ToTable("Surveys");
        builder.HasKey(s => s.Id).HasName("PK_Surveys");

        builder.Property(s => s.Title).IsRequired().HasMaxLength(300);
        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(s => s.TenantId).HasDatabaseName("IX_Surveys_TenantId");
        builder.HasIndex(s => new { s.TenantId, s.Status }).HasDatabaseName("IX_Surveys_TenantId_Status");
    }
}

public class SurveyQuestionConfiguration : IEntityTypeConfiguration<SurveyQuestion>
{
    public void Configure(EntityTypeBuilder<SurveyQuestion> builder)
    {
        builder.ToTable("SurveyQuestions");
        builder.HasKey(q => q.Id).HasName("PK_SurveyQuestions");

        builder.Property(q => q.QuestionText).IsRequired();
        builder.Property(q => q.QuestionType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(q => q.OptionsJson).HasColumnType("jsonb");

        builder.HasIndex(q => q.TenantId).HasDatabaseName("IX_SurveyQuestions_TenantId");
        builder.HasIndex(q => new { q.SurveyId, q.OrderSequence }).HasDatabaseName("IX_SurveyQuestions_SurveyId_Order");

        builder.HasOne(q => q.Survey)
            .WithMany(s => s.Questions)
            .HasForeignKey(q => q.SurveyId)
            .HasConstraintName("FK_SurveyQuestions_Surveys_SurveyId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SurveyInvitationConfiguration : IEntityTypeConfiguration<SurveyInvitation>
{
    public void Configure(EntityTypeBuilder<SurveyInvitation> builder)
    {
        builder.ToTable("SurveyInvitations");
        builder.HasKey(i => i.Id).HasName("PK_SurveyInvitations");

        builder.Property(i => i.TokenHash).IsRequired().HasMaxLength(64).IsFixedLength();
        builder.Property(i => i.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(i => new { i.TenantId, i.SurveyId, i.UserId })
            .IsUnique()
            .HasDatabaseName("UQ_SurveyInvitations_SurveyId_UserId");
        builder.HasIndex(i => i.TokenHash)
            .IsUnique()
            .HasDatabaseName("UQ_SurveyInvitations_TokenHash");
        builder.HasIndex(i => i.TenantId).HasDatabaseName("IX_SurveyInvitations_TenantId");
        builder.HasIndex(i => new { i.SurveyId, i.Status }).HasDatabaseName("IX_SurveyInvitations_SurveyId_Status");
        builder.HasIndex(i => i.UserId).HasDatabaseName("IX_SurveyInvitations_UserId");

        builder.HasOne(i => i.Survey)
            .WithMany(s => s.Invitations)
            .HasForeignKey(i => i.SurveyId)
            .HasConstraintName("FK_SurveyInvitations_Surveys_SurveyId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.User)
            .WithMany()
            .HasForeignKey(i => i.UserId)
            .HasConstraintName("FK_SurveyInvitations_Users_UserId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SurveyResponseConfiguration : IEntityTypeConfiguration<SurveyResponse>
{
    public void Configure(EntityTypeBuilder<SurveyResponse> builder)
    {
        builder.ToTable("SurveyResponses");
        builder.HasKey(r => r.Id).HasName("PK_SurveyResponses");

        builder.HasIndex(r => new { r.TenantId, r.SurveyId, r.UserId })
            .IsUnique()
            .HasDatabaseName("UQ_SurveyResponses_SurveyId_UserId");
        builder.HasIndex(r => r.TenantId).HasDatabaseName("IX_SurveyResponses_TenantId");
        builder.HasIndex(r => r.SurveyId).HasDatabaseName("IX_SurveyResponses_SurveyId");

        builder.HasOne(r => r.Survey)
            .WithMany(s => s.Responses)
            .HasForeignKey(r => r.SurveyId)
            .HasConstraintName("FK_SurveyResponses_Surveys_SurveyId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .HasConstraintName("FK_SurveyResponses_Users_UserId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Invitation)
            .WithOne(i => i.Response)
            .HasForeignKey<SurveyResponse>(r => r.InvitationId)
            .HasConstraintName("FK_SurveyResponses_SurveyInvitations_InvitationId")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SurveyAnswerConfiguration : IEntityTypeConfiguration<SurveyAnswer>
{
    public void Configure(EntityTypeBuilder<SurveyAnswer> builder)
    {
        builder.ToTable("SurveyAnswers");
        builder.HasKey(a => a.Id).HasName("PK_SurveyAnswers");

        builder.Property(a => a.AnswerOptionsJson).HasColumnType("jsonb");

        builder.HasIndex(a => new { a.ResponseId, a.QuestionId })
            .IsUnique()
            .HasDatabaseName("UQ_SurveyAnswers_ResponseId_QuestionId");
        builder.HasIndex(a => a.TenantId).HasDatabaseName("IX_SurveyAnswers_TenantId");
        builder.HasIndex(a => a.ResponseId).HasDatabaseName("IX_SurveyAnswers_ResponseId");
        builder.HasIndex(a => a.QuestionId).HasDatabaseName("IX_SurveyAnswers_QuestionId");

        builder.HasOne(a => a.Response)
            .WithMany(r => r.Answers)
            .HasForeignKey(a => a.ResponseId)
            .HasConstraintName("FK_SurveyAnswers_SurveyResponses_ResponseId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Question)
            .WithMany(q => q.Answers)
            .HasForeignKey(a => a.QuestionId)
            .HasConstraintName("FK_SurveyAnswers_SurveyQuestions_QuestionId")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
