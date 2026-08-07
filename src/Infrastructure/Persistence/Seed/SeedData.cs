using Sportner.Domain.Common.Constants;
using Sportner.Domain.Common.Enums;

namespace Sportner.Infrastructure.Persistence.Seed;

/// <summary>
/// Canonical reference data seeded on startup. Keyed by stable identifiers
/// (sport slug, badge code, report reason code) so re-runs update in place instead of duplicating.
/// </summary>
internal static class SeedData
{
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
        new("Badminton", "badminton", 15)
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
        new(BadgeCodes.MarathonRunner, "Maratoncu", "Uzun süreli bir aktivite serisini sürdürdün.",
            "badges/marathon-runner.png", BadgeCategory.Streak, BadgeRarity.Legendary, 500, 8)
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
}
