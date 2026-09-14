namespace AmusementPark.Application.Tests.Features.ParkFit.Handlers;

internal sealed class SearchParksByFitQueryHandlerTestsFixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset now;

    public SearchParksByFitQueryHandlerTestsFixedTimeProvider(DateTimeOffset now)
    {
        this.now = now;
    }

    public override DateTimeOffset GetUtcNow()
    {
        return this.now;
    }
}
