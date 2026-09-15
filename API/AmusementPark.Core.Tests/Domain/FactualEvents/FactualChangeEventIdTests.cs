using AmusementPark.Core.Domain.FactualEvents;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.FactualEvents;

public sealed class FactualChangeEventIdTests
{
    [Fact]
    public void Parse_ShouldNormalizeIdentifier()
    {
        FactualChangeEventId id = FactualChangeEventId.Parse(" event-1 ");

        Assert.Equal("event-1", id.Value);
        Assert.Equal("event-1", id.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryParse_WithMissingIdentifier_ShouldReturnFalse(string? value)
    {
        bool parsed = FactualChangeEventId.TryParse(value, out FactualChangeEventId id);

        Assert.False(parsed);
        Assert.Throws<InvalidOperationException>(() => id.ToString());
    }

    [Fact]
    public void New_ShouldCreateDistinctOpaqueIdentifiers()
    {
        FactualChangeEventId first = FactualChangeEventId.New();
        FactualChangeEventId second = FactualChangeEventId.New();

        Assert.NotEqual(first, second);
        Assert.Equal(32, first.Value.Length);
        Assert.Equal(32, second.Value.Length);
    }
}
