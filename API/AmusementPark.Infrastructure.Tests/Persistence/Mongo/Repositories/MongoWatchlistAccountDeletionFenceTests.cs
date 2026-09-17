using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class MongoWatchlistAccountDeletionFenceTests
{
    [Fact]
    public void BuildDeletionKey_ShouldBeStableAndNotExposeTheUserIdentifier()
    {
        string first = MongoWatchlistAccountDeletionFence.BuildDeletionKey("user-1");
        string second = MongoWatchlistAccountDeletionFence.BuildDeletionKey("user-1");

        Assert.Equal(first, second);
        Assert.Equal(64, first.Length);
        Assert.DoesNotContain("user-1", first, StringComparison.Ordinal);
    }
}
