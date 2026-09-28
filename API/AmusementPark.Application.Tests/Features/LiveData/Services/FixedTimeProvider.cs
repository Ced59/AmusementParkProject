namespace AmusementPark.Application.Tests.Features.LiveData.Services;

internal sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset utcNow;

    public FixedTimeProvider(DateTime utcNow)
    {
        this.utcNow = new DateTimeOffset(utcNow);
    }

    public override DateTimeOffset GetUtcNow()
    {
        return this.utcNow;
    }
}
