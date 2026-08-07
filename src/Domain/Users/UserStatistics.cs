using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Users;

public class UserStatistics : AuditableEntity
{
    private UserStatistics()
    {
    }

    public Guid UserId { get; private set; }

    public int EventsJoined { get; private set; }

    public int EventsOrganized { get; private set; }

    public int EventsCompleted { get; private set; }

    public int EventsCancelled { get; private set; }

    public decimal AttendanceRate { get; private set; }

    public decimal AverageRating { get; private set; }

    public int TotalReviews { get; private set; }

    public int FriendsCount { get; private set; }

    public int PostsCount { get; private set; }

    public int BadgesCount { get; private set; }

    public static UserStatistics Create(Guid userId, DateTimeOffset utcNow)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        return new UserStatistics
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EventsJoined = 0,
            EventsOrganized = 0,
            EventsCompleted = 0,
            EventsCancelled = 0,
            AttendanceRate = 0m,
            AverageRating = 0m,
            TotalReviews = 0,
            FriendsCount = 0,
            PostsCount = 0,
            BadgesCount = 0,
            CreatedAt = utcNow
        };
    }

    public void IncreaseEventsJoined(DateTimeOffset utcNow)
    {
        EventsJoined = Increment(EventsJoined);
        Touch(utcNow);
    }

    public void DecreaseEventsJoined(DateTimeOffset utcNow)
    {
        EventsJoined = Decrement(EventsJoined, "Events joined");
        Touch(utcNow);
    }

    public void IncreaseHostedEvents(DateTimeOffset utcNow)
    {
        EventsOrganized = Increment(EventsOrganized);
        Touch(utcNow);
    }

    public void IncreaseCompletedEvents(DateTimeOffset utcNow)
    {
        EventsCompleted = Increment(EventsCompleted);
        Touch(utcNow);
    }

    public void IncreaseCancelledEvents(DateTimeOffset utcNow)
    {
        EventsCancelled = Increment(EventsCancelled);
        Touch(utcNow);
    }

    public void IncreaseReviewCount(DateTimeOffset utcNow)
    {
        TotalReviews = Increment(TotalReviews);
        Touch(utcNow);
    }

    public void IncreaseFriendsCount(DateTimeOffset utcNow)
    {
        FriendsCount = Increment(FriendsCount);
        Touch(utcNow);
    }

    public void DecreaseFriendsCount(DateTimeOffset utcNow)
    {
        FriendsCount = Decrement(FriendsCount, "Friends count");
        Touch(utcNow);
    }

    public void IncreasePostsCount(DateTimeOffset utcNow)
    {
        PostsCount = Increment(PostsCount);
        Touch(utcNow);
    }

    public void DecreasePostsCount(DateTimeOffset utcNow)
    {
        PostsCount = Decrement(PostsCount, "Posts count");
        Touch(utcNow);
    }

    public void IncreaseBadgesCount(DateTimeOffset utcNow)
    {
        BadgesCount = Increment(BadgesCount);
        Touch(utcNow);
    }

    public void UpdateAverageRating(decimal averageRating, DateTimeOffset utcNow)
    {
        if (averageRating is < 0m or > 5m)
        {
            throw new DomainException("Average rating must be between 0 and 5.");
        }

        AverageRating = decimal.Round(averageRating, 2, MidpointRounding.AwayFromZero);
        Touch(utcNow);
    }

    public void UpdateAttendanceRate(decimal attendanceRate, DateTimeOffset utcNow)
    {
        if (attendanceRate is < 0m or > 100m)
        {
            throw new DomainException("Attendance rate must be between 0 and 100.");
        }

        AttendanceRate = decimal.Round(attendanceRate, 2, MidpointRounding.AwayFromZero);
        Touch(utcNow);
    }

    private void Touch(DateTimeOffset utcNow)
    {
        UpdatedAt = utcNow;
    }

    private static int Increment(int value)
    {
        if (value == int.MaxValue)
        {
            throw new DomainException("Statistic value overflow.");
        }

        return value + 1;
    }

    private static int Decrement(int value, string fieldName)
    {
        if (value <= 0)
        {
            throw new DomainException($"{fieldName} cannot become negative.");
        }

        return value - 1;
    }
}
