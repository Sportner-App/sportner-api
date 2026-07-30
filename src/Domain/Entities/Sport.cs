namespace Sportner.Domain.Entities;

public class Sport
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? IconName { get; set; }
    public string? Category { get; set; }
}
