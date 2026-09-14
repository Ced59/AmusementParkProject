namespace AmusementPark.Application.Tests.Features.ParkFit.Handlers;

internal sealed class ChangeParkFitOperationalStatusCommandHandlerTestsFixedTimeProvider
    : TimeProvider
{
    private readonly DateTimeOffset now;

    public ChangeParkFitOperationalStatusCommandHandlerTestsFixedTimeProvider(DateTime nowUtc)
    {
        this.now = new DateTimeOffset(nowUtc);
    }

    public override DateTimeOffset GetUtcNow()
    {
        return this.now;
    }
}
