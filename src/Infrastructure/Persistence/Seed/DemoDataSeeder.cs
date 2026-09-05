using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Messaging;
using Sportner.Domain.Notifications;
using Sportner.Domain.Social;
using Sportner.Domain.Sports;
using Sportner.Domain.Users;
using DomainEvent = Sportner.Domain.Events.Event;

namespace Sportner.Infrastructure.Persistence.Seed;

/// <summary>
/// Development-only demo data. Idempotent: presence of the first demo phone number short-circuits
/// bulk seeding; password hashes are backfilled on later runs if missing.
/// </summary>
public sealed class DemoDataSeeder : IDemoDataSeeder
{
    public const string DemoPassword = "Demo123!";

    private const string DemoPhonePrefix = "+90555000000";

    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DemoDataSeeder> _logger;

    public DemoDataSeeder(
        AppDbContext dbContext,
        TimeProvider timeProvider,
        IPasswordHasher passwordHasher,
        ILogger<DemoDataSeeder> logger)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var markerPhone = $"{DemoPhonePrefix}1";

        var alreadySeeded = await _dbContext.Users
            .AnyAsync(user => user.PhoneNumber == markerPhone, cancellationToken);

        if (alreadySeeded)
        {
            await EnsureDemoPasswordsAsync(cancellationToken);
            _logger.LogInformation("Demo data seeding skipped: demo users already exist.");
            return;
        }

        var sports = await _dbContext.Sports
            .OrderBy(sport => sport.DisplayOrder)
            .ToListAsync(cancellationToken);

        if (sports.Count == 0)
        {
            _logger.LogWarning("Demo data seeding skipped: no sports found. Run reference seeding first.");
            return;
        }

        var utcNow = _timeProvider.GetUtcNow();

        var users = CreateUsers(sports, utcNow);
        CreateSocialGraph(users, utcNow);
        var basketballEvent = CreateEvents(users, sports, utcNow);
        CreateEventConversation(basketballEvent, users, utcNow);
        CreatePosts(users, utcNow);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Demo data seeded: {UserCount} users with events, chat and posts.", users.Count);
    }

    private async Task EnsureDemoPasswordsAsync(CancellationToken cancellationToken)
    {
        var demoUsers = await _dbContext.Users
            .Where(user => user.PhoneNumber != null && user.PhoneNumber.StartsWith(DemoPhonePrefix))
            .ToListAsync(cancellationToken);

        if (demoUsers.Count == 0)
        {
            return;
        }

        var utcNow = _timeProvider.GetUtcNow();
        var hash = _passwordHasher.Hash(DemoPassword);
        var updated = 0;

        foreach (var user in demoUsers.Where(user => string.IsNullOrWhiteSpace(user.PasswordHash)))
        {
            user.SetPasswordHash(hash, utcNow);
            updated++;
        }

        if (updated > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Backfilled password hash for {Count} demo users.", updated);
        }
    }
    private List<User> CreateUsers(IReadOnlyList<Sport> sports, DateTimeOffset utcNow)
    {
        // Gender/BirthDate zorunlu: doğum tarihi olmayan bir profil etkinlik yaş
        // kontrolüne takılır (davet ve katılım reddedilir), yani demo kullanıcı
        // bunlarsız kullanılamaz durumda doğar.
        var definitions = new[]
        {
            (Index: 1, Username: "ahmet", FirstName: "Ahmet", LastName: "Yılmaz", City: "İstanbul",
                Bio: "Basketbol ve koşu. Hafta sonları sahadayım.",
                Gender: (short)1, BirthDate: new DateOnly(1995, 4, 12)),
            (Index: 2, Username: "elif", FirstName: "Elif", LastName: "Demir", City: "İstanbul",
                Bio: "Voleybol ve pilates yapıyorum.",
                Gender: (short)2, BirthDate: new DateOnly(1998, 9, 3)),
            (Index: 3, Username: "mert", FirstName: "Mert", LastName: "Kaya", City: "Ankara",
                Bio: "Koşu ve bisiklet. Sürekli yeni rotalar arıyorum.",
                Gender: (short)1, BirthDate: new DateOnly(1992, 1, 27)),
            (Index: 4, Username: "zeynep", FirstName: "Zeynep", LastName: "Şahin", City: "İzmir",
                Bio: "Tenis ve yoga.",
                Gender: (short)2, BirthDate: new DateOnly(2000, 6, 18))
        };

        var users = new List<User>();

        foreach (var definition in definitions)
        {
            var user = User.Create($"{DemoPhonePrefix}{definition.Index}", utcNow);
            user.VerifyPhoneNumber(utcNow);
            user.Activate(utcNow);
            user.SetPasswordHash(_passwordHasher.Hash(DemoPassword), utcNow);

            var profile = UserProfile.Create(
                user.Id,
                definition.Username,
                definition.FirstName,
                utcNow,
                definition.LastName);

            profile.UpdateBio(definition.Bio, utcNow);
            profile.UpdateLocation(definition.City, utcNow);
            profile.UpdatePersonalDetails(
                definition.Gender,
                definition.BirthDate,
                utcNow);
            user.AttachUserProfile(profile);

            var primarySport = sports[(definition.Index - 1) % sports.Count];
            var secondarySport = sports[definition.Index % sports.Count];

            user.AddSport(primarySport.Id, SkillLevel.Intermediate, utcNow, isPrimary: true);

            if (secondarySport.Id != primarySport.Id)
            {
                user.AddSport(secondarySport.Id, SkillLevel.Beginner, utcNow);
            }

            user.CompleteOnboarding(utcNow);
            user.UpdateLastLogin(utcNow);

            _dbContext.Users.Add(user);
            AddDefaultNotificationSettings(user.Id, utcNow);

            users.Add(user);
        }

        return users;
    }

    private void CreateSocialGraph(IReadOnlyList<User> users, DateTimeOffset utcNow)
    {
        var accepted = Friendship.CreateRequest(users[0].Id, users[1].Id, utcNow);
        accepted.Accept(utcNow);
        users[0].Statistics!.IncreaseFriendsCount(utcNow);
        users[1].Statistics!.IncreaseFriendsCount(utcNow);

        var pending = Friendship.CreateRequest(users[2].Id, users[0].Id, utcNow);

        _dbContext.Friendships.AddRange(accepted, pending);
    }

    private DomainEvent CreateEvents(
        IReadOnlyList<User> users,
        IReadOnlyList<Sport> sports,
        DateTimeOffset utcNow)
    {
        // Open event with free capacity: one approved participant and one still pending.
        var openEvent = DomainEvent.Create(
            users[0].Id,
            FindSport(sports, "basketbol").Id,
            "Pazar sabahı basketbol maçı",
            utcNow.AddDays(3),
            90,
            41.015137m,
            28.979530m,
            "Kadıköy Spor Salonu, İstanbul",
            utcNow,
            description: "Dostluk maçı, her seviyeye açık.",
            maxParticipants: 6);

        openEvent.Publish(utcNow);
        openEvent.Apply(users[1].Id, utcNow);
        openEvent.ApproveParticipant(users[1].Id, utcNow);
        openEvent.Apply(users[2].Id, utcNow);

        users[0].Statistics!.IncreaseHostedEvents(utcNow);
        users[1].Statistics!.IncreaseEventsJoined(utcNow);

        // Tight capacity event so the waitlist path has data too.
        var fullEvent = DomainEvent.Create(
            users[1].Id,
            FindSport(sports, "kosu").Id,
            "Sahil boyunca akşam koşusu",
            utcNow.AddDays(7),
            60,
            38.423733m,
            27.142826m,
            "Kordon, İzmir",
            utcNow,
            description: "Sakin tempo, yaklaşık 8 km.",
            maxParticipants: 2);

        fullEvent.Publish(utcNow);
        fullEvent.Apply(users[2].Id, utcNow);
        fullEvent.ApproveParticipant(users[2].Id, utcNow);
        fullEvent.Apply(users[3].Id, utcNow);

        users[1].Statistics!.IncreaseHostedEvents(utcNow);
        users[2].Statistics!.IncreaseEventsJoined(utcNow);

        _dbContext.Events.AddRange(openEvent, fullEvent);

        return openEvent;
    }

    private void CreateEventConversation(
        DomainEvent openEvent,
        IReadOnlyList<User> users,
        DateTimeOffset utcNow)
    {
        var conversation = Conversation.CreateEventConversation(
            openEvent.Id,
            users[0].Id,
            utcNow,
            openEvent.Title);

        conversation.AddMember(users[1].Id, utcNow);

        _dbContext.Conversations.Add(conversation);

        _dbContext.Messages.AddRange(
            Message.CreateText(conversation.Id, users[0].Id, "Herkese merhaba, salonu ayırttım.", utcNow),
            Message.CreateText(conversation.Id, users[1].Id, "Harika, ben 30 dakika önce orada olurum.", utcNow.AddMinutes(2)),
            Message.CreateText(conversation.Id, users[0].Id, "Top ve yelek bende, siz sadece gelin.", utcNow.AddMinutes(5)));
    }

    private void CreatePosts(IReadOnlyList<User> users, DateTimeOffset utcNow)
    {
        var firstPost = Post.Create(
            users[0].Id,
            "Bu haftaki maç için kadro neredeyse tamam. Bir kişilik yer kaldı!",
            utcNow);

        var secondPost = Post.Create(
            users[2].Id,
            "Sabah koşusu 10 km. Hava mükemmeldi.",
            utcNow.AddHours(1));

        var comment = PostComment.CreateRoot(
            firstPost.Id,
            users[1].Id,
            "Ben varım, saat kaçta başlıyoruz?",
            utcNow.AddMinutes(10));

        var reply = PostComment.CreateReply(
            firstPost.Id,
            users[0].Id,
            comment.Id,
            "10:00 gibi başlarız.",
            utcNow.AddMinutes(15));

        comment.IncrementReplyCount(utcNow.AddMinutes(15));
        firstPost.IncrementCommentCount(utcNow.AddMinutes(15), amount: 2);

        var likes = new[]
        {
            PostLike.Create(firstPost.Id, users[1].Id, utcNow.AddMinutes(11)),
            PostLike.Create(firstPost.Id, users[2].Id, utcNow.AddMinutes(12)),
            PostLike.Create(secondPost.Id, users[0].Id, utcNow.AddHours(1).AddMinutes(5))
        };

        firstPost.IncrementLikeCount(utcNow.AddMinutes(12), amount: 2);
        secondPost.IncrementLikeCount(utcNow.AddHours(1).AddMinutes(5));

        users[0].Statistics!.IncreasePostsCount(utcNow);
        users[2].Statistics!.IncreasePostsCount(utcNow);

        _dbContext.Posts.AddRange(firstPost, secondPost);
        _dbContext.PostComments.AddRange(comment, reply);
        _dbContext.PostLikes.AddRange(likes);
    }

    private static Sport FindSport(IReadOnlyList<Sport> sports, string slug) =>
        sports.FirstOrDefault(sport =>
            string.Equals(sport.Slug, slug, StringComparison.OrdinalIgnoreCase))
        ?? sports[0];

    private void AddDefaultNotificationSettings(Guid userId, DateTimeOffset utcNow)
    {
        foreach (var notificationType in Enum.GetValues<NotificationType>())
        {
            _dbContext.NotificationSettings.Add(
                NotificationSetting.CreateDefault(userId, notificationType, utcNow));
        }
    }
}
