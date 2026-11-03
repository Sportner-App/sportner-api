namespace Sportner.Domain.Common.Enums;

/// <summary>
/// Challenge ilerlemesinin neye göre sayıldığı. Hepsi yoklamada "geldi" işaretlenen
/// ya da organizatörü olunan tamamlanmış etkinliklerden türetilir.
/// </summary>
public enum ChallengeMetric : short
{
    EventsPlayed = 0,
    DistinctSports = 1,
    EventsOrganized = 2,
    SportEventsPlayed = 3
}
