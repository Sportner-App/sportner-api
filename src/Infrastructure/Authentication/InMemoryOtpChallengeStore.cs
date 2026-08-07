using System.Collections.Concurrent;
using Sportner.Application.Abstractions.Authentication;

namespace Sportner.Infrastructure.Authentication;

/// <summary>
/// Process-local OTP store. Suitable for single-instance / development.
/// Replace with a distributed store before horizontal scaling.
/// </summary>
public sealed class InMemoryOtpChallengeStore : IOtpChallengeStore
{
    private readonly ConcurrentDictionary<string, Challenge> _challenges = new(StringComparer.Ordinal);
    private readonly TimeProvider _timeProvider;

    public InMemoryOtpChallengeStore(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public Task SetAsync(
        string phoneNumber,
        string codeHash,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        _challenges[Normalize(phoneNumber)] = new Challenge(codeHash, expiresAt);
        return Task.CompletedTask;
    }

    public Task<string?> GetHashAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var key = Normalize(phoneNumber);

        if (!_challenges.TryGetValue(key, out var challenge))
        {
            return Task.FromResult<string?>(null);
        }

        if (challenge.ExpiresAt <= _timeProvider.GetUtcNow())
        {
            _challenges.TryRemove(key, out _);
            return Task.FromResult<string?>(null);
        }

        return Task.FromResult<string?>(challenge.CodeHash);
    }

    public Task RemoveAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        _challenges.TryRemove(Normalize(phoneNumber), out _);
        return Task.CompletedTask;
    }

    public Task<int> RemoveExpiredAsync(
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var removed = 0;

        foreach (var pair in _challenges)
        {
            if (pair.Value.ExpiresAt <= utcNow && _challenges.TryRemove(pair.Key, out _))
            {
                removed++;
            }
        }

        return Task.FromResult(removed);
    }

    private static string Normalize(string phoneNumber) => phoneNumber.Trim();

    private sealed record Challenge(string CodeHash, DateTimeOffset ExpiresAt);
}
