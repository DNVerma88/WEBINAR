namespace KnowHub.Domain.Entities;

public class SpeakerAvailability : BaseEntity
{
    public Guid UserId { get; set; }
    public DateTime AvailableFrom { get; set; }
    public DateTime AvailableTo { get; set; }
    public bool IsRecurring { get; set; }
    public string? RecurrencePattern { get; set; }
    public List<string> Topics { get; set; } = new();
    public string? Notes { get; set; }
    public bool IsBooked { get; set; }

    public User? User { get; set; }
    public ICollection<SpeakerBooking> Bookings { get; set; } = new List<SpeakerBooking>();
}
