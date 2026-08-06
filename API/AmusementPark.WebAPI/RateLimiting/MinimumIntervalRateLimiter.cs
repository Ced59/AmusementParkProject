using System.Threading.RateLimiting;

namespace AmusementPark.WebAPI.RateLimiting;

internal sealed class MinimumIntervalRateLimiter : RateLimiter
{
    private static readonly RateLimitLease SuccessfulLease = new MinimumIntervalRateLimitLease(true, null);
    private readonly object synchronizationLock = new object();
    private readonly TimeSpan minimumInterval;
    private readonly TimeProvider timeProvider;
    private long? lastAcquisitionTimestamp;
    private long successfulLeaseCount;
    private long failedLeaseCount;
    private bool isDisposed;

    public MinimumIntervalRateLimiter(TimeSpan minimumInterval)
        : this(minimumInterval, TimeProvider.System)
    {
    }

    internal MinimumIntervalRateLimiter(TimeSpan minimumInterval, TimeProvider timeProvider)
    {
        if (minimumInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumInterval));
        }

        this.minimumInterval = minimumInterval;
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public override TimeSpan? IdleDuration
    {
        get
        {
            lock (this.synchronizationLock)
            {
                return this.lastAcquisitionTimestamp.HasValue
                    ? this.timeProvider.GetElapsedTime(this.lastAcquisitionTimestamp.Value)
                    : null;
            }
        }
    }

    public override RateLimiterStatistics GetStatistics()
    {
        lock (this.synchronizationLock)
        {
            return new RateLimiterStatistics
            {
                CurrentAvailablePermits = this.CanAcquire(this.timeProvider.GetTimestamp()) ? 1 : 0,
                CurrentQueuedCount = 0,
                TotalFailedLeases = this.failedLeaseCount,
                TotalSuccessfulLeases = this.successfulLeaseCount,
            };
        }
    }

    protected override RateLimitLease AttemptAcquireCore(int permitCount)
    {
        lock (this.synchronizationLock)
        {
            if (this.isDisposed || permitCount > 1)
            {
                this.failedLeaseCount++;
                return new MinimumIntervalRateLimitLease(false, null);
            }

            if (permitCount == 0)
            {
                this.successfulLeaseCount++;
                return SuccessfulLease;
            }

            long currentTimestamp = this.timeProvider.GetTimestamp();
            if (this.CanAcquire(currentTimestamp))
            {
                this.lastAcquisitionTimestamp = currentTimestamp;
                this.successfulLeaseCount++;
                return SuccessfulLease;
            }

            this.failedLeaseCount++;
            TimeSpan retryAfter = this.minimumInterval
                - this.timeProvider.GetElapsedTime(this.lastAcquisitionTimestamp!.Value, currentTimestamp);
            return new MinimumIntervalRateLimitLease(false, retryAfter);
        }
    }

    protected override ValueTask<RateLimitLease> AcquireAsyncCore(
        int permitCount,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled<RateLimitLease>(cancellationToken);
        }

        return ValueTask.FromResult(this.AttemptAcquireCore(permitCount));
    }

    protected override void Dispose(bool disposing)
    {
        lock (this.synchronizationLock)
        {
            this.isDisposed = true;
        }
    }

    private bool CanAcquire(long currentTimestamp)
    {
        return !this.isDisposed
            && (!this.lastAcquisitionTimestamp.HasValue
                || this.timeProvider.GetElapsedTime(
                    this.lastAcquisitionTimestamp.Value,
                    currentTimestamp) >= this.minimumInterval);
    }

    private sealed class MinimumIntervalRateLimitLease : RateLimitLease
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
}
