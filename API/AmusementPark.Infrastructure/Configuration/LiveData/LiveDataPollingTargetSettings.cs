using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Infrastructure.Configuration.LiveData;

public sealed class LiveDataPollingTargetSettings
{
    public bool Enabled { get; set; }

    public string SourceId { get; set; } = "themeparks-wiki";

    public string ExternalEntityId { get; set; } = string.Empty;

    public string TimeZoneId { get; set; } = "Europe/Paris";

    public int ActiveFromHourLocal { get; set; } = 6;

    public int ActiveUntilHourLocal { get; set; } = 23;

    public int PollingIntervalSeconds { get; set; } = 300;

    public int MaximumJitterSeconds { get; set; } = 30;

    public int InitialFailureBackoffSeconds { get; set; } = 300;

    public int MaximumFailureBackoffSeconds { get; set; } = 3600;

    public int CircuitBreakerFailureThreshold { get; set; } = 5;

    public int CircuitBreakDurationSeconds { get; set; } = 1800;

    internal LivePollingTarget BuildTarget()
    {
        if (string.IsNullOrWhiteSpace(this.TimeZoneId))
        {
            throw new InvalidOperationException("TimeZoneId is required for live polling.");
        }

        if (this.ActiveFromHourLocal is < 5 or > 12)
        {
            throw new InvalidOperationException(
                "ActiveFromHourLocal must be between 5 and 12 to prevent night polling.");
        }

        if (this.ActiveUntilHourLocal is < 17 or > 23)
        {
            throw new InvalidOperationException(
                "ActiveUntilHourLocal must be between 17 and 23 to prevent night polling.");
        }

        if (this.ActiveFromHourLocal >= this.ActiveUntilHourLocal)
        {
            throw new InvalidOperationException(
                "The live polling active window must end after it starts.");
        }

        if (this.MaximumJitterSeconds is < 0 or > 60)
        {
            throw new InvalidOperationException("MaximumJitterSeconds must be between 0 and 60.");
        }

        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(this.TimeZoneId.Trim());
        LivePollingPolicy policy = new LivePollingPolicy(
            TimeSpan.FromSeconds(this.PollingIntervalSeconds),
            TimeSpan.FromSeconds(this.InitialFailureBackoffSeconds),
            TimeSpan.FromSeconds(this.MaximumFailureBackoffSeconds),
            this.CircuitBreakerFailureThreshold,
            TimeSpan.FromSeconds(this.CircuitBreakDurationSeconds));
        return new LivePollingTarget(
            LiveDataSourceId.Parse(this.SourceId),
            this.ExternalEntityId,
            new LivePollingActiveWindow(
                timeZone,
                this.ActiveFromHourLocal,
                this.ActiveUntilHourLocal),
            policy,
            TimeSpan.FromSeconds(this.MaximumJitterSeconds));
    }
}
