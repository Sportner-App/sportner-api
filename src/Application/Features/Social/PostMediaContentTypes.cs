using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Social;

internal static class PostMediaContentTypes
{
    private static readonly Dictionary<string, MediaType> ByContentType = new(
        StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = MediaType.Image,
        ["image/jpg"] = MediaType.Image,
        ["image/png"] = MediaType.Image,
        ["image/webp"] = MediaType.Image,
        ["video/mp4"] = MediaType.Video,
        ["video/quicktime"] = MediaType.Video,
        ["video/webm"] = MediaType.Video
    };

    private static readonly Dictionary<string, string> ContentTypeByExtension = new(
        StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp",
        [".mp4"] = "video/mp4",
        [".mov"] = "video/quicktime",
        [".webm"] = "video/webm"
    };

    public static bool TryResolve(
        string? contentType,
        string? fileName,
        out string normalizedContentType,
        out MediaType mediaType)
    {
        var raw = StripParameters(contentType);

        if (!string.IsNullOrWhiteSpace(raw)
            && !raw.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase)
            && ByContentType.TryGetValue(raw, out mediaType))
        {
            normalizedContentType = NormalizeAlias(raw);
            return true;
        }

        var extension = Path.GetExtension(fileName);
        if (!string.IsNullOrWhiteSpace(extension)
            && ContentTypeByExtension.TryGetValue(extension, out var inferred)
            && ByContentType.TryGetValue(inferred, out mediaType))
        {
            normalizedContentType = inferred;
            return true;
        }

        normalizedContentType = string.Empty;
        mediaType = default;
        return false;
    }

    public static string ResolveExtension(string? fileName, string contentType)
    {
        var extension = Path.GetExtension(fileName);
        if (!string.IsNullOrWhiteSpace(extension)
            && ContentTypeByExtension.ContainsKey(extension))
        {
            return extension.ToLowerInvariant();
        }

        return contentType.ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/webp" => ".webp",
            "video/mp4" => ".mp4",
            "video/quicktime" => ".mov",
            "video/webm" => ".webm",
            _ => ".jpg"
        };
    }

    private static string StripParameters(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return string.Empty;
        }

        var separator = contentType.IndexOf(';');
        return separator < 0
            ? contentType.Trim()
            : contentType[..separator].Trim();
    }

    private static string NormalizeAlias(string contentType)
    {
        return contentType.Equals("image/jpg", StringComparison.OrdinalIgnoreCase)
            ? "image/jpeg"
            : contentType;
    }
}
