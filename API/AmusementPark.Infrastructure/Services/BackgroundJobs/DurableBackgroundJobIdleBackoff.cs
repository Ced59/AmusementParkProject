namespace AmusementPark.Infrastructure.Services.BackgroundJobs;

internal sealed class DurableBackgroundJobIdleBackoff
{
    private readonly TimeSpan initialDelay;
    private readonly TimeSpan maximumDelay;
    private readonly double multiplier;
    private TimeSpan nextDelay;

    public DurableBackgroundJobIdleBackoff(TimeSpan initialDelay, TimeSpan maximumDelay, double multiplier)
    {
        if (initialDelay <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(initialDelay));
        }

        if (maximumDelay < initialDelay)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumDelay));
        }

        if (multiplier < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(multiplier));
        }

        this.initialDelay = initialDelay;
        this.maximumDelay = maximumDelay;
        this.multiplier = multiplier;
        this.nextDelay = initialDelay;
    }

    public TimeSpan TakeNextDelay()
    {
        TimeSpan currentDelay = this.nextDelay;
        double multipliedMilliseconds = currentDelay.TotalMilliseconds * this.multiplier;
        this.nextDelay = TimeSpan.FromMilliseconds(Math.Min(
            multipliedMilliseconds,
            this.maximumDelay.TotalMilliseconds));
        return currentDelay;
    }

    public void Reset()
    {
        this.nextDelay = this.initialDelay;
    }
}
