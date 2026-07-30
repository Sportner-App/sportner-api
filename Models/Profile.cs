using System.ComponentModel.DataAnnotations.Schema;

namespace SportnerApi.Models;

[Table("profiles")]
public class Profile
{
    [Column("id")]
    public Guid Id { get; set; }

    // Nullable because legacy Supabase Auth rows may not have these values set.
    [Column("email")]
    public string? Email { get; set; }

    [Column("password_hash")]
    public string? PasswordHash { get; set; }

    [Column("full_name")]
    public string? FullName { get; set; }

    [Column("avatar_url")]
    public string? AvatarUrl { get; set; }

    [Column("bio")]
    public string? Bio { get; set; }

    [Column("sports", TypeName = "text[]")]
    public List<string>? Sports { get; set; }

    [Column("intro_video_url")]
    public string? IntroVideoUrl { get; set; }

    [Column("is_onboarded")]
    public bool IsOnboarded { get; set; }

    [Column("birth_date")]
    public DateTime? BirthDate { get; set; }

    [Column("skill_levels", TypeName = "jsonb")]
    public string? SkillLevels { get; set; }

    [Column("avg_rating")]
    public decimal? AvgRating { get; set; }

    [Column("review_count")]
    public int? ReviewCount { get; set; }

    [Column("push_token")]
    public string? PushToken { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Event> OrganizedEvents { get; set; } = [];
    public ICollection<EventParticipant> Participations { get; set; } = [];
    public ICollection<Message> Messages { get; set; } = [];
    public ICollection<Review> ReviewsGiven { get; set; } = [];
    public ICollection<Review> ReviewsReceived { get; set; } = [];
}
