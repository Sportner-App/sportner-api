using System.ComponentModel.DataAnnotations.Schema;

namespace SportnerApi.Models;

[Table("events")]
public class Event
{
    [Column("id")]
    public Guid Id { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("created_by")]
    public Guid CreatedBy { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("sport_type")]
    public string SportType { get; set; } = string.Empty;

    [Column("event_date")]
    public DateTime EventDate { get; set; }

    [Column("max_players")]
    public int MaxPlayers { get; set; }

    [Column("address_text")]
    public string? AddressText { get; set; }

    /// <summary>
    /// PostGIS geography/point column. Ignored by EF when not using NetTopologySuite;
    /// use Latitude/Longitude for queries and writes.
    /// </summary>
    [Column("location")]
    [NotMapped]
    public string? Location { get; set; }

    [Column("participants_count")]
    public int ParticipantsCount { get; set; }

    [Column("latitude")]
    public double Latitude { get; set; }

    [Column("longitude")]
    public double Longitude { get; set; }

    public Profile? Organizer { get; set; }
    public ICollection<EventParticipant> Participants { get; set; } = [];
    public ICollection<Message> Messages { get; set; } = [];
    public ICollection<Review> Reviews { get; set; } = [];
}
