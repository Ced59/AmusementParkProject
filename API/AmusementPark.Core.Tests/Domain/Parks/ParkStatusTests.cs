using AmusementPark.Core.Domain.Parks;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Parks;

public sealed class ParkStatusTests
{
    [Fact]
    public void HistoricalNumericValues_ShouldRemainStable()
    {
        Assert.Equal(0, (int)ParkStatus.Operating);
        Assert.Equal(1, (int)ParkStatus.ClosedDefinitively);
    }

    [Theory]
    [InlineData(ParkStatus.Operating, true)]
    [InlineData(ParkStatus.Planned, false)]
    [InlineData(ParkStatus.UnderConstruction, false)]
    [InlineData(ParkStatus.TemporarilyClosed, false)]
    [InlineData(ParkStatus.ClosedDefinitively, false)]
    [InlineData(ParkStatus.Cancelled, false)]
    public void CanHaveCurrentOpeningHours_ShouldOnlyAllowOperating(ParkStatus status, bool expected)
    {
        Assert.Equal(expected, status.CanHaveCurrentOpeningHours());
        Assert.Equal(expected, status.IsOpenToVisitors());
    }

    [Theory]
    [InlineData(ParkStatus.Operating, true)]
    [InlineData(ParkStatus.Planned, true)]
    [InlineData(ParkStatus.UnderConstruction, true)]
    [InlineData(ParkStatus.TemporarilyClosed, true)]
    [InlineData(ParkStatus.ClosedDefinitively, true)]
    [InlineData(ParkStatus.Cancelled, true)]
    public void CanAppearInPublicDiscovery_ShouldPreserveDocumentedProjects(ParkStatus status, bool expected)
    {
        Assert.Equal(expected, status.CanAppearInPublicDiscovery());
    }
}
