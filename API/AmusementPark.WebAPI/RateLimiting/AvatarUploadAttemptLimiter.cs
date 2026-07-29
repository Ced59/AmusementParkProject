using System.Collections.Concurrent;

namespace AmusementPark.WebAPI.RateLimiting;

public readonly record struct AvatarUploadAttemptLease(bool IsAcquired, TimeSpan RetryAfter);

public interface IAvatarUploadAttemptLimiter
{
    AvatarUploadAttemptLease TryAcquire(string userId, DateTime nowUtc);
}

/// <summary>
/// Limite en mémoire les tentatives d'upload d'avatar par compte authentifié.
/// </summary>
public sealed class AvatarUploadAttemptLimiter : IAvatarUploadAttemptLimiter
{
    internal const int PermitLimit = 3;
    internal static readonly TimeSpan Window = TimeSpan.FromMinutes(15);
    private readonly ConcurrentDictionary<string, Queue<DateTime>> attemptsByUser =
        new ConcurrentDictionary<string, Queue<DateTime>>(StringComparer.Ordinal);

    public AvatarUploadAttemptLease TryAcquire(string userId, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        DateTime normalizedNowUtc = nowUtc.Kind == DateTimeKind.Utc
            ? nowUtc
            : nowUtc.ToUniversalTime();
        Queue<DateTime> attempts = this.attemptsByUser.GetOrAdd(
            userId.Trim(),
            static _ => new Queue<DateTime>());

        lock (attempts)
        {
            DateTime oldestAllowedUtc = normalizedNowUtc.Subtract(Window);
            while (attempts.TryPeek(out DateTime oldestAttemptUtc)
                && oldestAttemptUtc <= oldestAllowedUtc)
            {
                attempts.Dequeue();
            }

            if (attempts.Count >= PermitLimit)
            {
                DateTime retryAtUtc = attempts.Peek().Add(Window);
                TimeSpan retryAfter = retryAtUtc > normalizedNowUtc
                    ? retryAtUtc.Subtract(normalizedNowUtc)
                    : TimeSpan.Zero;
                return new AvatarUploadAttemptLease(false, retryAfter);
            }

            attempts.Enqueue(normalizedNowUtc);
            return new AvatarUploadAttemptLease(true, TimeSpan.Zero);
        }
    }
}
