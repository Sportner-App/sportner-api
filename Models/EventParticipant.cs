using System.ComponentModel.DataAnnotations.Schema;

namespace SportnerApi.Models;

[Table("event_participants")]
public class EventParticipant
{
    [Column("id")]
    public Guid Id { get; set; }

    [Column("event_id")]
    public Guid EventId { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("status")]
    public string Status { get; set; } = "pending";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    public Event? Event { get; set; }
    public Profile? User { get; set; }
}
