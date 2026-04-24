using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Services;
using KnowHub.Tests.TestHelpers;

namespace KnowHub.Tests.Services;

public class MentoringServiceTests
{
    private static MentoringService CreateService(
        KnowHub.Infrastructure.Persistence.KnowHubDbContext db,
        KnowHub.Application.Contracts.ICurrentUserAccessor currentUser)
        => new(db, currentUser, new FakeNotificationService());

    private static async Task<(KnowHub.Infrastructure.Persistence.KnowHubDbContext db, Guid tenantId, Guid menteeId, Guid mentorId)> SetupWithMentorAsync()
    {
        var (db, tenantId, menteeId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Employee);

        var mentorId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = mentorId, TenantId = tenantId,
            FullName = "Mentor User", Email = "mentor@knowhub.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role = UserRole.Contributor, IsActive = true,
            CreatedBy = mentorId, ModifiedBy = mentorId
        });

        db.ContributorProfiles.Add(new ContributorProfile
        {
            TenantId = tenantId,
            UserId = mentorId,
            AvailableForMentoring = true,
            CreatedBy = mentorId,
            ModifiedBy = mentorId
        });

        await db.SaveChangesAsync();
        return (db, tenantId, menteeId, mentorId);
    }

    [Fact]
    public async Task RequestMentorAsync_MentorAvailable_CreatesPendingPairing()
    {
        var (db, tenantId, menteeId, mentorId) = await SetupWithMentorAsync();
        var currentUser = FakeCurrentUserAccessor.AsEmployee(menteeId, tenantId);
        var service = CreateService(db, currentUser);

        var result = await service.RequestMentorAsync(
            new KnowHub.Application.Contracts.RequestMentorRequest { MentorId = mentorId, GoalsText = "Grow as an engineer" },
            CancellationToken.None);

        Assert.Equal(MentorMenteeStatus.Pending, result.Status);
        Assert.Equal(mentorId, result.MentorId);
        Assert.Equal(menteeId, result.MenteeId);
    }

    [Fact]
    public async Task RequestMentorAsync_MentorNotAvailable_ThrowsBusinessRuleException()
    {
        var (db, tenantId, menteeId, _) = await SetupWithMentorAsync();

        // Create another mentor who is NOT available for mentoring
        var unavailableMentorId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = unavailableMentorId, TenantId = tenantId,
            FullName = "Unavailable Mentor", Email = "unavail@knowhub.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role = UserRole.Contributor, IsActive = true,
            CreatedBy = unavailableMentorId, ModifiedBy = unavailableMentorId
        });
        db.ContributorProfiles.Add(new ContributorProfile
        {
            TenantId = tenantId,
            UserId = unavailableMentorId,
            AvailableForMentoring = false,
            CreatedBy = unavailableMentorId,
            ModifiedBy = unavailableMentorId
        });
        await db.SaveChangesAsync();

        var currentUser = FakeCurrentUserAccessor.AsEmployee(menteeId, tenantId);
        var service = CreateService(db, currentUser);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.RequestMentorAsync(
                new KnowHub.Application.Contracts.RequestMentorRequest { MentorId = unavailableMentorId },
                CancellationToken.None));
    }

    [Fact]
    public async Task AcceptAsync_ByMentor_UpdatesStatusToActive()
    {
        var (db, tenantId, menteeId, mentorId) = await SetupWithMentorAsync();

        // Mentee requests mentor
        var menteeCurrentUser = FakeCurrentUserAccessor.AsEmployee(menteeId, tenantId);
        var menteeService = CreateService(db, menteeCurrentUser);
        var pairing = await menteeService.RequestMentorAsync(
            new KnowHub.Application.Contracts.RequestMentorRequest { MentorId = mentorId },
            CancellationToken.None);

        // Mentor accepts
        var mentorCurrentUser = FakeCurrentUserAccessor.AsContributor(mentorId, tenantId);
        var mentorService = CreateService(db, mentorCurrentUser);
        var result = await mentorService.AcceptAsync(pairing.Id, CancellationToken.None);

        Assert.Equal(MentorMenteeStatus.Active, result.Status);
        Assert.NotNull(result.StartedAt);
    }

    [Fact]
    public async Task AcceptAsync_ByNonMentor_ThrowsForbiddenException()
    {
        var (db, tenantId, menteeId, mentorId) = await SetupWithMentorAsync();

        var menteeCurrentUser = FakeCurrentUserAccessor.AsEmployee(menteeId, tenantId);
        var menteeService = CreateService(db, menteeCurrentUser);
        var pairing = await menteeService.RequestMentorAsync(
            new KnowHub.Application.Contracts.RequestMentorRequest { MentorId = mentorId },
            CancellationToken.None);

        // Random user tries to accept
        var random = FakeCurrentUserAccessor.AsEmployee(Guid.NewGuid(), tenantId);
        var randomService = CreateService(db, random);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            randomService.AcceptAsync(pairing.Id, CancellationToken.None));
    }

    [Fact]
    public async Task DeclineAsync_ByMentor_UpdatesStatusToDeclined()
    {
        var (db, tenantId, menteeId, mentorId) = await SetupWithMentorAsync();

        var menteeCurrentUser = FakeCurrentUserAccessor.AsEmployee(menteeId, tenantId);
        var menteeService = CreateService(db, menteeCurrentUser);
        var pairing = await menteeService.RequestMentorAsync(
            new KnowHub.Application.Contracts.RequestMentorRequest { MentorId = mentorId },
            CancellationToken.None);

        var mentorCurrentUser = FakeCurrentUserAccessor.AsContributor(mentorId, tenantId);
        var mentorService = CreateService(db, mentorCurrentUser);
        var result = await mentorService.DeclineAsync(pairing.Id, CancellationToken.None);

        Assert.Equal(MentorMenteeStatus.Declined, result.Status);
    }
}
