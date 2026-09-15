using AmusementPark.Core.Domain.Watchlists;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Watchlists;

public sealed class UserCollectionEntryIdTests
{
    [Fact]
    public void Parse_ShouldNormalizeOpaqueIdentifier()
    {
        UserCollectionEntryId entryId = UserCollectionEntryId.Parse(" entry-1 ");

        Assert.Equal("entry-1", entryId.Value);
    }

    [Fact]
    public void TryParse_WithoutValue_ShouldReturnFalse()
    {
        bool parsed = UserCollectionEntryId.TryParse(" ", out UserCollectionEntryId entryId);

        Assert.False(parsed);
        Assert.Equal(default, entryId);
    }
}
