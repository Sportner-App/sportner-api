using Sportner.Application.DTOs.Sports;
using Sportner.Domain.Entities;

namespace Sportner.Application.Mappers;

public static class SportMapper
{
    public static SportDto ToDto(this Sport sport) => new(
        sport.Id,
        sport.Name,
        sport.IconName,
        sport.Category
    );
}
