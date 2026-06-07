using AmusementPark.Application.Features.ParkItems.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkItems.Services;

public sealed class ParkItemAdministrationDefaultsTests
{
    [Fact]
    public void Constants_WhenUsedForQuickCreate_ShouldRepresentSafeEditorialDefaults()
    {
        Assert.False(ParkItemAdministrationDefaults.QuickCreateIsVisible);
        Assert.Equal(AdminReviewStatus.ToReview, ParkItemAdministrationDefaults.QuickCreateAdminReviewStatus);
        Assert.Equal(ParkItemCategory.Attraction, ParkItemAdministrationDefaults.QuickCreateCategory);
        Assert.Equal(ParkItemType.Attraction, ParkItemAdministrationDefaults.QuickCreateType);
    }

    [Fact]
    public void ApplyQuickCreateDefaults_WhenCalled_ShouldKeepDescriptionsReadyForWrite()
    {
        ParkItem parkItem = new ParkItem
        {
            ParkId = "park-1",
            Name = "New item",
            Category = ParkItemCategory.Attraction,
            Type = ParkItemType.Attraction,
            Descriptions = null!,
        };

        ParkItemAdministrationDefaults.ApplyQuickCreateDefaults(parkItem);

        Assert.NotNull(parkItem.Descriptions);
        Assert.Empty(parkItem.Descriptions);
    }

    [Fact]
    public void ApplyQuickCreateDefaults_WhenTypeDoesNotMatchCategory_ShouldUseCategoryDefaultType()
    {
        ParkItem parkItem = new ParkItem
        {
            Category = ParkItemCategory.Restaurant,
            Type = ParkItemType.RollerCoaster,
        };

        ParkItemAdministrationDefaults.ApplyQuickCreateDefaults(parkItem);

        Assert.Equal(ParkItemType.Restaurant, parkItem.Type);
    }

    [Fact]
    public void ApplyQuickCreateDefaults_WhenPositionIsMissing_ShouldUseFallbackPosition()
    {
        ParkItem parkItem = new ParkItem();
        GeoPoint fallbackPosition = new GeoPoint(50.6333, 3.0667);

        ParkItemAdministrationDefaults.ApplyQuickCreateDefaults(parkItem, fallbackPosition);

        Assert.NotNull(parkItem.Position);
        Assert.Equal(50.6333, parkItem.Position.Latitude);
        Assert.Equal(3.0667, parkItem.Position.Longitude);
    }
}
