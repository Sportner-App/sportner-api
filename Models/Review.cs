using System.ComponentModel.DataAnnotations.Schema;

namespace SportnerApi.Models;

[Table("reviews")]
public class Review
{
    [Column("id")]
    public Guid Id { get; set; }

    [Column("event_id")]
    public Guid EventId { get; set; }

    [Column("reviewer_id")]
    public Guid ReviewerId { get; set; }

    [Column("reviewed_id")]
    public Guid ReviewedId { get; set; }

    [Column("rating")]
    public int Rating { get; set; }

    [Column("comment")]
    public string? Comment { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    public Event? Event { get; set; }
    public Profile? Reviewer { get; set; }
    public Profile? Reviewed { get; set; }
}
