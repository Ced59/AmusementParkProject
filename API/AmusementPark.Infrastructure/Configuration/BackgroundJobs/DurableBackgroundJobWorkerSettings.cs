using Microsoft.Extensions.Configuration;

namespace AmusementPark.Infrastructure.Configuration.BackgroundJobs;

public sealed class DurableBackgroundJobWorkerSettings
{
    public const string SectionName = "DurableBackgroundJobs:Worker";

    public bool Enabled { get; set; } = true;

    public int HeavyWorkerCount { get; set; } = 1;

    public int LightWorkerCount { get; set; } = 1;

    public int LeaseDurationSeconds { get; set; } = 120;

    public int LeaseRenewalIntervalSeconds { get; set; } = 30;

    public int EmptyQueueInitialDelayMilliseconds { get; set; } = 250;

    public int EmptyQueueMaximumDelayMilliseconds { get; set; } = 5000;

    public double EmptyQueueDelayMultiplier { get; set; } = 2;

    public int LeaseRecoveryIntervalSeconds { get; set; } = 60;

    public int LeaseRecoveryBatchSize { get; set; } = 100;

    public int UnknownKindGracePeriodSeconds { get; set; } = 3600;

    public TimeSpan LeaseDuration => TimeSpan.FromSeconds(this.LeaseDurationSeconds);

    public TimeSpan LeaseRenewalInterval => TimeSpan.FromSeconds(this.LeaseRenewalIntervalSeconds);

    public TimeSpan EmptyQueueInitialDelay => TimeSpan.FromMilliseconds(this.EmptyQueueInitialDelayMilliseconds);

    public TimeSpan EmptyQueueMaximumDelay => TimeSpan.FromMilliseconds(this.EmptyQueueMaximumDelayMilliseconds);

    public TimeSpan LeaseRecoveryInterval => TimeSpan.FromSeconds(this.LeaseRecoveryIntervalSeconds);

    public TimeSpan UnknownKindGracePeriod => TimeSpan.FromSeconds(this.UnknownKindGracePeriodSeconds);

    public static DurableBackgroundJobWorkerSettings Bind(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        DurableBackgroundJobWorkerSettings settings =
            configuration.GetSection(SectionName).Get<DurableBackgroundJobWorkerSettings>()
            ?? new DurableBackgroundJobWorkerSettings();
        settings.Validate();
        return settings;
    }

    internal void Validate()
    {
        ValidateRange(this.HeavyWorkerCount, 0, 1, nameof(this.HeavyWorkerCount));
        ValidateRange(this.LightWorkerCount, 0, 2, nameof(this.LightWorkerCount));
        if (this.Enabled && this.HeavyWorkerCount + this.LightWorkerCount == 0)
        {
            throw new InvalidOperationException("At least one durable background job worker is required when the worker is enabled.");
        }

        ValidateRange(this.LeaseDurationSeconds, 30, 3600, nameof(this.LeaseDurationSeconds));
        ValidateRange(
            this.LeaseRenewalIntervalSeconds,
            5,
            this.LeaseDurationSeconds / 2,
            nameof(this.LeaseRenewalIntervalSeconds));
        ValidateRange(
            this.EmptyQueueInitialDelayMilliseconds,
            100,
            10_000,
            nameof(this.EmptyQueueInitialDelayMilliseconds));
        ValidateRange(
            this.EmptyQueueMaximumDelayMilliseconds,
            this.EmptyQueueInitialDelayMilliseconds,
            60_000,
            nameof(this.EmptyQueueMaximumDelayMilliseconds));
        if (this.EmptyQueueDelayMultiplier < 1 || this.EmptyQueueDelayMultiplier > 10)
        {
            throw new InvalidOperationException($"{nameof(this.EmptyQueueDelayMultiplier)} must be between 1 and 10.");
        }

        ValidateRange(
            this.LeaseRecoveryIntervalSeconds,
            10,
            3600,
            nameof(this.LeaseRecoveryIntervalSeconds));
        ValidateRange(this.LeaseRecoveryBatchSize, 1, 500, nameof(this.LeaseRecoveryBatchSize));
        ValidateRange(this.UnknownKindGracePeriodSeconds, 300, 86_400, nameof(this.UnknownKindGracePeriodSeconds));
    }

    private static void ValidateRange(int value, int minimum, int maximum, string propertyName)
    {
        if (value < minimum || value > maximum)
        {
            throw new InvalidOperationException($"{propertyName} must be between {minimum} and {maximum}.");
        }
    }
}
