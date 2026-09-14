namespace AmusementPark.Application.Tests.Features.ParkFit.Handlers;

internal sealed class FixedParkFitPilotTimeProvider : TimeProvider
{
    private readonly DateTimeOffset utcNow;

    public FixedParkFitPilotTimeProvider(DateTimeOffset utcNow)
    {
        this.utcNow = utcNow;
    }

    public override DateTimeOffset GetUtcNow()
    {
        return this.utcNow;
    }
}
