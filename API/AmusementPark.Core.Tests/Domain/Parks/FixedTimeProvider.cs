using AmusementPark.Core.Domain.Parks;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Parks;

internal sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset utcNow;

    public FixedTimeProvider(DateTimeOffset utcNow)
    {
        this.utcNow = utcNow;
    }

    public override DateTimeOffset GetUtcNow()
    {
        return this.utcNow;
    }
}
