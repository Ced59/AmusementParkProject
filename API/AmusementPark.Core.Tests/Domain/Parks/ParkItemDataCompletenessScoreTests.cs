using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Parks;

public sealed class ParkItemDataCompletenessScoreTests
{
    [Fact]
    public void CalculateDataCompletenessScore_WhenVisibleItemParentIsInvisible_LosesParentVisibilityPoint()
    {
        ParkItem item = CreateMechanicalAttraction();
        ParkItemDataCompletenessContext visibleParentContext = CreateRichParkItemContext() with
        {
            ParentParkVisible = true,
        };
        ParkItemDataCompletenessContext invisibleParentContext = CreateRichParkItemContext() with
        {
            ParentParkVisible = false,
        };

        DataCompletenessScore visibleParentScore = item.CalculateDataCompletenessScore(visibleParentContext);
        DataCompletenessScore invisibleParentScore = item.CalculateDataCompletenessScore(invisibleParentContext);

        Assert.Equal(visibleParentScore.ApplicableMaxPoints, invisibleParentScore.ApplicableMaxPoints);
        Assert.True(invisibleParentScore.EarnedPoints < visibleParentScore.EarnedPoints);
        Assert.True(invisibleParentScore.CompletenessScore < visibleParentScore.CompletenessScore);
    }

    [Fact]
    public void CalculateDataCompletenessScore_WhenItemIsRestaurant_DoesNotRequireManufacturerOrAccessConditions()
    {
        ParkItem restaurant = new ParkItem
        {
            ParkId = "park-1",
            ZoneId = "zone-1",
            Name = "Market Restaurant",
            Category = ParkItemCategory.Restaurant,
            Type = ParkItemType.Restaurant,
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
            Descriptions = CreateLocalizedTexts(),
        };
        restaurant.SetPosition(48.866, 2.333);

        DataCompletenessScore score = restaurant.CalculateDataCompletenessScore(CreateRichParkItemContext());

        Assert.True(score.ApplicableMaxPoints < 100);
        Assert.True(score.CompletenessScore >= 85);
        Assert.True(score.DataQualityLevel is DataQualityLevel.Good or DataQualityLevel.Excellent);
    }

    [Fact]
    public void CalculateDataCompletenessScore_WhenMechanicalAttractionMissesStructuredData_IsPenalized()
    {
        ParkItem attraction = CreateMechanicalAttraction();
        DataCompletenessScore completeScore = attraction.CalculateDataCompletenessScore(CreateRichParkItemContext());
        attraction.AttractionDetails = new AttractionDetails
        {
            Status = "Operating",
        };

        DataCompletenessScore score = attraction.CalculateDataCompletenessScore(CreateRichParkItemContext());

        Assert.True(score.EarnedPoints < completeScore.EarnedPoints);
        Assert.True(score.CompletenessScore < completeScore.CompletenessScore);
    }

    private static ParkItem CreateMechanicalAttraction()
    {
        ParkItem item = new ParkItem
        {
            ParkId = "park-1",
            ZoneId = "zone-1",
            Name = "Reference Coaster",
            Category = ParkItemCategory.Attraction,
            Type = ParkItemType.RollerCoaster,
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
            Descriptions = CreateLocalizedTexts(),
            AttractionDetails = new AttractionDetails
            {
                ManufacturerId = "manufacturer-1",
                Model = "Custom coaster",
                SourceUrl = "https://example.test/coaster",
                Status = "Operating",
                OpeningDate = new DateTime(2020, 6, 1),
                MaterialType = "Steel",
                SeatingType = "Sit down",
                HeightInMeters = 42,
                LengthInMeters = 900,
                SpeedInKmH = 88,
                CapacityPerHour = 1200,
                AccessConditions = new List<AttractionAccessCondition>
                {
                    new()
                    {
                        Type = AttractionAccessConditionType.MinHeight,
                        Value = 120,
                        Unit = AttractionAccessConditionUnit.Centimeter,
                    },
                    new()
                    {
                        Type = AttractionAccessConditionType.MinAgeAccompanied,
                        Value = 8,
                        Unit = AttractionAccessConditionUnit.Year,
                        RequiresAccompaniment = true,
                        MinimumCompanionAge = 16,
                    },
                    new()
                    {
                        Type = AttractionAccessConditionType.PregnancyRestriction,
                    },
                },
            },
        };
        item.SetPosition(48.866, 2.333);
        return item;
    }

    private static ParkItemDataCompletenessContext CreateRichParkItemContext()
    {
        return new ParkItemDataCompletenessContext
        {
            ParentParkResolved = true,
            ParentParkVisible = true,
            HasOfficialZoneContext = true,
            HasUsefulVisitGrouping = true,
            HasRepresentativeImage = true,
            HasResolvedImageOwner = true,
            HasLocalizedImageAltText = true,
            HasNonMisleadingImage = true,
            HasOriginalMedia = true,
            HasHistoricalImageContext = true,
            HistoryEventCount = 2,
            ClosureOrChangeHistoryEventCount = 1,
            PublishedArticleCount = 1,
            HistoryEventsWithSourcesCount = 2,
            HasReferenceDetailsOrDocumentedDebt = true,
            HasInternalLinks = true,
            HasSeoSignals = true,
            HasStructuredDataSignals = true,
            HasHumanReviewOrDocumentedDebt = true,
        };
    }

    private static List<LocalizedText> CreateLocalizedTexts()
    {
        return new List<LocalizedText>
        {
            new("fr", "Une fiche spécifique avec assez de contexte public, une description naturelle et des détails utiles pour les visiteurs."),
            new("en", "A specific page with enough public context, natural description and useful details for visitors."),
            new("de", "Eine spezifische Seite mit ausreichend öffentlichem Kontext, natürlicher Beschreibung und nützlichen Details."),
            new("nl", "Een specifieke pagina met genoeg publieke context, natuurlijke beschrijving en nuttige details voor bezoekers."),
            new("it", "Una pagina specifica con sufficiente contesto pubblico, descrizione naturale e dettagli utili per i visitatori."),
            new("es", "Una ficha específica con suficiente contexto público, descripción natural y detalles útiles para visitantes."),
            new("pl", "Konkretna strona z wystarczającym kontekstem publicznym, naturalnym opisem i przydatnymi szczegółami."),
            new("pt", "Uma ficha específica com contexto público suficiente, descrição natural e detalhes úteis para visitantes."),
        };
    }
}
