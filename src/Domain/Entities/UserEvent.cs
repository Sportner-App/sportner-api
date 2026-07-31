namespace Sportner.Domain.Entities;

public class UserEvent
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime CreatedAt { get; set; }

    public Event? Event { get; set; }
    public User? User { get; set; }
}
