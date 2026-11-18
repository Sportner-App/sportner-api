using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Events.CreateEvent;

public sealed record CreateEventCommand(
    Guid SportId,
    string Title,
    string? Description,
    DateTimeOffset EventDate,
    int? DurationMinutes,
    decimal Latitude,
    decimal Longitude,
    string Address,
    int? MaxParticipants,
    int? MinParticipantAge,
    int? MaxParticipantAge,
    short? SkillLevel = null,
    bool IsPaid = false,
    decimal? FeeAmount = null,
    Guid? OrganizationId = null,
    short? ParticipantGender = null,
    /// <summary>
    /// null: istemci alanı göndermedi. Kulübe bağlı etkinlikte varsayılan "sadece üyeler"dir;
    /// alanı bilmeyen eski uygulama sürümleri kulüp etkinliğini yanlışlıkla herkese açmasın.
    /// </summary>
    bool? IsOrganizationOnly = null,
    /// <summary>
    /// null: istemci alanı göndermedi → true (organizatör de oynar). false yalnızca kulüp etkinliğinde
    /// geçerlidir: etkinlik kulübündür, oluşturan kişi oynamadan yönetir.
    /// </summary>
    bool? OrganizerParticipates = null) : ICommand<EventResponse>;
