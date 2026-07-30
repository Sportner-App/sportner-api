using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sportner.Application.Abstractions;
using Sportner.Infrastructure.Options;
using Sportner.Localization.Resources;

namespace Sportner.Infrastructure.Services;

public class SupabaseStorageService(
    HttpClient httpClient,
    IOptions<SupabaseSettings> options,
    ILogger<SupabaseStorageService> logger) : IStorageService
{
    private readonly SupabaseSettings _settings = options.Value;

    public async Task<string> UploadAvatarAsync(
        Guid userId,
        Stream fileStream,
        string contentType,
        string fileExtension,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.Url) || string.IsNullOrWhiteSpace(_settings.ServiceRoleKey))
        {
            throw new InvalidOperationException("SupabaseSettings:Url and ServiceRoleKey must be configured.");
        }

        var bucket = string.IsNullOrWhiteSpace(_settings.AvatarsBucket)
            ? "avatars"
            : _settings.AvatarsBucket;

        var extension = fileExtension.TrimStart('.').ToLowerInvariant();
        var objectPath = $"{userId}/avatar.{extension}";
        var baseUrl = _settings.Url.TrimEnd('/');
        var uploadUrl = $"{baseUrl}/storage/v1/object/{bucket}/{objectPath}";

        using var content = new StreamContent(fileStream);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

        using var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl)
        {
            Content = content
        };

        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_settings.ServiceRoleKey}");
        request.Headers.TryAddWithoutValidation("apikey", _settings.ServiceRoleKey);
        request.Headers.TryAddWithoutValidation("x-upsert", "true");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError(
                "Supabase avatar upload failed ({StatusCode}): {ErrorBody}",
                (int)response.StatusCode,
                errorBody);
            throw new InvalidOperationException(ValidationResource.Exception_Profile_AvatarUploadFailed);
        }

        // Cache-bust so clients refresh the image after overwrite.
        var publicUrl = $"{baseUrl}/storage/v1/object/public/{bucket}/{objectPath}?t={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        return publicUrl;
    }
}
