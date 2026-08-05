using AmusementPark.Core.Domain.Parks;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Parks;

public sealed class ParkPublicDiscoveryTests
{
    [Fact]
    public void IsPubliclyDiscoverable_WhenParkIsVisibleAndRelevant_ShouldReturnTrue()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Adventure Park",
            IsVisible = true,
            Status = ParkStatus.Operating,
            AdminReviewStatus = AdminReviewStatus.ToReview,
        };

        Assert.True(park.IsPubliclyDiscoverable());
    }

    [Theory]
    [InlineData(false, AdminReviewStatus.ToReview)]
    [InlineData(true, AdminReviewStatus.NotRelevant)]
    public void IsPubliclyDiscoverable_WhenParkIsHiddenOrNotRelevant_ShouldReturnFalse(
        bool isVisible,
        AdminReviewStatus reviewStatus)
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Adventure Park",
            IsVisible = isVisible,
            Status = ParkStatus.Operating,
            AdminReviewStatus = reviewStatus,
        };

        Assert.False(park.IsPubliclyDiscoverable());
    }
}
