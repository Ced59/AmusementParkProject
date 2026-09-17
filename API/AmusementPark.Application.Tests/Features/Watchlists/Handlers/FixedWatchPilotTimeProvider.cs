namespace AmusementPark.Application.Tests.Features.Watchlists.Handlers;

internal sealed class FixedWatchPilotTimeProvider : TimeProvider
{
    private readonly DateTimeOffset utcNow;

    public FixedWatchPilotTimeProvider(DateTimeOffset utcNow)
    {
        this.utcNow = utcNow;
    }

    public override DateTimeOffset GetUtcNow()
    {
        return this.utcNow;
    }
}
