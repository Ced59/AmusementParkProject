namespace AmusementPark.Application.Tests.Features.Ratings.Handlers;

internal sealed class UserRankingShareFixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset now;

    public UserRankingShareFixedTimeProvider(DateTimeOffset now)
    {
        this.now = now;
    }

    public override DateTimeOffset GetUtcNow()
    {
        return this.now;
    }
}
