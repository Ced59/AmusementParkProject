using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Mappers;

public sealed class EntityMongoMappersAttractionAccessConditionsTests
{
    [Fact]
    public void ToDocumentToDomain_ShouldPreserveRestrictionProvenance()
    {
        DateTime collectedAtUtc = new DateTime(2026, 8, 1, 8, 30, 0, DateTimeKind.Utc);
        DateTime verifiedAtUtc = new DateTime(2026, 9, 1, 9, 45, 0, DateTimeKind.Utc);
        ParkItem parkItem = new ParkItem
        {
            Id = "attraction-1",
            ParkId = "park-1",
            Name = "Attraction",
            Category = ParkItemCategory.Attraction,
            Type = ParkItemType.RollerCoaster,
            AttractionDetails = new AttractionDetails
            {
                AccessConditions = new List<AttractionAccessCondition>
                {
                    new AttractionAccessCondition
                    {
                        Type = AttractionAccessConditionType.MinHeight,
                        Value = 120,
                        Unit = AttractionAccessConditionUnit.Centimeter,
                        SourceKind = AttractionAccessConditionSourceKind.Official,
                        SourceUrl = "https://example.test/restrictions",
                        SourceReference = "safety-board-2026",
                        CollectedAtUtc = collectedAtUtc,
                        VerifiedAtUtc = verifiedAtUtc,
                        SourceLanguageCode = "fr",
                        SourceSummary = new List<LocalizedText>
                        {
                            new LocalizedText("fr", "Taille minimale de 120 cm."),
                        },
                        SourceConfidence = AttractionAccessConditionConfidence.High,
                        Scope = AttractionAccessConditionScope.Seat,
                        ScopeDetail = "Rangée arrière",
                        EffectiveFrom = new DateOnly(2026, 4, 1),
                        EffectiveTo = new DateOnly(2026, 11, 2),
                    },
                },
            },
        };

        ParkItem result = parkItem.ToDocument().ToDomain();

        AttractionAccessCondition condition = Assert.Single(result.AttractionDetails!.AccessConditions);
        Assert.Equal(AttractionAccessCondition.CurrentProvenanceSchemaVersion, condition.ProvenanceSchemaVersion);
        Assert.Equal(AttractionAccessConditionSourceKind.Official, condition.SourceKind);
        Assert.Equal("https://example.test/restrictions", condition.SourceUrl);
        Assert.Equal("safety-board-2026", condition.SourceReference);
        Assert.Equal(collectedAtUtc, condition.CollectedAtUtc);
        Assert.Equal(verifiedAtUtc, condition.VerifiedAtUtc);
        Assert.Equal("fr", condition.SourceLanguageCode);
        Assert.Equal("Taille minimale de 120 cm.", Assert.Single(condition.SourceSummary).Value);
        Assert.Equal(AttractionAccessConditionConfidence.High, condition.SourceConfidence);
        Assert.Equal(AttractionAccessConditionScope.Seat, condition.Scope);
        Assert.Equal("Rangée arrière", condition.ScopeDetail);
        Assert.Equal(new DateOnly(2026, 4, 1), condition.EffectiveFrom);
        Assert.Equal(new DateOnly(2026, 11, 2), condition.EffectiveTo);
    }
}
