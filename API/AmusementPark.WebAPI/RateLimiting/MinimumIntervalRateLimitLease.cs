using System.Threading.RateLimiting;

namespace AmusementPark.WebAPI.RateLimiting;

internal sealed class MinimumIntervalRateLimitLease : RateLimitLease
{
    private readonly TimeSpan? retryAfter;

    public MinimumIntervalRateLimitLease(bool isAcquired, TimeSpan? retryAfter)
    {
        this.IsAcquired = isAcquired;
        this.retryAfter = retryAfter;
    }

    public override bool IsAcquired { get; }

    public override IEnumerable<string> MetadataNames => this.retryAfter.HasValue
        ? new[] { MetadataName.RetryAfter.Name }
        : Array.Empty<string>();

    public override bool TryGetMetadata(string metadataName, out object? metadata)
    {
        if (this.retryAfter.HasValue
            && string.Equals(metadataName, MetadataName.RetryAfter.Name, StringComparison.Ordinal))
        {
            metadata = this.retryAfter.Value;
            return true;
        }

        metadata = null;
        return false;
    }
}
