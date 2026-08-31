using AmusementPark.Application.Features.BackgroundJobs.Models;

namespace AmusementPark.Application.Features.BackgroundJobs.Services;

public sealed class DurableBackgroundJobRetryDelayCalculator
{
    private const double MinimumJitterMultiplier = 0.8;
    private const double JitterMultiplierRange = 0.4;
    private readonly Func<double> nextJitterValue;

    public DurableBackgroundJobRetryDelayCalculator()
        : this(static () => Random.Shared.NextDouble())
    {
    }

    internal DurableBackgroundJobRetryDelayCalculator(Func<double> nextJitterValue)
    {
        ArgumentNullException.ThrowIfNull(nextJitterValue);
        this.nextJitterValue = nextJitterValue;
    }

    public TimeSpan Calculate(DurableBackgroundJobHandlerDefinition definition, int attemptCount)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (attemptCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(attemptCount));
        }

        int exponent = Math.Min(attemptCount - 1, 30);
        double exponentialMilliseconds = definition.InitialRetryDelay.TotalMilliseconds * Math.Pow(2, exponent);
        double boundedMilliseconds = Math.Min(
            exponentialMilliseconds,
            definition.MaximumRetryDelay.TotalMilliseconds);
        double normalizedJitter = Math.Clamp(this.nextJitterValue(), 0, 1);
        double jitteredMilliseconds = boundedMilliseconds *
            (MinimumJitterMultiplier + (normalizedJitter * JitterMultiplierRange));
        return TimeSpan.FromMilliseconds(Math.Min(
            jitteredMilliseconds,
            definition.MaximumRetryDelay.TotalMilliseconds));
    }
}
