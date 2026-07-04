using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Parks;

public sealed class ParkDataCompletenessScoreTests
{
    [Fact]
    public void CalculateDataCompletenessScore_WhenParkHasNoOfficialZones_DoesNotApplyZonePoints()
    {
        Park park = CreatePublishablePark();
        ParkDataCompletenessContext withoutOfficialZones = CreateRichParkContext() with
        {
            HasOfficialZones = false,
            ZonesTotalCount = 0,
            ZonesWithDescriptionsCount = 0,
            ParkItemsAttachedToZonesCount = 0,
        };
        ParkDataCompletenessContext missingOfficialZones = CreateRichParkContext() with
        {
            HasOfficialZones = true,
            ZonesTotalCount = 0,
            ZonesWithDescriptionsCount = 0,
            ParkItemsAttachedToZonesCount = 0,
        };

        DataCompletenessScore noOfficialZoneScore = park.CalculateDataCompletenessScore(withoutOfficialZones);
        DataCompletenessScore missingOfficialZoneScore = park.CalculateDataCompletenessScore(missingOfficialZones);

        Assert.True(missingOfficialZoneScore.ApplicableMaxPoints > noOfficialZoneScore.ApplicableMaxPoints);
        Assert.True(noOfficialZoneScore.CompletenessScore > missingOfficialZoneScore.CompletenessScore);
    }

    [Fact]
    public void CalculateDataCompletenessScore_WhenParkIsClosed_DoesNotApplyOpeningHoursPoints()
    {
        Park park = CreatePublishablePark();
        park.Status = ParkStatus.ClosedDefinitively;
        park.ClosingDate = new DateTime(2010, 10, 31);
        ParkDataCompletenessContext withoutOpeningHours = CreateRichParkContext() with
        {
            HasOpeningHours = false,
            HasOpeningHoursSource = false,
            HasOpeningHoursTimeZone = false,
            HasOpeningHoursExceptions = false,
            HasOpeningHoursRecentVerification = false,
        };
        ParkDataCompletenessContext withOpeningHours = CreateRichParkContext();

        DataCompletenessScore scoreWithoutOpeningHours = park.CalculateDataCompletenessScore(withoutOpeningHours);
        DataCompletenessScore scoreWithOpeningHours = park.CalculateDataCompletenessScore(withOpeningHours);

        Assert.Equal(108, scoreWithoutOpeningHours.ApplicableMaxPoints);
        Assert.Equal(scoreWithOpeningHours.ApplicableMaxPoints, scoreWithoutOpeningHours.ApplicableMaxPoints);
        Assert.Equal(scoreWithOpeningHours.CompletenessScore, scoreWithoutOpeningHours.CompletenessScore);
    }

    [Fact]
    public void CalculateDataCompletenessScore_WhenParkIsNotRelevant_LosesRelevancePoints()
    {
        Park validatedPark = CreatePublishablePark();
        Park notRelevantPark = CreatePublishablePark();
        notRelevantPark.AdminReviewStatus = AdminReviewStatus.NotRelevant;

        DataCompletenessScore validatedScore = validatedPark.CalculateDataCompletenessScore(CreateRichParkContext());
        DataCompletenessScore notRelevantScore = notRelevantPark.CalculateDataCompletenessScore(CreateRichParkContext());

        Assert.True(notRelevantScore.EarnedPoints < validatedScore.EarnedPoints);
        Assert.True(notRelevantScore.CompletenessScore < validatedScore.CompletenessScore);
    }

    private static Park CreatePublishablePark()
    {
        Park park = new Park
        {
            Name = "Reference Park",
            CountryCode = "FR",
            Type = ParkType.ThemePark,
            AudienceClassification = ParkAudienceClassification.Regional,
            Status = ParkStatus.Operating,
            OpeningDate = new DateTime(1992, 4, 12),
            FounderId = "founder-1",
            OperatorId = "operator-1",
            WebsiteUrl = "https://example.test",
            Street = "1 Main Street",
            City = "Paris",
            PostalCode = "75000",
            CurrentLogoImageId = "image-logo",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
            Descriptions = CreateLocalizedTexts(),
        };
        park.SetPosition(48.866, 2.333);
        return park;
    }

    private static ParkDataCompletenessContext CreateRichParkContext()
    {
        return new ParkDataCompletenessContext
        {
            ParkItemsTotalCount = 8,
            ParkItemsVisibleCount = 8,
            DistinctParkItemCategoryCount = 4,
            ClosedImportantParkItemsCount = 0,
            ParkItemsWithKnownStatusOrDatesCount = 8,
            AttractionManufacturerIdsCount = 4,
            AttractionsWithAccessConditionsCount = 4,
            HasOfficialZones = true,
            ZonesTotalCount = 3,
            ZonesWithDescriptionsCount = 3,
            ParkItemsAttachedToZonesCount = 8,
            ParkItemsWithDescriptionsCount = 8,
            CommercialOrServiceItemsWithDescriptionsCount = 3,
            ParkPublishedImageCount = 4,
            ParkImagesWithResolvedOwnerCount = 4,
            ParkImagesWithLocalizedAltTextCount = 4,
            ParkItemPublishedImageCount = 8,
            HasOriginalMedia = true,
            HasOpeningHours = true,
            OpeningHoursStatus = ParkOpeningHoursAdminStatus.UpToDate,
            HasOpeningHoursSource = true,
            HasOpeningHoursTimeZone = true,
            HasOpeningHoursExceptions = true,
            HasOpeningHoursRecentVerification = true,
            ParkHistoryEventCount = 3,
            MajorHistoryEventCount = 2,
            ParkItemHistoryEventCount = 2,
            PublishedArticleCount = 1,
            StructuredArticleCount = 1,
            LocalizedHistoryContentCount = 2,
            HistoryEventsWithSourcesCount = 3,
            HistoryEventsWithMediaCount = 1,
            ImportantReferencesWithDescriptionsCount = 3,
            ReferencesWithUsefulDetailsCount = 3,
            HasCriticalSources = true,
            HasDocumentedRemainingDebt = false,
            HasPublicSeoSignals = true,
        };
    }

    private static List<LocalizedText> CreateLocalizedTexts()
    {
        return new List<LocalizedText>
        {
            new("fr", "Un parc de référence avec une offre variée, des attractions connues et des informations publiques utiles pour préparer une visite."),
            new("en", "A reference park with a varied offer, known attractions and useful public information for planning a visit."),
            new("de", "Ein Referenzpark mit vielfältigem Angebot, bekannten Attraktionen und nützlichen öffentlichen Informationen für den Besuch."),
            new("nl", "Een referentiepark met een gevarieerd aanbod, bekende attracties en nuttige publieke informatie voor een bezoek."),
            new("it", "Un parco di riferimento con un'offerta varia, attrazioni note e informazioni pubbliche utili per la visita."),
            new("es", "Un parque de referencia con oferta variada, atracciones conocidas e información pública útil para preparar la visita."),
            new("pl", "Park referencyjny z różnorodną ofertą, znanymi atrakcjami i przydatnymi informacjami publicznymi dla odwiedzających."),
            new("pt", "Um parque de referência com oferta variada, atrações conhecidas e informação pública útil para preparar a visita."),
        };
    }
}
