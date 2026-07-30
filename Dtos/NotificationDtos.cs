using System.ComponentModel.DataAnnotations;

namespace SportnerApi.Dtos;

public record UpdatePushTokenDto(
    [Required] string PushToken
);
