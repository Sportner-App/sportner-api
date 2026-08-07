using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sportner.Application.Abstractions.Authentication;

namespace Sportner.Infrastructure.Authentication;

/// <summary>
/// Generates and verifies phone OTP codes. The code is stored only as a hash with a short TTL
/// in the in-memory cache (single-instance / development). A distributed cache should replace
/// <see cref="IMemoryCache"/> before horizontal scaling. The OTP code is never logged unless
/// <see cref="OtpOptions.ExposeCodeInLogs"/> is explicitly enabled for local development.
/// </summary>
public sealed class OtpService : IOtpService
{
    private const string CacheKeyPrefix = "otp:";

    private readonly IMemoryCache _cache;
    private readonly ITokenHasher _tokenHasher;
    private readonly ISmsSender _smsSender;
    private readonly OtpOptions _options;
    private readonly ILogger<OtpService> _logger;

    public OtpService(
        IMemoryCache cache,
        ITokenHasher tokenHasher,
        ISmsSender smsSender,
        IOptions<OtpOptions> options,
        ILogger<OtpService> logger)
    {
        _cache = cache;
        _tokenHasher = tokenHasher;
        _smsSender = smsSender;
        _options = options.Value;
        _logger = logger;
    }

    public async Task RequestAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);

        var code = GenerateCode(_options.CodeLength);
        var hash = _tokenHasher.Hash(code);

        _cache.Set(
            BuildKey(phoneNumber),
            hash,
            TimeSpan.FromMinutes(_options.ExpirationMinutes));

        if (_options.ExposeCodeInLogs)
        {
            _logger.LogWarning(
                "Development OTP for {PhoneNumber}: {Code}",
                phoneNumber,
                code);
        }

        await _smsSender.SendAsync(
            phoneNumber,
            $"Sportner verification code: {code}",
            cancellationToken);
    }

    public Task<bool> VerifyAsync(
        string phoneNumber,
        string code,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;

        if (string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(code))
        {
            return Task.FromResult(false);
        }

        var key = BuildKey(phoneNumber);

        if (!_cache.TryGetValue(key, out string? storedHash) || storedHash is null)
        {
            return Task.FromResult(false);
        }

        var isValid = _tokenHasher.Verify(code, storedHash);

        if (isValid)
        {
            _cache.Remove(key);
        }

        return Task.FromResult(isValid);
    }

    private static string BuildKey(string phoneNumber) => CacheKeyPrefix + phoneNumber.Trim();

    private static string GenerateCode(int length)
    {
        var max = (int)Math.Pow(10, length);
        var value = RandomNumberGenerator.GetInt32(0, max);
        return value.ToString().PadLeft(length, '0');
    }
}
