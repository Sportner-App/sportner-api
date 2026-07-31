namespace Sportner.Domain.Entities;

public class Event
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SportType { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public int MaxPlayers { get; set; }
    public string? AddressText { get; set; }
    public int ParticipantsCount { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public User? Organizer { get; set; }
    public ICollection<UserEvent> UserEvents { get; set; } = [];
    public ICollection<Message> Messages { get; set; } = [];
    public ICollection<Review> Reviews { get; set; } = [];
}
