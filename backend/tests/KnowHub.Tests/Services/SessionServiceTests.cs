using KnowHub.Application.Contracts;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Services;
using KnowHub.Tests.TestHelpers;

namespace KnowHub.Tests.Services;

public class SessionServiceTests
{
    private static async Task<(KnowHub.Infrastructure.Persistence.KnowHubDbContext db, Guid tenantId, Guid adminId, Guid proposalId, Guid categoryId)> SetupWithPublishedProposalAsync()
    {
        var (db, tenantId, adminId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);

        var category = new Category
        {
            TenantId = tenantId,
            Name = "Engineering",
            IsActive = true,
            CreatedBy = adminId,
            ModifiedBy = adminId
        };
        db.Categories.Add(category);

        var proposal = new SessionProposal
        {
            TenantId = tenantId,
            ProposerId = adminId,
            Title = "Clean Architecture",
            CategoryId = category.Id,
            Topic = "Architecture",
            Description = "Talk about CA",
            Format = SessionFormat.Webinar,
            Duration = 60,
            DifficultyLevel = DifficultyLevel.Intermediate,
            AllowRecording = true,
            Status = ProposalStatus.Published,
            CreatedBy = adminId,
            ModifiedBy = adminId
        };
        db.SessionProposals.Add(proposal);
        await db.SaveChangesAsync();

        return (db, tenantId, adminId, proposal.Id, category.Id);
    }

    [Fact]
    public async Task CreateAsync_AdminWithPublishedProposal_CreatesSession()
    {
        var (db, tenantId, adminId, proposalId, _) = await SetupWithPublishedProposalAsync();
        var currentUser = FakeCurrentUserAccessor.AsAdmin(adminId, tenantId);
        var notifications = new FakeNotificationService();
        var service = new SessionService(db, currentUser, notifications);

        var result = await service.CreateAsync(new CreateSessionRequest
        {
            ProposalId = proposalId,
            ScheduledAt = DateTime.UtcNow.AddDays(7),
            DurationMinutes = 60,
            MeetingLink = "https://teams.microsoft.com/l/meetup-join/abc",
            MeetingPlatform = MeetingPlatform.Teams,
            IsPublic = true
        }, CancellationToken.None);

        Assert.Equal("Clean Architecture", result.Title);
        Assert.Equal(SessionStatus.Scheduled, result.Status);
        Assert.Equal(adminId, result.SpeakerId);
    }

    [Fact]
    public async Task CreateAsync_EmployeeRole_ThrowsForbiddenException()
    {
        var (db, tenantId, adminId, proposalId, _) = await SetupWithPublishedProposalAsync();
        var employeeId = Guid.NewGuid();
        var employee = new User
        {
            Id = employeeId,
            TenantId = tenantId,
            FullName = "Employee",
            Email = "emp@knowhub.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role = UserRole.Employee,
            IsActive = true,
            CreatedBy = adminId,
            ModifiedBy = adminId
        };
        db.Users.Add(employee);
        await db.SaveChangesAsync();

        var currentUser = FakeCurrentUserAccessor.AsEmployee(employeeId, tenantId);
        var service = new SessionService(db, currentUser, new FakeNotificationService());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.CreateAsync(new CreateSessionRequest
            {
                ProposalId = proposalId,
                ScheduledAt = DateTime.UtcNow.AddDays(7),
                DurationMinutes = 60,
                MeetingLink = "https://teams.microsoft.com/l/meetup-join/abc",
                MeetingPlatform = MeetingPlatform.Teams
            }, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_NonPublishedProposal_ThrowsBusinessRuleException()
    {
        var (db, tenantId, adminId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var category = new Category { TenantId = tenantId, Name = "QA", IsActive = true, CreatedBy = adminId, ModifiedBy = adminId };
        db.Categories.Add(category);
        var draftProposal = new SessionProposal
        {
            TenantId = tenantId,
            ProposerId = adminId,
            Title = "Draft Session",
            CategoryId = category.Id,
            Topic = "Topic",
            Description = "Desc",
            Format = SessionFormat.Demo,
            Duration = 30,
            DifficultyLevel = DifficultyLevel.Beginner,
            AllowRecording = false,
            Status = ProposalStatus.Draft,
            CreatedBy = adminId,
            ModifiedBy = adminId
        };
        db.SessionProposals.Add(draftProposal);
        await db.SaveChangesAsync();

        var currentUser = FakeCurrentUserAccessor.AsAdmin(adminId, tenantId);
        var service = new SessionService(db, currentUser, new FakeNotificationService());

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.CreateAsync(new CreateSessionRequest
            {
                ProposalId = draftProposal.Id,
                ScheduledAt = DateTime.UtcNow.AddDays(7),
                DurationMinutes = 30,
                MeetingLink = "https://teams.microsoft.com/l/meetup-join/abc",
                MeetingPlatform = MeetingPlatform.Teams
            }, CancellationToken.None));
    }

    [Fact]
    public async Task RegisterAsync_ScheduledSession_RegistersParticipant()
    {
        var (db, tenantId, adminId, proposalId, _) = await SetupWithPublishedProposalAsync();
        var adminUser = FakeCurrentUserAccessor.AsAdmin(adminId, tenantId);
        var adminService = new SessionService(db, adminUser, new FakeNotificationService());

        var session = await adminService.CreateAsync(new CreateSessionRequest
        {
            ProposalId = proposalId,
            ScheduledAt = DateTime.UtcNow.AddDays(7),
            DurationMinutes = 60,
            MeetingLink = "https://teams.microsoft.com/l/meetup-join/abc",
            MeetingPlatform = MeetingPlatform.Teams,
            IsPublic = true
        }, CancellationToken.None);

        // A second user registers
        var participantId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = participantId,
            TenantId = tenantId,
            FullName = "Participant",
            Email = "p@knowhub.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role = UserRole.Employee,
            IsActive = true,
            CreatedBy = adminId,
            ModifiedBy = adminId
        });
        await db.SaveChangesAsync();

        var participantUser = FakeCurrentUserAccessor.AsEmployee(participantId, tenantId);
        var participantService = new SessionService(db, participantUser, new FakeNotificationService());

        var reg = await participantService.RegisterAsync(session.Id, CancellationToken.None);

        Assert.Equal(RegistrationStatus.Registered, reg.Status);
        Assert.Equal(participantId, reg.ParticipantId);
    }

    [Fact]
    public async Task RegisterAsync_AlreadyRegistered_ThrowsConflictException()
    {
        var (db, tenantId, adminId, proposalId, _) = await SetupWithPublishedProposalAsync();
        var adminUser = FakeCurrentUserAccessor.AsAdmin(adminId, tenantId);
        var adminService = new SessionService(db, adminUser, new FakeNotificationService());

        var session = await adminService.CreateAsync(new CreateSessionRequest
        {
            ProposalId = proposalId,
            ScheduledAt = DateTime.UtcNow.AddDays(7),
            DurationMinutes = 60,
            MeetingLink = "https://teams.microsoft.com/l/meetup-join/abc",
            MeetingPlatform = MeetingPlatform.Teams,
            IsPublic = true
        }, CancellationToken.None);

        var participantId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = participantId, TenantId = tenantId, FullName = "P2", Email = "p2@k.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role = UserRole.Employee, IsActive = true, CreatedBy = adminId, ModifiedBy = adminId
        });
        await db.SaveChangesAsync();

        var participantUser = FakeCurrentUserAccessor.AsEmployee(participantId, tenantId);
        var participantService = new SessionService(db, participantUser, new FakeNotificationService());

        await participantService.RegisterAsync(session.Id, CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            participantService.RegisterAsync(session.Id, CancellationToken.None));
    }

    [Fact]
    public async Task CancelAsync_AdminCancelsSession_StatusBecomeCancelled()
    {
        var (db, tenantId, adminId, proposalId, _) = await SetupWithPublishedProposalAsync();
        var adminUser = FakeCurrentUserAccessor.AsAdmin(adminId, tenantId);
        var notifications = new FakeNotificationService();
        var service = new SessionService(db, adminUser, notifications);

        var session = await service.CreateAsync(new CreateSessionRequest
        {
            ProposalId = proposalId,
            ScheduledAt = DateTime.UtcNow.AddDays(7),
            DurationMinutes = 60,
            MeetingLink = "https://teams.microsoft.com/l/meetup-join/abc",
            MeetingPlatform = MeetingPlatform.Teams
        }, CancellationToken.None);

        var cancelled = await service.CancelAsync(session.Id, CancellationToken.None);

        Assert.Equal(SessionStatus.Cancelled, cancelled.Status);
    }

    [Fact]
    public async Task CancelAsync_AlreadyCancelledSession_ThrowsBusinessRuleException()
    {
        var (db, tenantId, adminId, proposalId, _) = await SetupWithPublishedProposalAsync();
        var adminUser = FakeCurrentUserAccessor.AsAdmin(adminId, tenantId);
        var service = new SessionService(db, adminUser, new FakeNotificationService());

        var session = await service.CreateAsync(new CreateSessionRequest
        {
            ProposalId = proposalId,
            ScheduledAt = DateTime.UtcNow.AddDays(7),
            DurationMinutes = 60,
            MeetingLink = "https://teams.microsoft.com/l/meetup-join/abc",
            MeetingPlatform = MeetingPlatform.Teams
        }, CancellationToken.None);

        await service.CancelAsync(session.Id, CancellationToken.None);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.CancelAsync(session.Id, CancellationToken.None));
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentSession_ThrowsNotFoundException()
    {
        var (db, tenantId, adminId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(adminId, tenantId);
        var service = new SessionService(db, currentUser, new FakeNotificationService());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None));
    }
}
