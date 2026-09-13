using Sportner.Domain.Common.Constants;
using Sportner.Domain.Common.Enums;

namespace Sportner.Infrastructure.Persistence.Seed;

/// <summary>
/// Canonical reference data seeded on startup. Keyed by stable identifiers
/// (sport slug, badge code, report reason code) so re-runs update in place instead of duplicating.
/// </summary>
internal static class SeedData
{
    internal sealed record CitySeed(short PlateCode, string Name);

    internal sealed record SportCategorySeed(
        string Name,
        string Slug,
        int DisplayOrder,
        string? NameEn = null);

    /// <param name="CategorySlug">
    /// Matches a <see cref="SportCategorySeed.Slug"/>; the seeder resolves it to the category id.
    /// </param>
    /// <param name="LegacySlug">
    /// Slug used by an earlier seed revision. When present in the database the row is renamed in
    /// place so existing foreign keys (events, user sports) keep pointing at the same sport.
    /// </param>
    internal sealed record SportSeed(
        string Name,
        string Slug,
        int DisplayOrder,
        string CategorySlug,
        string? LegacySlug = null,
        string? NameEn = null);

    internal sealed record BadgeSeed(
        string Code,
        string Name,
        string Description,
        string IconPath,
        BadgeCategory Category,
        BadgeRarity Rarity,
        int ExperiencePoints,
        short DisplayOrder,
        string? NameEn = null,
        string? DescriptionEn = null);

    internal sealed record ReportReasonSeed(
        string Code,
        string Name,
        string? Description,
        short DisplayOrder);

    internal sealed record QuestSeed(
        string Code,
        string Title,
        string Description,
        string MetricCode,
        int TargetValue,
        string RewardBadgeCode,
        short SortOrder,
        string? TitleEn = null,
        string? DescriptionEn = null);

    internal static readonly IReadOnlyList<CitySeed> Cities = new CitySeed[]
    {
        new(1, "Adana"), new(2, "Adıyaman"), new(3, "Afyonkarahisar"), new(4, "Ağrı"),
        new(5, "Amasya"), new(6, "Ankara"), new(7, "Antalya"), new(8, "Artvin"),
        new(9, "Aydın"), new(10, "Balıkesir"), new(11, "Bilecik"), new(12, "Bingöl"),
        new(13, "Bitlis"), new(14, "Bolu"), new(15, "Burdur"), new(16, "Bursa"),
        new(17, "Çanakkale"), new(18, "Çankırı"), new(19, "Çorum"), new(20, "Denizli"),
        new(21, "Diyarbakır"), new(22, "Edirne"), new(23, "Elazığ"), new(24, "Erzincan"),
        new(25, "Erzurum"), new(26, "Eskişehir"), new(27, "Gaziantep"), new(28, "Giresun"),
        new(29, "Gümüşhane"), new(30, "Hakkari"), new(31, "Hatay"), new(32, "Isparta"),
        new(33, "Mersin"), new(34, "İstanbul"), new(35, "İzmir"), new(36, "Kars"),
        new(37, "Kastamonu"), new(38, "Kayseri"), new(39, "Kırklareli"), new(40, "Kırşehir"),
        new(41, "Kocaeli"), new(42, "Konya"), new(43, "Kütahya"), new(44, "Malatya"),
        new(45, "Manisa"), new(46, "Kahramanmaraş"), new(47, "Mardin"), new(48, "Muğla"),
        new(49, "Muş"), new(50, "Nevşehir"), new(51, "Niğde"), new(52, "Ordu"),
        new(53, "Rize"), new(54, "Sakarya"), new(55, "Samsun"), new(56, "Siirt"),
        new(57, "Sinop"), new(58, "Sivas"), new(59, "Tekirdağ"), new(60, "Tokat"),
        new(61, "Trabzon"), new(62, "Tunceli"), new(63, "Şanlıurfa"), new(64, "Uşak"),
        new(65, "Van"), new(66, "Yozgat"), new(67, "Zonguldak"), new(68, "Aksaray"),
        new(69, "Bayburt"), new(70, "Karaman"), new(71, "Kırıkkale"), new(72, "Batman"),
        new(73, "Şırnak"), new(74, "Bartın"), new(75, "Ardahan"), new(76, "Iğdır"),
        new(77, "Yalova"), new(78, "Karabük"), new(79, "Kilis"), new(80, "Osmaniye"),
        new(81, "Düzce")
    };

    // Slugs stay ASCII because they are URL identifiers; display names are Turkish.
    internal static readonly IReadOnlyList<SportCategorySeed> SportCategories =
        new SportCategorySeed[]
    {
        new("Takım Sporları", "takim-sporlari", 1, NameEn: "Team Sports"),
        new("Raket Sporları", "raket-sporlari", 2, NameEn: "Racket Sports"),
        new("Fitness & Kondisyon", "fitness-kondisyon", 3, NameEn: "Fitness & Conditioning"),
        new("Dövüş Sporları", "dovus-sporlari", 4, NameEn: "Combat Sports"),
        new("Outdoor & Dayanıklılık", "outdoor-dayaniklilik", 5, NameEn: "Outdoor & Endurance"),
        new("Su Sporları", "su-sporlari", 6, NameEn: "Water Sports"),
        new("Kış Sporları", "kis-sporlari", 7, NameEn: "Winter Sports"),
        new("Hedef Sporları", "hedef-sporlari", 8, NameEn: "Target Sports"),
        new("Diğer", "diger", 9, NameEn: "Other")
    };

    internal static readonly IReadOnlyList<SportSeed> Sports = new SportSeed[]
    {
        new("Basketbol", "basketbol", 1, "takim-sporlari", LegacySlug: "basketball", NameEn: "Basketball"),
        new("Futbol", "futbol", 2, "takim-sporlari", LegacySlug: "football", NameEn: "Football"),
        new("Voleybol", "voleybol", 3, "takim-sporlari", LegacySlug: "volleyball", NameEn: "Volleyball"),
        new("Tenis", "tenis", 4, "raket-sporlari", LegacySlug: "tennis", NameEn: "Tennis"),
        new("Masa Tenisi", "masa-tenisi", 5, "raket-sporlari", LegacySlug: "table-tennis", NameEn: "Table Tennis"),
        new("Koşu", "kosu", 6, "outdoor-dayaniklilik", LegacySlug: "running", NameEn: "Running"),
        new("Bisiklet", "bisiklet", 7, "outdoor-dayaniklilik", LegacySlug: "cycling", NameEn: "Cycling"),
        new("Yüzme", "yuzme", 8, "su-sporlari", LegacySlug: "swimming", NameEn: "Swimming"),
        new("Fitness", "fitness", 9, "fitness-kondisyon", NameEn: "Fitness"),
        new("Doğa Yürüyüşü", "doga-yuruyusu", 10, "outdoor-dayaniklilik", LegacySlug: "hiking", NameEn: "Hiking"),
        new("Boks", "boks", 11, "dovus-sporlari", LegacySlug: "boxing", NameEn: "Boxing"),
        new("Pilates", "pilates", 12, "fitness-kondisyon", NameEn: "Pilates"),
        new("Yoga", "yoga", 13, "fitness-kondisyon", NameEn: "Yoga"),
        new("CrossFit", "crossfit", 14, "fitness-kondisyon", NameEn: "CrossFit"),
        new("Badminton", "badminton", 15, "raket-sporlari", NameEn: "Badminton"),
        new("Padel", "padel", 16, "raket-sporlari", NameEn: "Padel"),
        new("Pickleball", "pickleball", 17, "raket-sporlari", NameEn: "Pickleball"),
        new("Squash", "squash", 18, "raket-sporlari", NameEn: "Squash"),
        new("Hentbol", "hentbol", 19, "takim-sporlari", NameEn: "Handball"),
        new("Plaj Voleybolu", "plaj-voleybolu", 20, "takim-sporlari", NameEn: "Beach Volleyball"),
        new("Kickboks", "kickboks", 21, "dovus-sporlari", NameEn: "Kickboxing"),
        new("Judo", "judo", 22, "dovus-sporlari", NameEn: "Judo"),
        new("Jiu-Jitsu", "jiu-jitsu", 23, "dovus-sporlari", NameEn: "Jiu-Jitsu"),
        new("Karate", "karate", 24, "dovus-sporlari", NameEn: "Karate"),
        new("Tırmanış", "tirmanis", 25, "outdoor-dayaniklilik", NameEn: "Climbing"),
        new("Kayak", "kayak", 26, "kis-sporlari", NameEn: "Skiing"),
        new("Snowboard", "snowboard", 27, "kis-sporlari", NameEn: "Snowboard"),
        new("Bowling", "bowling", 28, "hedef-sporlari", NameEn: "Bowling"),
        new("Dans", "dans", 29, "fitness-kondisyon", NameEn: "Dance"),
        new("Golf", "golf", 30, "hedef-sporlari", NameEn: "Golf"),
        new("Okçuluk", "okculuk", 31, "hedef-sporlari", NameEn: "Archery"),
        new("Dalış", "dalis", 32, "su-sporlari", NameEn: "Diving"),
        new("Yelken", "yelken", 33, "su-sporlari", NameEn: "Sailing"),
        new("Rugby", "rugby", 34, "takim-sporlari", NameEn: "Rugby"),
        new("Kürek", "kurek", 35, "su-sporlari", NameEn: "Rowing"),
        // Katalogda olmayan bir spor için etkinlik açmak isteyenlere kaçış yolu;
        // her zaman listenin sonunda görünsün diye yüksek bir sıralama numarası.
        new("Diğer", "diger", 999, "diger", NameEn: "Other")
    };

    internal static readonly IReadOnlyList<BadgeSeed> Badges = new BadgeSeed[]
    {
        new(BadgeCodes.FirstEvent, "İlk Etkinlik", "İlk etkinliğine katıldın.",
            "badges/first-event.png", BadgeCategory.Events, BadgeRarity.Common, 50, 1,
            NameEn: "First Event", DescriptionEn: "You attended your first event."),
        new(BadgeCodes.FirstPost, "İlk Gönderi", "İlk gönderini paylaştın.",
            "badges/first-post.png", BadgeCategory.Social, BadgeRarity.Common, 25, 2,
            NameEn: "First Post", DescriptionEn: "You shared your first post."),
        new(BadgeCodes.FirstFriend, "İlk Arkadaş", "İlk arkadaşını edindin.",
            "badges/first-friend.png", BadgeCategory.Social, BadgeRarity.Common, 25, 3,
            NameEn: "First Friend", DescriptionEn: "You made your first friend."),
        new(BadgeCodes.FirstReview, "İlk Değerlendirme", "İlk değerlendirmeni yazdın.",
            "badges/first-review.png", BadgeCategory.Community, BadgeRarity.Common, 25, 4,
            NameEn: "First Review", DescriptionEn: "You wrote your first review."),
        new(BadgeCodes.CommunityHelper, "Topluluk Destekçisi", "Topluluğa katkıların için takdir edildin.",
            "badges/community-helper.png", BadgeCategory.Community, BadgeRarity.Rare, 100, 5,
            NameEn: "Community Helper", DescriptionEn: "Recognized for your contributions to the community."),
        new(BadgeCodes.SportsExplorer, "Spor Kâşifi", "Birçok farklı sporu denedin.",
            "badges/sports-explorer.png", BadgeCategory.Sports, BadgeRarity.Rare, 100, 6,
            NameEn: "Sports Explorer", DescriptionEn: "You've tried many different sports."),
        new(BadgeCodes.EventMaster, "Etkinlik Ustası", "Çok sayıda etkinliğe katıldın.",
            "badges/event-master.png", BadgeCategory.Events, BadgeRarity.Epic, 250, 7,
            NameEn: "Event Master", DescriptionEn: "You attended a large number of events."),
        new(BadgeCodes.MarathonRunner, "Maratoncu", "Uzun süreli bir etkinlik serisini sürdürdün.",
            "badges/marathon-runner.png", BadgeCategory.Streak, BadgeRarity.Legendary, 500, 8,
            NameEn: "Marathon Runner", DescriptionEn: "You kept up a long streak of events."),
        new(BadgeCodes.SocialButterfly, "Sosyal Kelebek", "Geniş bir arkadaş çevresi kurdun.",
            "badges/social-butterfly.png", BadgeCategory.Social, BadgeRarity.Rare, 150, 9,
            NameEn: "Social Butterfly", DescriptionEn: "You built a wide circle of friends."),
        new(BadgeCodes.HostHero, "Ev Sahibi Kahraman", "Birçok etkinliği başarıyla tamamladın.",
            "badges/host-hero.png", BadgeCategory.Events, BadgeRarity.Epic, 200, 10,
            NameEn: "Host Hero", DescriptionEn: "You successfully completed many events."),
        new(BadgeCodes.ReviewGuru, "Değerlendirme Ustası", "Çok sayıda değerlendirme yazdın.",
            "badges/review-guru.png", BadgeCategory.Community, BadgeRarity.Rare, 150, 11,
            NameEn: "Review Guru", DescriptionEn: "You've written a large number of reviews."),
        new(BadgeCodes.EarlyBird, "Erken Kalkan", "Sabah erken başlayan etkinliklere katıldın.",
            "badges/early-bird.png", BadgeCategory.Events, BadgeRarity.Rare, 150, 12,
            NameEn: "Early Bird", DescriptionEn: "You attended events that started early in the morning.")
    };

    internal static readonly IReadOnlyList<ReportReasonSeed> ReportReasons = new ReportReasonSeed[]
    {
        new(ReportReasonCodes.Spam, "Spam", "İstenmeyen veya tekrarlayan içerik.", 1),
        new(ReportReasonCodes.Harassment, "Taciz", "Zorbalık veya hedefli kötüye kullanım.", 2),
        new(ReportReasonCodes.HateSpeech, "Nefret Söylemi", "Korunan gruplara yönelik saldırı.", 3),
        new(ReportReasonCodes.InappropriateContent, "Uygunsuz İçerik", "Topluluk kurallarını ihlal eden içerik.", 4),
        new(ReportReasonCodes.Violence, "Şiddet", "Şiddet tehdidi veya şiddet içeren tasvir.", 5),
        new(ReportReasonCodes.Nudity, "Müstehcenlik", "Cinsel içerik veya çıplaklık.", 6),
        new(ReportReasonCodes.FakeInformation, "Yanlış Bilgi", "Yanıltıcı veya asılsız bilgi.", 7),
        new(ReportReasonCodes.Impersonation, "Sahte Hesap", "Başkasının kimliğine bürünme.", 8),
        new(ReportReasonCodes.Scam, "Dolandırıcılık", "Hileli veya aldatıcı davranış.", 9),
        new(ReportReasonCodes.Other, "Diğer", "Diğer nedenlerle kapsanmayan durumlar.", 10)
    };

    internal static readonly IReadOnlyList<QuestSeed> Quests = new QuestSeed[]
    {
        new(QuestCodes.Attend3, "3 etkinliğe katıl", "Üç etkinlikte katılımını onaylat.",
            QuestMetrics.EventsAttended, 3, BadgeCodes.FirstEvent, 1,
            TitleEn: "Attend 3 events", DescriptionEn: "Get your attendance confirmed at three events."),
        new(QuestCodes.Post5, "5 gönderi paylaş", "Beş gönderi oluştur.",
            QuestMetrics.PostsCreated, 5, BadgeCodes.FirstPost, 2,
            TitleEn: "Share 5 posts", DescriptionEn: "Create five posts."),
        new(QuestCodes.MakeFriends5, "5 arkadaş edin", "Beş arkadaşlık kur.",
            QuestMetrics.FriendsAccepted, 5, BadgeCodes.FirstFriend, 3,
            TitleEn: "Make 5 friends", DescriptionEn: "Form five friendships."),
        new(QuestCodes.Host1, "Bir etkinlik tamamla", "Organize ettiğin bir etkinliği tamamla.",
            QuestMetrics.EventsOrganizedCompleted, 1, BadgeCodes.HostHero, 4,
            TitleEn: "Complete an event", DescriptionEn: "Complete an event you organized."),
        new(QuestCodes.Review3, "3 değerlendirme yaz", "Üç değerlendirme bırak.",
            QuestMetrics.ReviewsCreated, 3, BadgeCodes.FirstReview, 5,
            TitleEn: "Write 3 reviews", DescriptionEn: "Leave three reviews.")
    };
}
