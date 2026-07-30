using System.ComponentModel.DataAnnotations.Schema;

namespace SportnerApi.Models;

[Table("sports")]
public class Sport
{
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("icon_name")]
    public string? IconName { get; set; }

    [Column("category")]
    public string? Category { get; set; }
}
