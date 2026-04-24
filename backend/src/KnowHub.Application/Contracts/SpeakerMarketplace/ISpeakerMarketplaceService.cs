using KnowHub.Application.Models;

namespace KnowHub.Application.Contracts.SpeakerMarketplace;

public interface ISpeakerMarketplaceService
{
    Task<SpeakerAvailabilityDto> SetAvailabilityAsync(SetAvailabilityRequest request, CancellationToken cancellationToken);
    Task<SpeakerAvailabilityDto> UpdateAvailabilityAsync(Guid availabilityId, UpdateAvailabilityRequest request, CancellationToken cancellationToken);
    Task DeleteAvailabilityAsync(Guid availabilityId, CancellationToken cancellationToken);
    Task<PagedResult<SpeakerAvailabilityDto>> GetAvailableSpeakersAsync(GetAvailableSpeakersRequest request, CancellationToken cancellationToken);
    Task<List<SpeakerAvailabilityDto>> GetMyAvailabilityAsync(CancellationToken cancellationToken);
    Task<SpeakerBookingDto> RequestBookingAsync(RequestBookingRequest request, CancellationToken cancellationToken);
    Task<PagedResult<SpeakerBookingDto>> GetMyBookingsAsync(GetMyBookingsRequest request, CancellationToken cancellationToken);
    Task<SpeakerBookingDto> RespondToBookingAsync(Guid bookingId, RespondToBookingRequest request, CancellationToken cancellationToken);
    Task<SpeakerBookingDto> CompleteBookingAsync(Guid bookingId, CancellationToken cancellationToken);
    Task<SpeakerBookingDto> LinkToSessionAsync(Guid bookingId, Guid sessionId, CancellationToken cancellationToken);
    Task<SpeakerBookingDto> AdminAssignAsync(AdminAssignRequest request, CancellationToken cancellationToken);
}
