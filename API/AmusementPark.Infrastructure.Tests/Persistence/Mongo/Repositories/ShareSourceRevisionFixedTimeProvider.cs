namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class ShareSourceRevisionFixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset now;

    public ShareSourceRevisionFixedTimeProvider(DateTime nowUtc)
    {
        this.now = new DateTimeOffset(nowUtc);
    }

    public override DateTimeOffset GetUtcNow()
    {
        return this.now;
    }
}
