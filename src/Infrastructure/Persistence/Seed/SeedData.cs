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

    /// <param name="LegacySlug">
    /// Slug used by an earlier seed revision. When present in the database the row is renamed in
    /// place so existing foreign keys (events, user sports) keep pointing at the same sport.
    /// </param>
    internal sealed record SportSeed(
        string Name,
        string Slug,
        int DisplayOrder,
        string? LegacySlug = null);

    internal sealed record BadgeSeed(
        string Code,
        string Name,
        string Description,
        string IconPath,
        BadgeCategory Category,
        BadgeRarity Rarity,
        int ExperiencePoints,
        short DisplayOrder);

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
        short SortOrder);

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
    internal static readonly IReadOnlyList<SportSeed> Sports = new SportSeed[]
    {
        new("Basketbol", "basketbol", 1, LegacySlug: "basketball"),
        new("Futbol", "futbol", 2, LegacySlug: "football"),
        new("Voleybol", "voleybol", 3, LegacySlug: "volleyball"),
        new("Tenis", "tenis", 4, LegacySlug: "tennis"),
        new("Masa Tenisi", "masa-tenisi", 5, LegacySlug: "table-tennis"),
        new("Koşu", "kosu", 6, LegacySlug: "running"),
        new("Bisiklet", "bisiklet", 7, LegacySlug: "cycling"),
        new("Yüzme", "yuzme", 8, LegacySlug: "swimming"),
        new("Fitness", "fitness", 9),
        new("Doğa Yürüyüşü", "doga-yuruyusu", 10, LegacySlug: "hiking"),
        new("Boks", "boks", 11, LegacySlug: "boxing"),
        new("Pilates", "pilates", 12),
        new("Yoga", "yoga", 13),
        new("CrossFit", "crossfit", 14),
        new("Badminton", "badminton", 15),
        new("Padel", "padel", 16),
        new("Pickleball", "pickleball", 17),
        new("Squash", "squash", 18),
        new("Hentbol", "hentbol", 19),
        new("Plaj Voleybolu", "plaj-voleybolu", 20),
        new("Kickboks", "kickboks", 21),
        new("Judo", "judo", 22),
        new("Jiu-Jitsu", "jiu-jitsu", 23),
        new("Karate", "karate", 24),
        new("Tırmanış", "tirmanis", 25),
        new("Kayak", "kayak", 26),
        new("Snowboard", "snowboard", 27),
        new("Bowling", "bowling", 28),
        new("Dans", "dans", 29),
        new("Golf", "golf", 30),
        new("Okçuluk", "okculuk", 31),
        new("Dalış", "dalis", 32),
        new("Yelken", "yelken", 33),
        new("Rugby", "rugby", 34)
    };

    internal static readonly IReadOnlyList<BadgeSeed> Badges = new BadgeSeed[]
    {
        new(BadgeCodes.FirstEvent, "İlk Etkinlik", "İlk etkinliğine katıldın.",
            "badges/first-event.png", BadgeCategory.Events, BadgeRarity.Common, 50, 1),
        new(BadgeCodes.FirstPost, "İlk Gönderi", "İlk gönderini paylaştın.",
            "badges/first-post.png", BadgeCategory.Social, BadgeRarity.Common, 25, 2),
        new(BadgeCodes.FirstFriend, "İlk Arkadaş", "İlk arkadaşını edindin.",
            "badges/first-friend.png", BadgeCategory.Social, BadgeRarity.Common, 25, 3),
        new(BadgeCodes.FirstReview, "İlk Değerlendirme", "İlk değerlendirmeni yazdın.",
            "badges/first-review.png", BadgeCategory.Community, BadgeRarity.Common, 25, 4),
        new(BadgeCodes.CommunityHelper, "Topluluk Destekçisi", "Topluluğa katkıların için takdir edildin.",
            "badges/community-helper.png", BadgeCategory.Community, BadgeRarity.Rare, 100, 5),
        new(BadgeCodes.SportsExplorer, "Spor Kâşifi", "Birçok farklı sporu denedin.",
            "badges/sports-explorer.png", BadgeCategory.Sports, BadgeRarity.Rare, 100, 6),
        new(BadgeCodes.EventMaster, "Etkinlik Ustası", "Çok sayıda etkinliğe katıldın.",
            "badges/event-master.png", BadgeCategory.Events, BadgeRarity.Epic, 250, 7),
        new(BadgeCodes.MarathonRunner, "Maratoncu", "Uzun süreli bir etkinlik serisini sürdürdün.",
            "badges/marathon-runner.png", BadgeCategory.Streak, BadgeRarity.Legendary, 500, 8),
        new(BadgeCodes.SocialButterfly, "Sosyal Kelebek", "Geniş bir arkadaş çevresi kurdun.",
            "badges/social-butterfly.png", BadgeCategory.Social, BadgeRarity.Rare, 150, 9),
        new(BadgeCodes.HostHero, "Ev Sahibi Kahraman", "Birçok etkinliği başarıyla tamamladın.",
            "badges/host-hero.png", BadgeCategory.Events, BadgeRarity.Epic, 200, 10),
        new(BadgeCodes.ReviewGuru, "Değerlendirme Ustası", "Çok sayıda değerlendirme yazdın.",
            "badges/review-guru.png", BadgeCategory.Community, BadgeRarity.Rare, 150, 11),
        new(BadgeCodes.EarlyBird, "Erken Kalkan", "Sabah erken başlayan etkinliklere katıldın.",
            "badges/early-bird.png", BadgeCategory.Events, BadgeRarity.Rare, 150, 12)
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
            QuestMetrics.EventsAttended, 3, BadgeCodes.FirstEvent, 1),
        new(QuestCodes.Post5, "5 gönderi paylaş", "Beş gönderi oluştur.",
            QuestMetrics.PostsCreated, 5, BadgeCodes.FirstPost, 2),
        new(QuestCodes.MakeFriends5, "5 arkadaş edin", "Beş arkadaşlık kur.",
            QuestMetrics.FriendsAccepted, 5, BadgeCodes.FirstFriend, 3),
        new(QuestCodes.Host1, "Bir etkinlik tamamla", "Organize ettiğin bir etkinliği tamamla.",
            QuestMetrics.EventsOrganizedCompleted, 1, BadgeCodes.HostHero, 4),
        new(QuestCodes.Review3, "3 değerlendirme yaz", "Üç değerlendirme bırak.",
            QuestMetrics.ReviewsCreated, 3, BadgeCodes.FirstReview, 5)
    };
}
