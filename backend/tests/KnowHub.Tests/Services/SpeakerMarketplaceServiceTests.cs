using KnowHub.Application.Contracts.SpeakerMarketplace;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Services;
using KnowHub.Tests.TestHelpers;

namespace KnowHub.Tests.Services;

public class SpeakerMarketplaceServiceTests
{
    private static SpeakerMarketplaceService CreateService(
        KnowHub.Infrastructure.Persistence.KnowHubDbContext db,
        KnowHub.Application.Contracts.ICurrentUserAccessor currentUser)
        => new(db, currentUser, new FakeNotificationService());

    private static DateTime Future(int hours = 2) => DateTime.UtcNow.AddHours(hours);
    private static DateTime Future(int days, int hours) => DateTime.UtcNow.AddDays(days).AddHours(hours);

    // -- SetAvailability ------------------------------------------------------

    [Fact]
    public async Task SetAvailabilityAsync_ValidSlot_CreatesAvailability()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Contributor);
        var currentUser = FakeCurrentUserAccessor.AsEmployee(userId, tenantId);
        var service = CreateService(db, currentUser);

        var from = Future(1);
        var to = Future(3);

        var result = await service.SetAvailabilityAsync(
            new SetAvailabilityRequest(from, to, false, null, new List<string> { "DevOps", "Kubernetes" }, null),
            CancellationToken.None);

        Assert.Equal(userId, result.UserId);
        Assert.False(result.IsBooked);
        Assert.Contains("DevOps", result.Topics);
    }

    [Fact]
    public async Task SetAvailabilityAsync_ToBeforeFrom_ThrowsValidationException()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync();
        var currentUser = FakeCurrentUserAccessor.AsEmployee(userId, tenantId);
        var service = CreateService(db, currentUser);

        var from = Future(3);
        var to = Future(1); // to is before from

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.SetAvailabilityAsync(
                new SetAvailabilityRequest(from, to, false, null, new List<string>(), null),
                CancellationToken.None));
    }

    // -- DeleteAvailability ---------------------------------------------------

    [Fact]
    public async Task DeleteAvailabilityAsync_OwnerCanDelete_RemovesSlot()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Contributor);
        var currentUser = FakeCurrentUserAccessor.AsEmployee(userId, tenantId);
        var service = CreateService(db, currentUser);

        // Create a slot first
        var slot = await service.SetAvailabilityAsync(
            new SetAvailabilityRequest(Future(1), Future(3), false, null, new List<string>(), null),
            CancellationToken.None);

        await service.DeleteAvailabilityAsync(slot.Id, CancellationToken.None);

        var dbSlot = await db.SpeakerAvailability.FindAsync(slot.Id);
        Assert.Null(dbSlot);
    }

    [Fact]
    public async Task DeleteAvailabilityAsync_NonOwnerNonAdmin_ThrowsForbiddenException()
    {
        var (db, tenantId, speakerId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Contributor);

        var slotId = Guid.NewGuid();
        db.SpeakerAvailability.Add(new SpeakerAvailability
        {
            Id = slotId, TenantId = tenantId, UserId = speakerId,
            AvailableFrom = Future(1), AvailableTo = Future(3),
            IsBooked = false, Topics = new List<string>(),
            CreatedBy = speakerId, ModifiedBy = speakerId,
        });

        var otherId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = otherId, TenantId = tenantId, FullName = "Other", Email = "other@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("P@ss"), Role = UserRole.Employee,
            IsActive = true, CreatedBy = otherId, ModifiedBy = otherId,
        });
        await db.SaveChangesAsync();

        var otherCurrentUser = FakeCurrentUserAccessor.AsEmployee(otherId, tenantId);
        var otherService = CreateService(db, otherCurrentUser);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            otherService.DeleteAvailabilityAsync(slotId, CancellationToken.None));
    }

    // -- RequestBooking -------------------------------------------------------

    [Fact]
    public async Task RequestBookingAsync_ValidRequest_CreatesPendingBooking()
    {
        var (db, tenantId, speakerId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Contributor);

        var slotId = Guid.NewGuid();
        db.SpeakerAvailability.Add(new SpeakerAvailability
        {
            Id = slotId, TenantId = tenantId, UserId = speakerId,
            AvailableFrom = Future(1), AvailableTo = Future(3),
            IsBooked = false, Topics = new List<string> { "Testing" },
            CreatedBy = speakerId, ModifiedBy = speakerId,
        });

        var requesterId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = requesterId, TenantId = tenantId, FullName = "Requester", Email = "req@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("P@ss"), Role = UserRole.Employee,
            IsActive = true, CreatedBy = requesterId, ModifiedBy = requesterId,
        });
        await db.SaveChangesAsync();

        var requesterCurrentUser = FakeCurrentUserAccessor.AsEmployee(requesterId, tenantId);
        var service = CreateService(db, requesterCurrentUser);

        var result = await service.RequestBookingAsync(
            new RequestBookingRequest(slotId, "Testing best practices", null),
            CancellationToken.None);

        Assert.Equal(BookingStatus.Pending, result.Status);
        Assert.Equal(speakerId, result.SpeakerUserId);
        Assert.Equal(requesterId, result.RequesterUserId);
    }

    [Fact]
    public async Task RequestBookingAsync_AlreadyBooked_ThrowsBusinessRuleException()
    {
        var (db, tenantId, speakerId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Contributor);

        var slotId = Guid.NewGuid();
        db.SpeakerAvailability.Add(new SpeakerAvailability
        {
            Id = slotId, TenantId = tenantId, UserId = speakerId,
            AvailableFrom = Future(1), AvailableTo = Future(3),
            IsBooked = true, // already taken
            Topics = new List<string>(),
            CreatedBy = speakerId, ModifiedBy = speakerId,
        });

        var requesterId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = requesterId, TenantId = tenantId, FullName = "R2", Email = "r2@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("P@ss"), Role = UserRole.Employee,
            IsActive = true, CreatedBy = requesterId, ModifiedBy = requesterId,
        });
        await db.SaveChangesAsync();

        var requesterCurrentUser = FakeCurrentUserAccessor.AsEmployee(requesterId, tenantId);
        var service = CreateService(db, requesterCurrentUser);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.RequestBookingAsync(new RequestBookingRequest(slotId, "Topic", null), CancellationToken.None));
    }

    // -- RespondToBooking -----------------------------------------------------

    [Fact]
    public async Task RespondToBookingAsync_SpeakerAccepts_UpdatesStatusToAccepted()
    {
        var (db, tenantId, speakerId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Contributor);
        var requesterId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = requesterId, TenantId = tenantId, FullName = "Req", Email = "req@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("P@ss"), Role = UserRole.Employee,
            IsActive = true, CreatedBy = requesterId, ModifiedBy = requesterId,
        });

        var slotId = Guid.NewGuid();
        db.SpeakerAvailability.Add(new SpeakerAvailability
        {
            Id = slotId, TenantId = tenantId, UserId = speakerId,
            AvailableFrom = Future(1), AvailableTo = Future(3),
            IsBooked = false, Topics = new List<string>(),
            CreatedBy = speakerId, ModifiedBy = speakerId,
        });

        var bookingId = Guid.NewGuid();
        db.SpeakerBookings.Add(new SpeakerBooking
        {
            Id = bookingId, TenantId = tenantId, SpeakerAvailabilityId = slotId,
            SpeakerUserId = speakerId, RequesterUserId = requesterId,
            Topic = "Test", Status = BookingStatus.Pending,
            CreatedBy = requesterId, ModifiedBy = requesterId,
        });
        await db.SaveChangesAsync();

        var speakerCurrentUser = FakeCurrentUserAccessor.AsEmployee(speakerId, tenantId);
        var service = CreateService(db, speakerCurrentUser);

        var result = await service.RespondToBookingAsync(bookingId,
            new RespondToBookingRequest(true, "Happy to help!"),
            CancellationToken.None);

        Assert.Equal(BookingStatus.Accepted, result.Status);
        Assert.Equal("Happy to help!", result.ResponseNotes);
    }
}
