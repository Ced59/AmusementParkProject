namespace AmusementPark.Application.Tests.Features.History.Handlers;

internal sealed class FixedHistoryEditorialTimeProvider : TimeProvider
{
    private readonly DateTimeOffset now;

    public FixedHistoryEditorialTimeProvider(DateTimeOffset now)
    {
        this.now = now;
    }

    public override DateTimeOffset GetUtcNow()
    {
        return this.now;
    }
}
