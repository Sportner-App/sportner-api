namespace Sportner.Application.Services.Recommendations;

public sealed class RecommendationOptions
{
    public const string SectionName = "Recommendation";

    public int PeopleCandidateCap { get; set; } = 200;

    public int EventsCandidateCap { get; set; } = 300;

    public int PostsCandidateCap { get; set; } = 300;

    public PeopleWeights People { get; set; } = new();

    public EventsWeights Events { get; set; } = new();

    public PostsWeights Posts { get; set; } = new();

    public sealed class PeopleWeights
    {
        public double MutualFriends { get; set; } = 5.0;

        public double SharedSports { get; set; } = 2.0;

        public double SameCity { get; set; } = 1.0;

        public double Reputation { get; set; } = 0.5;

        public double Activity { get; set; } = 0.25;
    }

    public sealed class EventsWeights
    {
        public double SportMatch { get; set; } = 4.0;

        public double Distance { get; set; } = 3.0;

        public double FriendsAttending { get; set; } = 2.5;

        public double TimeFit { get; set; } = 1.5;

        public double FillRatio { get; set; } = 1.0;

        public double OrganizerRep { get; set; } = 0.5;
    }

    public sealed class PostsWeights
    {
        public double Recency { get; set; } = 3.0;

        public double Engagement { get; set; } = 2.0;

        public double AuthorFriend { get; set; } = 2.5;

        public double AuthorRep { get; set; } = 0.5;
    }
}
