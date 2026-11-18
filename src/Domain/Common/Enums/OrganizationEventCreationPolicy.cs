namespace Sportner.Domain.Common.Enums;

/// <summary>Kulüp adına kimlerin etkinlik oluşturabileceği.</summary>
public enum OrganizationEventCreationPolicy : short
{
    AllMembers = 0,
    ManagersOnly = 1
}
