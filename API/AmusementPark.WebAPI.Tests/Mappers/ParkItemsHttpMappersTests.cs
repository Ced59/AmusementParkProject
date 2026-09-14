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
    public void ToDomain_WhenQuickCreateAttractionUsesDropTowerType_ShouldKeepDropTowerType()
    {
        ParkItemQuickCreateDto dto = new ParkItemQuickCreateDto
        {
            ParkId = "park-1",
            Name = "Drop tower",
            Category = ParkItemCategoryDto.Attraction,
            Type = ParkItemTypeDto.DropTower,
        };

        ParkItem parkItem = dto.ToDomain();

        Assert.Equal(ParkItemCategory.Attraction, parkItem.Category);
        Assert.Equal(ParkItemType.DropTower, parkItem.Type);
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

    [Fact]
    public void ToDomainToHttp_WhenAccessConditionHasProvenance_ShouldPreserveTheEvidenceContract()
    {
        DateTime collectedAtUtc = new DateTime(2026, 8, 1, 8, 30, 0, DateTimeKind.Utc);
        DateTime verifiedAtUtc = new DateTime(2026, 9, 1, 9, 45, 0, DateTimeKind.Utc);
        ParkItemCreateDto dto = new ParkItemCreateDto
        {
            ParkId = "park-1",
            Name = "Attraction",
            Category = ParkItemCategoryDto.Attraction,
            Type = ParkItemTypeDto.RollerCoaster,
            AttractionDetails = new AttractionDetailsDto
            {
                AccessConditions = new List<AttractionAccessConditionDto>
                {
                    new AttractionAccessConditionDto
                    {
                        Type = AttractionAccessConditionTypeDto.MinHeight,
                        ProvenanceSchemaVersion = 99,
                        SourceKind = AttractionAccessConditionSourceKindDto.Official,
                        SourceUrl = "https://example.test/restrictions",
                        SourceReference = "safety-board-2026",
                        CollectedAtUtc = collectedAtUtc,
                        VerifiedAtUtc = verifiedAtUtc,
                        SourceLanguageCode = "fr",
                        SourceSummary = new List<LocalizedTextDto>
                        {
                            new LocalizedTextDto
                            {
                                LanguageCode = "fr",
                                Value = "Taille minimale de 120 cm.",
                            },
                        },
                        SourceConfidence = AttractionAccessConditionConfidenceDto.High,
                        Scope = AttractionAccessConditionScopeDto.Seat,
                        ScopeDetail = "Rangée arrière",
                        EffectiveFrom = new DateOnly(2026, 4, 1),
                        EffectiveTo = new DateOnly(2026, 11, 2),
                    },
                },
            },
        };

        ParkItemDto result = dto.ToDomain().ToHttp();

        AttractionAccessConditionDto condition = Assert.Single(result.AttractionDetails!.AccessConditions!);
        Assert.Equal(AttractionAccessCondition.CurrentProvenanceSchemaVersion, condition.ProvenanceSchemaVersion);
        Assert.Equal(AttractionAccessConditionSourceKindDto.Official, condition.SourceKind);
        Assert.Equal("https://example.test/restrictions", condition.SourceUrl);
        Assert.Equal("safety-board-2026", condition.SourceReference);
        Assert.Equal(collectedAtUtc, condition.CollectedAtUtc);
        Assert.Equal(verifiedAtUtc, condition.VerifiedAtUtc);
        Assert.Equal("fr", condition.SourceLanguageCode);
        Assert.Equal("Taille minimale de 120 cm.", Assert.Single(condition.SourceSummary!).Value);
        Assert.Equal(AttractionAccessConditionConfidenceDto.High, condition.SourceConfidence);
        Assert.Equal(AttractionAccessConditionScopeDto.Seat, condition.Scope);
        Assert.Equal("Rangée arrière", condition.ScopeDetail);
        Assert.Equal(new DateOnly(2026, 4, 1), condition.EffectiveFrom);
        Assert.Equal(new DateOnly(2026, 11, 2), condition.EffectiveTo);
    }
}
