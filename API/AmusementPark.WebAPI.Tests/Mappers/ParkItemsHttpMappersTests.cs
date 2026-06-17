using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.ParkItems;
using AmusementPark.WebAPI.Mappers;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Mappers;

public sealed class ParkItemsHttpMappersTests
{
    [Fact]
    public void ToDomain_WhenQuickCreateDtoIsMinimal_ShouldApplyFastEditionDefaults()
    {
        ParkItemQuickCreateDto dto = new ParkItemQuickCreateDto
        {
            ParkId = "park-1",
            Name = " Taron ",
        };

        ParkItem parkItem = dto.ToDomain(new GeoPoint(50.801, 6.879));

        Assert.Equal("park-1", parkItem.ParkId);
        Assert.Equal(" Taron ", parkItem.Name);
        Assert.Equal(ParkItemCategory.Attraction, parkItem.Category);
        Assert.Equal(ParkItemType.Attraction, parkItem.Type);
        Assert.False(parkItem.IsVisible);
        Assert.Equal(AdminReviewStatus.ToReview, parkItem.AdminReviewStatus);
        Assert.Empty(parkItem.Descriptions);
        Assert.NotNull(parkItem.Position);
        Assert.Equal(50.801, parkItem.Position.Latitude);
        Assert.Equal(6.879, parkItem.Position.Longitude);
    }

    [Fact]
    public void ToDomain_WhenQuickCreateTypeDoesNotMatchCategory_ShouldNormalizeType()
    {
        ParkItemQuickCreateDto dto = new ParkItemQuickCreateDto
        {
            ParkId = "park-1",
            Name = "Burger",
            Category = ParkItemCategoryDto.Restaurant,
            Type = ParkItemTypeDto.RollerCoaster,
        };

        ParkItem parkItem = dto.ToDomain();

        Assert.Equal(ParkItemCategory.Restaurant, parkItem.Category);
        Assert.Equal(ParkItemType.Restaurant, parkItem.Type);
        Assert.Null(parkItem.AttractionDetails);
    }

    [Fact]
    public void ToDomain_WhenQuickCreateAttractionUsesCinemaType_ShouldKeepCinemaType()
    {
        ParkItemQuickCreateDto dto = new ParkItemQuickCreateDto
        {
            ParkId = "park-1",
            Name = "Cinema 4D",
            Category = ParkItemCategoryDto.Attraction,
            Type = ParkItemTypeDto.Cinema,
        };

        ParkItem parkItem = dto.ToDomain();

        Assert.Equal(ParkItemCategory.Attraction, parkItem.Category);
        Assert.Equal(ParkItemType.Cinema, parkItem.Type);
    }

    [Fact]
    public void ToDomain_WhenQuickCreateAttractionHasManufacturer_ShouldMapLightAttractionDetails()
    {
        ParkItemQuickCreateDto dto = new ParkItemQuickCreateDto
        {
            ParkId = "park-1",
            Name = "Coaster",
            ManufacturerId = " intamin ",
        };

        ParkItem parkItem = dto.ToDomain();

        Assert.NotNull(parkItem.AttractionDetails);
        Assert.Equal("intamin", parkItem.AttractionDetails.ManufacturerId);
    }
}
