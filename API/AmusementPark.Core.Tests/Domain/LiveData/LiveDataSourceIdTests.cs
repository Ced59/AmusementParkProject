using AmusementPark.Core.Domain.LiveData;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.LiveData;

public sealed class LiveDataSourceIdTests
{
    [Fact]
    public void Parse_ShouldNormalizeOpaqueIdentifier()
    {
        LiveDataSourceId sourceId = LiveDataSourceId.Parse("  theming-source  ");

        Assert.Equal("theming-source", sourceId.Value);
        Assert.Equal("theming-source", sourceId.ToString());
    }

    [Fact]
    public void New_ShouldCreateDistinctInitializedIdentifiers()
    {
        LiveDataSourceId first = LiveDataSourceId.New();
        LiveDataSourceId second = LiveDataSourceId.New();

        Assert.NotEqual(first, second);
        Assert.NotEmpty(first.Value);
        Assert.NotEmpty(second.Value);
    }

    [Fact]
    public void TryParse_WithMissingIdentifier_ShouldReturnFalse()
    {
        bool parsed = LiveDataSourceId.TryParse("  ", out LiveDataSourceId sourceId);

        Assert.False(parsed);
        Assert.Throws<InvalidOperationException>(() => sourceId.Value);
    }
}
