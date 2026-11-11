namespace Sportner.Application.Common.Media;

/// <summary>
/// Depoya yazılan dosyanın uzantısı, kullanıcının gönderdiği dosya adından değil
/// doğrulanmış içerik türünden belirlenir. Aksi halde "resim" diye kabul edilen bir
/// dosya ".html" gibi bir uzantıyla herkese açık depoya yazılabilir.
/// </summary>
public static class MediaFileExtension
{
    private static readonly Dictionary<string, string> ByContentType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/jpg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
        ["image/heic"] = ".heic",
        ["image/heif"] = ".heif",
        ["video/mp4"] = ".mp4",
        ["video/quicktime"] = ".mov",
        ["video/webm"] = ".webm",
        ["application/pdf"] = ".pdf",
    };

    /// <summary>Bilinmeyen bir tür için uzantısız döner; tür zaten çağıran tarafta izin listesinden geçmiş olmalı.</summary>
    public static string FromContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return string.Empty;
        }

        var separator = contentType.IndexOf(';');
        var raw = separator < 0 ? contentType.Trim() : contentType[..separator].Trim();
        return ByContentType.GetValueOrDefault(raw, string.Empty);
    }
}
