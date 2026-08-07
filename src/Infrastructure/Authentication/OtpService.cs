using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sportner.Application.Abstractions.Authentication;

namespace Sportner.Infrastructure.Authentication;

/// <summary>
/// Generates and verifies phone OTP codes. The code is stored only as a hash with a short TTL.
/// When <see cref="OtpOptions.ExposeCodeInLogs"/> is true, codes are logged and
/// optional <see cref="OtpOptions.FixedCode"/> is used (temporary until SMS provider).
/// </summary>
public sealed class OtpService : IOtpService
{
    private readonly IOtpChallengeStore _challengeStore;
    private readonly ITokenHasher _tokenHasher;
    private readonly ISmsSender _smsSender;
    private readonly OtpOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<OtpService> _logger;

    public OtpService(
        IOtpChallengeStore challengeStore,
        ITokenHasher tokenHasher,
        ISmsSender smsSender,
        IOptions<OtpOptions> options,
        TimeProvider timeProvider,
        ILogger<OtpService> logger)
    {
        _challengeStore = challengeStore;
        _tokenHasher = tokenHasher;
        _smsSender = smsSender;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task RequestAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);

        var code = ResolveCode();
        var hash = _tokenHasher.Hash(code);
        var expiresAt = _timeProvider.GetUtcNow().AddMinutes(_options.ExpirationMinutes);

        await _challengeStore.SetAsync(phoneNumber, hash, expiresAt, cancellationToken);

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

    public async Task<bool> VerifyAsync(
        string phoneNumber,
        string code,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var storedHash = await _challengeStore.GetHashAsync(phoneNumber, cancellationToken);

        if (storedHash is null)
        {
            return false;
        }

        var isValid = _tokenHasher.Verify(code, storedHash);

        if (isValid)
        {
            await _challengeStore.RemoveAsync(phoneNumber, cancellationToken);
        }

        return isValid;
    }

    private string ResolveCode()
    {
        if (_options.ExposeCodeInLogs && !string.IsNullOrWhiteSpace(_options.FixedCode))
        {
            var fixedCode = _options.FixedCode.Trim();

            if (fixedCode.Length != _options.CodeLength || !fixedCode.All(char.IsDigit))
            {
                throw new InvalidOperationException(
                    $"Otp:FixedCode must be {_options.CodeLength} digits when ExposeCodeInLogs is enabled.");
            }

            return fixedCode;
        }

        return GenerateCode(_options.CodeLength);
    }

    private static string GenerateCode(int length)
    {
        var max = (int)Math.Pow(10, length);
        var value = RandomNumberGenerator.GetInt32(0, max);
        return value.ToString().PadLeft(length, '0');
    }
}

