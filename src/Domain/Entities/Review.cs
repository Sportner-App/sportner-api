namespace Sportner.Domain.Entities;

public class Review
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid ReviewerId { get; set; }
    public Guid ReviewedId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }

    public Event? Event { get; set; }
    public Profile? Reviewer { get; set; }
    public Profile? Reviewed { get; set; }
}
