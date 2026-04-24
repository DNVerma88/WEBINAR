using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowHub.Infrastructure.Persistence.Configurations;

public class SessionQuizConfiguration : IEntityTypeConfiguration<SessionQuiz>
{
    public void Configure(EntityTypeBuilder<SessionQuiz> builder)
    {
        builder.ToTable("SessionQuizzes");
        builder.HasKey(q => q.Id).HasName("PK_SessionQuizzes");
        builder.Property(q => q.Title).IsRequired().HasMaxLength(200);

        builder.HasIndex(q => q.TenantId).HasDatabaseName("IX_SessionQuizzes_TenantId");
        builder.HasIndex(q => new { q.TenantId, q.SessionId }).IsUnique().HasDatabaseName("IX_SessionQuizzes_TenantId_SessionId");

        builder.HasOne(q => q.Session).WithOne().HasForeignKey<SessionQuiz>(q => q.SessionId)
            .HasConstraintName("FK_SessionQuizzes_Sessions_SessionId").OnDelete(DeleteBehavior.Cascade);
    }
}

public class QuizQuestionConfiguration : IEntityTypeConfiguration<QuizQuestion>
{
    public void Configure(EntityTypeBuilder<QuizQuestion> builder)
    {
        builder.ToTable("QuizQuestions");
        builder.HasKey(q => q.Id).HasName("PK_QuizQuestions");
        builder.Property(q => q.QuestionType).HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(q => q.CorrectAnswer).HasMaxLength(500);
        builder.Property(q => q.Options).HasColumnType("jsonb");

        builder.HasIndex(q => q.TenantId).HasDatabaseName("IX_QuizQuestions_TenantId");
        builder.HasIndex(q => new { q.TenantId, q.QuizId }).HasDatabaseName("IX_QuizQuestions_TenantId_QuizId");

        builder.HasOne(q => q.Quiz).WithMany(sq => sq.Questions).HasForeignKey(q => q.QuizId)
            .HasConstraintName("FK_QuizQuestions_SessionQuizzes_QuizId").OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserQuizAttemptConfiguration : IEntityTypeConfiguration<UserQuizAttempt>
{
    public void Configure(EntityTypeBuilder<UserQuizAttempt> builder)
    {
        builder.ToTable("UserQuizAttempts");
        builder.HasKey(a => a.Id).HasName("PK_UserQuizAttempts");
        builder.Property(a => a.Score).HasColumnType("decimal(5,2)");
        builder.Property(a => a.Answers).HasColumnType("jsonb");

        builder.HasIndex(a => new { a.TenantId, a.QuizId, a.UserId, a.AttemptNumber }).IsUnique()
            .HasDatabaseName("IX_UserQuizAttempts_TenantId_QuizId_UserId_AttemptNumber");
        builder.HasIndex(a => a.TenantId).HasDatabaseName("IX_UserQuizAttempts_TenantId");
        builder.HasIndex(a => new { a.TenantId, a.UserId }).HasDatabaseName("IX_UserQuizAttempts_TenantId_UserId");

        builder.HasOne(a => a.Quiz).WithMany(q => q.Attempts).HasForeignKey(a => a.QuizId)
            .HasConstraintName("FK_UserQuizAttempts_SessionQuizzes_QuizId").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId)
            .HasConstraintName("FK_UserQuizAttempts_Users_UserId").OnDelete(DeleteBehavior.Restrict);
    }
}

public class SessionChapterConfiguration : IEntityTypeConfiguration<SessionChapter>
{
    public void Configure(EntityTypeBuilder<SessionChapter> builder)
    {
        builder.ToTable("SessionChapters");
        builder.HasKey(c => c.Id).HasName("PK_SessionChapters");
        builder.Property(c => c.Title).IsRequired().HasMaxLength(200);

        builder.HasIndex(c => c.TenantId).HasDatabaseName("IX_SessionChapters_TenantId");
        builder.HasIndex(c => new { c.TenantId, c.SessionId }).HasDatabaseName("IX_SessionChapters_TenantId_SessionId");

        builder.HasOne(c => c.Session).WithMany().HasForeignKey(c => c.SessionId)
            .HasConstraintName("FK_SessionChapters_Sessions_SessionId").OnDelete(DeleteBehavior.Cascade);
    }
}

public class AfterActionReviewConfiguration : IEntityTypeConfiguration<AfterActionReview>
{
    public void Configure(EntityTypeBuilder<AfterActionReview> builder)
    {
        builder.ToTable("AfterActionReviews");
        builder.HasKey(a => a.Id).HasName("PK_AfterActionReviews");

        builder.HasIndex(a => a.TenantId).HasDatabaseName("IX_AfterActionReviews_TenantId");
        builder.HasIndex(a => new { a.TenantId, a.SessionId }).IsUnique().HasDatabaseName("IX_AfterActionReviews_TenantId_SessionId");

        builder.HasOne(a => a.Session).WithOne().HasForeignKey<AfterActionReview>(a => a.SessionId)
            .HasConstraintName("FK_AfterActionReviews_Sessions_SessionId").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(a => a.Author).WithMany().HasForeignKey(a => a.AuthorId)
            .HasConstraintName("FK_AfterActionReviews_Users_AuthorId").OnDelete(DeleteBehavior.Restrict);
    }
}
