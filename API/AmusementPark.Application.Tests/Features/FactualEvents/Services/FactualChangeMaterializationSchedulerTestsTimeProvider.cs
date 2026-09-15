namespace AmusementPark.Application.Tests.Features.FactualEvents.Services;

internal sealed class FactualChangeMaterializationSchedulerTestsTimeProvider : TimeProvider
{
    private readonly DateTimeOffset utcNow;

    public FactualChangeMaterializationSchedulerTestsTimeProvider(DateTimeOffset utcNow)
    {
        this.utcNow = utcNow;
    }

    public override DateTimeOffset GetUtcNow()
    {
        return this.utcNow;
    }
}
