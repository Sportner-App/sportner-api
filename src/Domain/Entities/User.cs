namespace Sportner.Domain.Entities;

public class User
{
    public Guid Id { get; set; }

    /// <summary>Nullable because legacy Supabase Auth rows may not have these values set.</summary>
    public string? Email { get; set; }

    public string? PasswordHash { get; set; }
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public List<string>? Sports { get; set; }
    public string? IntroVideoUrl { get; set; }
    public bool IsOnboarded { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? SkillLevels { get; set; }
    public decimal? AvgRating { get; set; }
    public int? ReviewCount { get; set; }
    public string? PushToken { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Event> OrganizedEvents { get; set; } = [];
    public ICollection<UserEvent> UserEvents { get; set; } = [];
    public ICollection<Message> Messages { get; set; } = [];
    public ICollection<Review> ReviewsGiven { get; set; } = [];
    public ICollection<Review> ReviewsReceived { get; set; } = [];
}
