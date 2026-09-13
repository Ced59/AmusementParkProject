using System.Collections.Concurrent;

namespace AmusementPark.WebAPI.RateLimiting;

public readonly record struct AvatarUploadAttemptLease(bool IsAcquired, TimeSpan RetryAfter);
