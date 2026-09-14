using AmusementPark.Application.Features.ParkItems;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkItems;

public sealed class ParkItemNormalizationRestrictionProvenanceTests
{
    [Fact]
    public void Normalize_ShouldCanonicalizeRestrictionProvenanceWithoutLosingEvidence()
    {
        ParkItem parkItem = new ParkItem
        {
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
                        ProvenanceSchemaVersion = 99,
                        SourceKind = AttractionAccessConditionSourceKind.Official,
                        SourceUrl = " https://example.test/restrictions ",
                        SourceReference = " safety-board-2026 ",
                        CollectedAtUtc = new DateTime(2026, 8, 1, 8, 30, 0, DateTimeKind.Unspecified),
                        VerifiedAtUtc = new DateTime(2026, 9, 1, 9, 45, 0, DateTimeKind.Utc),
                        SourceLanguageCode = " FR ",
                        SourceSummary = new List<LocalizedText>
                        {
                            new LocalizedText(" FR ", " Taille minimale de 120 cm. "),
                        },
                        SourceConfidence = AttractionAccessConditionConfidence.High,
                        Scope = AttractionAccessConditionScope.Seat,
                        ScopeDetail = " Rangée arrière ",
                        EffectiveFrom = new DateOnly(2026, 4, 1),
                        EffectiveTo = new DateOnly(2026, 11, 2),
                    },
                },
            },
        };

        ParkItemNormalization.Normalize(parkItem);

        AttractionAccessCondition condition = Assert.Single(parkItem.AttractionDetails!.AccessConditions);
        Assert.Equal(AttractionAccessCondition.CurrentProvenanceSchemaVersion, condition.ProvenanceSchemaVersion);
        Assert.Equal("https://example.test/restrictions", condition.SourceUrl);
        Assert.Equal("safety-board-2026", condition.SourceReference);
        Assert.Null(condition.CollectedAtUtc);
        Assert.Equal(DateTimeKind.Utc, condition.VerifiedAtUtc!.Value.Kind);
        Assert.Equal("fr", condition.SourceLanguageCode);
        LocalizedText summary = Assert.Single(condition.SourceSummary);
        Assert.Equal("fr", summary.LanguageCode);
        Assert.Equal("Taille minimale de 120 cm.", summary.Value);
        Assert.Equal("Rangée arrière", condition.ScopeDetail);
        Assert.Equal(new DateOnly(2026, 4, 1), condition.EffectiveFrom);
        Assert.Equal(new DateOnly(2026, 11, 2), condition.EffectiveTo);
    }
}
