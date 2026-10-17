namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

internal sealed class ImageMutationLockTestTimeProvider : TimeProvider
{
    private long utcTicks;

    public ImageMutationLockTestTimeProvider(DateTimeOffset initialUtc)
    {
        this.utcTicks = initialUtc.UtcTicks;
    }

    public override DateTimeOffset GetUtcNow()
    {
        return new DateTimeOffset(Interlocked.Read(ref this.utcTicks), TimeSpan.Zero);
    }

    public void Advance(TimeSpan elapsed)
    {
        Interlocked.Add(ref this.utcTicks, elapsed.Ticks);
    }
}
