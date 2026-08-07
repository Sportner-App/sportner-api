using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using Sportner.Application.Abstractions.Storage;

namespace Sportner.Infrastructure.Storage;

/// <summary>
/// <see cref="IFileStorage"/> backed by the Supabase Storage REST API using the service role key.
/// The database stores only object paths; this type maps those paths to and from Supabase objects.
/// </summary>
public sealed class SupabaseFileStorage : IFileStorage
{
    private readonly HttpClient _httpClient;
    private readonly SupabaseStorageOptions _options;

    public SupabaseFileStorage(HttpClient httpClient, IOptions<SupabaseStorageOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<string> UploadAsync(
        string bucket,
        string path,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bucket);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var normalizedPath = NormalizePath(path);
        var requestUri = $"{BaseUrl}/object/{bucket}/{normalizedPath}";

        using var request = new HttpRequestMessage(HttpMethod.Put, requestUri);
        request.Headers.Authorization = AuthHeader();
        request.Headers.TryAddWithoutValidation("x-upsert", "true");

        using var streamContent = new StreamContent(content);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        request.Content = streamContent;

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return normalizedPath;
    }

    public async Task DeleteAsync(
        string bucket,
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bucket);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var normalizedPath = NormalizePath(path);
        var requestUri = $"{BaseUrl}/object/{bucket}/{normalizedPath}";

        using var request = new HttpRequestMessage(HttpMethod.Delete, requestUri);
        request.Headers.Authorization = AuthHeader();

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public string GetPublicUrl(string bucket, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bucket);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return $"{BaseUrl}/object/public/{bucket}/{NormalizePath(path)}";
    }

    private string BaseUrl => $"{_options.Url.TrimEnd('/')}/storage/v1";

    private AuthenticationHeaderValue AuthHeader() =>
        new("Bearer", _options.ServiceRoleKey);

    private static string NormalizePath(string path) =>
        path.Trim().TrimStart('/');
}
