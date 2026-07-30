namespace Sportner.Application.DTOs.Sports;

public record SportDto(
    string Id,
    string Name,
    string? IconName,
    string? Category
);
