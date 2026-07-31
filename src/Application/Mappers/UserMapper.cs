using Sportner.Application.DTOs.Users;
using Sportner.Application.Helpers;
using Sportner.Domain.Entities;

namespace Sportner.Application.Mappers;

public static class UserMapper
{
    public static UserDto ToDto(this User user, bool includePushToken = true) => new(
        user.Id,
        user.Email ?? string.Empty,
        user.FullName,
        user.AvatarUrl,
        user.Bio,
        user.Sports,
        user.IntroVideoUrl,
        user.IsOnboarded,
        user.BirthDate,
        SkillLevelHelper.ParseJsonbString(user.SkillLevels),
        user.AvgRating,
        user.ReviewCount,
        includePushToken ? user.PushToken : null,
        user.UpdatedAt
    );
}
