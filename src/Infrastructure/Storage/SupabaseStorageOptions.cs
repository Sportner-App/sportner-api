namespace Sportner.Infrastructure.Storage;

/// <summary>
/// Bound from the <c>Supabase</c> configuration section. The service role key is server-only
/// and must never be exposed to clients.
/// </summary>
public sealed class SupabaseStorageOptions
{
    public const string SectionName = "Supabase";

    public string Url { get; set; } = string.Empty;

    public string ServiceRoleKey { get; set; } = string.Empty;
}
