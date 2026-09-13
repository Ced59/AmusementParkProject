using System.Collections.Concurrent;

namespace AmusementPark.WebAPI.RateLimiting;

public interface IAvatarUploadAttemptLimiter
{
    AvatarUploadAttemptLease TryAcquire(string userId, DateTime nowUtc);
}
