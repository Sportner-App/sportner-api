using System.ComponentModel.DataAnnotations.Schema;

namespace SportnerApi.Models;

[Table("messages")]
public class Message
{
    [Column("id")]
    public Guid Id { get; set; }

    [Column("event_id")]
    public Guid EventId { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    public Event? Event { get; set; }
    public Profile? User { get; set; }
}
