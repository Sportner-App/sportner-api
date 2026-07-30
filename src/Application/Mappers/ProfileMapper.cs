using Sportner.Application.DTOs.Profiles;
using Sportner.Application.Helpers;
using Sportner.Domain.Entities;

namespace Sportner.Application.Mappers;

public static class ProfileMapper
{
    public static UserProfileDto ToDto(this Profile profile, bool includePushToken = true) => new(
        profile.Id,
        profile.Email ?? string.Empty,
        profile.FullName,
        profile.AvatarUrl,
        profile.Bio,
        profile.Sports,
        profile.IntroVideoUrl,
        profile.IsOnboarded,
        profile.BirthDate,
        SkillLevelHelper.ParseJsonbString(profile.SkillLevels),
        profile.AvgRating,
        profile.ReviewCount,
        includePushToken ? profile.PushToken : null,
        profile.UpdatedAt
    );
}
