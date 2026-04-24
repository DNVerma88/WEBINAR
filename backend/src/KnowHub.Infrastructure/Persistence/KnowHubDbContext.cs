using KnowHub.Application.Contracts;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Entities.Talent;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace KnowHub.Infrastructure.Persistence;

public class KnowHubDbContext : DbContext
{
    // Captured per-scoped-instance (one DbContext per HTTP request) — correct value
    // for all queries in the same request. Guid.Empty means "unauthenticated request;
    // do not filter" (e.g. /api/auth/login resolving users across tenants).
    private readonly Guid _tenantId;

    public KnowHubDbContext(DbContextOptions<KnowHubDbContext> options, ITenantIdProvider? tenantIdProvider = null)
        : base(options)
    {
        _tenantId = tenantIdProvider?.CurrentTenantId ?? Guid.Empty;
    }

    // Phase 1
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ContributorProfile> ContributorProfiles => Set<ContributorProfile>();
    public DbSet<UserSkill> UserSkills => Set<UserSkill>();
    public DbSet<UserFollower> UserFollowers => Set<UserFollower>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<SessionProposal> SessionProposals => Set<SessionProposal>();
    public DbSet<ProposalApproval> ProposalApprovals => Set<ProposalApproval>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<SessionTag> SessionTags => Set<SessionTag>();
    public DbSet<SessionMaterial> SessionMaterials => Set<SessionMaterial>();
    public DbSet<SessionRegistration> SessionRegistrations => Set<SessionRegistration>();
    public DbSet<KnowledgeAsset> KnowledgeAssets => Set<KnowledgeAsset>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Like> Likes => Set<Like>();
    public DbSet<SessionRating> SessionRatings => Set<SessionRating>();
    public DbSet<Badge> Badges => Set<Badge>();
    public DbSet<UserBadge> UserBadges => Set<UserBadge>();
    public DbSet<Community> Communities => Set<Community>();
    public DbSet<CommunityMember> CommunityMembers => Set<CommunityMember>();
    public DbSet<CommunityWikiPage> CommunityWikiPages => Set<CommunityWikiPage>();
    public DbSet<CommunityPost> CommunityPosts => Set<CommunityPost>();
    public DbSet<CommunityPostTag> CommunityPostTags => Set<CommunityPostTag>();
    public DbSet<PostSeries> PostSeries => Set<PostSeries>();
    public DbSet<PostReaction> PostReactions => Set<PostReaction>();
    public DbSet<PostComment> PostComments => Set<PostComment>();
    public DbSet<PostBookmark> PostBookmarks => Set<PostBookmark>();
    public DbSet<UserTagFollow> UserTagFollows => Set<UserTagFollow>();
    public DbSet<ContentReport> ContentReports => Set<ContentReport>();
    public DbSet<KnowledgeRequest> KnowledgeRequests => Set<KnowledgeRequest>();
    public DbSet<Notification> Notifications => Set<Notification>();

    // Phase 2
    public DbSet<UserXpEvent> UserXpEvents => Set<UserXpEvent>();
    public DbSet<LearningPath> LearningPaths => Set<LearningPath>();
    public DbSet<LearningPathItem> LearningPathItems => Set<LearningPathItem>();
    public DbSet<UserLearningPathEnrollment> UserLearningPathEnrollments => Set<UserLearningPathEnrollment>();
    public DbSet<LearningPathCertificate> LearningPathCertificates => Set<LearningPathCertificate>();
    public DbSet<SessionQuiz> SessionQuizzes => Set<SessionQuiz>();
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
    public DbSet<UserQuizAttempt> UserQuizAttempts => Set<UserQuizAttempt>();
    public DbSet<SkillEndorsement> SkillEndorsements => Set<SkillEndorsement>();
    public DbSet<SessionChapter> SessionChapters => Set<SessionChapter>();
    public DbSet<KnowledgeBundle> KnowledgeBundles => Set<KnowledgeBundle>();
    public DbSet<KnowledgeBundleItem> KnowledgeBundleItems => Set<KnowledgeBundleItem>();
    public DbSet<AfterActionReview> AfterActionReviews => Set<AfterActionReview>();
    public DbSet<MentorMentee> MentorMentees => Set<MentorMentee>();
    public DbSet<UserLearningStreak> UserLearningStreaks => Set<UserLearningStreak>();
    public DbSet<LeaderboardSnapshot> LeaderboardSnapshots => Set<LeaderboardSnapshot>();
    public DbSet<LearningPathCohort> LearningPathCohorts => Set<LearningPathCohort>();

    // Phase 3
    public DbSet<ContentFlag> ContentFlags => Set<ContentFlag>();
    public DbSet<UserSuspension> UserSuspensions => Set<UserSuspension>();
    public DbSet<KnowledgeAssetReview> KnowledgeAssetReviews => Set<KnowledgeAssetReview>();
    public DbSet<SpeakerAvailability> SpeakerAvailability => Set<SpeakerAvailability>();
    public DbSet<SpeakerBooking> SpeakerBookings => Set<SpeakerBooking>();
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();

    // -- Assessment Module ------------------------------------------------------
    public DbSet<AssessmentGroup>                   AssessmentGroups                   => Set<AssessmentGroup>();
    public DbSet<AssessmentGroupMember>             AssessmentGroupMembers             => Set<AssessmentGroupMember>();
    public DbSet<AssessmentGroupCoLead>             AssessmentGroupCoLeads             => Set<AssessmentGroupCoLead>();
    public DbSet<AssessmentPeriod>                  AssessmentPeriods                  => Set<AssessmentPeriod>();
    public DbSet<RatingScale>                       RatingScales                       => Set<RatingScale>();
    public DbSet<RubricDefinition>                  RubricDefinitions                  => Set<RubricDefinition>();
    public DbSet<ParameterMaster>                   ParameterMasters                   => Set<ParameterMaster>();
    public DbSet<RoleParameterMapping>              RoleParameterMappings              => Set<RoleParameterMapping>();
    public DbSet<EmployeeAssessment>                EmployeeAssessments                => Set<EmployeeAssessment>();
    public DbSet<EmployeeAssessmentParameterDetail> EmployeeAssessmentParameterDetails => Set<EmployeeAssessmentParameterDetail>();
    public DbSet<AssessmentAuditLog>                AssessmentAuditLogs                => Set<AssessmentAuditLog>();
    public DbSet<WorkRole>                          WorkRoles                          => Set<WorkRole>();

    // -- Talent Module ---------------------------------------------------------
    public DbSet<ResumeProfile>      ResumeProfiles      => Set<ResumeProfile>();
    public DbSet<ScreeningJob>       ScreeningJobs       => Set<ScreeningJob>();
    public DbSet<ScreeningCandidate> ScreeningCandidates => Set<ScreeningCandidate>();

    // -- Survey Module ---------------------------------------------------------
    public DbSet<Survey>           Surveys           => Set<Survey>();
    public DbSet<SurveyQuestion>   SurveyQuestions   => Set<SurveyQuestion>();
    public DbSet<SurveyInvitation> SurveyInvitations => Set<SurveyInvitation>();
    public DbSet<SurveyResponse>   SurveyResponses   => Set<SurveyResponse>();
    public DbSet<SurveyAnswer>     SurveyAnswers     => Set<SurveyAnswer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KnowHubDbContext).Assembly);

        // Global TenantId query filter — applied automatically to every entity that
        // derives from BaseEntity. When _tenantId == Guid.Empty (unauthenticated
        // requests such as /api/auth/login) the filter is bypassed so those endpoints
        // can locate users/tenants across the full table.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType)) continue;

            var method = typeof(KnowHubDbContext)
                .GetMethod(nameof(ApplyTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(entityType.ClrType);

            method.Invoke(this, [modelBuilder]);
        }
    }

    private void ApplyTenantFilter<T>(ModelBuilder modelBuilder) where T : BaseEntity
    {
        modelBuilder.Entity<T>().HasQueryFilter(
            e => _tenantId == Guid.Empty || e.TenantId == _tenantId);
    }
}
