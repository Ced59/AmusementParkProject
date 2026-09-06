namespace AmusementPark.Application.Tests.Features.Sharing.Handlers;

internal sealed class SharePublicationFixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset now;

    public SharePublicationFixedTimeProvider(DateTimeOffset now)
    {
        this.now = now;
    }

    public override DateTimeOffset GetUtcNow()
    {
        return this.now;
    }
}
