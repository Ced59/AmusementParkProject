using AmusementPark.Application.Features.ParkFit.Results;
using AmusementPark.Application.Features.ParkFit.Queries;
using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using AmusementPark.WebAPI.Contracts.ParkFit;
using AmusementPark.WebAPI.Mappers;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Mappers;

public sealed class ParkFitSearchHttpMapperTests
{
    private static readonly DateOnly EvaluationDate = new DateOnly(2026, 10, 10);
    private static readonly DateTime EvaluationTimestamp =
        new DateTime(2026, 9, 14, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ToApplication_ShouldGenerateOnlyEphemeralOrdinalMemberKeys()
    {
        ParkFitSearchRequestDto request = new ParkFitSearchRequestDto
        {
            EvaluationDate = EvaluationDate,
            OriginLatitude = 50.6292d,
            OriginLongitude = 3.0573d,
            Members = new[]
            {
                new ParkFitSearchMemberCriteriaDto { HeightCentimeters = 120 },
                new ParkFitSearchMemberCriteriaDto { HeightCentimeters = 180 },
            },
        };

        SearchParksByFitQuery query = request.ToApplication();

        Assert.Equal(new[] { "member-1", "member-2" },
            query.Members.Select(static member => member.MemberKey));
        Assert.Equal(50.6292d, query.OriginLatitude);
        Assert.Equal(3.0573d, query.OriginLongitude);
    }

    [Fact]
    public void ToHttp_ShouldExposeNamesExplanationsQualityAndCriticalProofs()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Parc des preuves",
            CountryCode = "FR",
            Type = ParkType.ThemePark,
        };
        ParkFitDataQualityAssessment quality = new ParkFitDataQualityAssessment
        {
            ParkId = park.Id,
            ParkName = park.Name,
            Status = ParkFitDataQualityStatus.EligibleForFitComparison,
            CoveragePercent = 100,
            LastVerifiedAtUtc = EvaluationTimestamp.AddDays(-10),
        };
        AttractionAccessCondition evidence = BuildEvidence();
        ParkFitDateAvailability availability = new ParkFitDateAvailability(
            ParkFitDateAvailabilityState.Available,
            EvaluationDate,
            ParkFitCalendarState.OpenConfirmed,
            new[]
            {
                new ParkOpeningHoursTimeRange
                {
                    OpensAt = new TimeOnly(10, 0),
                    ClosesAt = new TimeOnly(19, 0),
                },
            },
            "Europe/Paris",
            "https://example.test/calendar",
            EvaluationTimestamp.AddDays(-2));
        ParkFitSearchResult result = new ParkFitSearchResult
        {
            MethodVersion = ParkFitScoreEvaluator.MethodVersion,
            EvaluationDate = EvaluationDate,
            EvaluatedAtUtc = EvaluationTimestamp,
            TotalCandidateCount = 3,
            InspectedCandidateCount = 3,
            QualityEligibleCandidateCount = 1,
            QualityRejectedCandidateCount = 2,
            QualityStatusCounts = new Dictionary<ParkFitDataQualityStatus, int>
            {
                [ParkFitDataQualityStatus.EligibleForFitComparison] = 1,
                [ParkFitDataQualityStatus.Insufficient] = 2,
            },
            QualityIssueCounts = new Dictionary<ParkFitDataQualityIssue, int>
            {
                [ParkFitDataQualityIssue.MissingAccessConditions] = 2,
            },
            Parks = new[]
            {
                new ParkFitSearchParkResult
                {
                    Park = park,
                    DataQuality = quality,
                    Score = BuildScore(),
                    DateAvailability = availability,
                    TravelDistance = new ParkFitTravelDistance(
                        42.56d,
                        ParkFitDistanceMethod.DirectGeodesic,
                        EvaluationTimestamp),
                    EveryoneTogetherAttractionCount = 1,
                    MemberSummaries = new[]
                    {
                        new ParkFitSearchMemberSummaryResult
                        {
                            MemberNumber = 1,
                            CompatibleAloneAttractionCount = 1,
                        },
                    },
                    CriticalSources = new[]
                    {
                        new AttractionCompatibilitySourceReference(evidence),
                    },
                },
            },
        };

        ParkFitSearchResponseDto dto = result.ToHttp();

        Assert.Equal(2, dto.QualityIssueCounts["MissingAccessConditions"]);
        ParkFitSearchParkDto parkDto = Assert.Single(dto.Parks);
        Assert.Equal("Parc des preuves", parkDto.ParkName);
        Assert.Equal("Available", parkDto.ScoreState);
        Assert.Equal("Available", parkDto.DateAvailabilityState);
        Assert.Equal("OpenConfirmed", parkDto.CalendarState);
        ParkFitOpeningTimeRangeDto timeRange = Assert.Single(parkDto.OpeningTimeRanges);
        Assert.Equal("10:00", timeRange.OpensAt);
        Assert.Equal("19:00", timeRange.ClosesAt);
        Assert.Equal("https://example.test/calendar", parkDto.CalendarSourceUrl);
        Assert.Equal(42.6d, parkDto.DistanceKilometers);
        Assert.Equal("DirectGeodesic", parkDto.DistanceMethod);
        Assert.Contains("ScoreAvailable", parkDto.Reasons);
        ParkFitSearchMemberSummaryDto member = Assert.Single(parkDto.MemberSummaries);
        Assert.Equal(1, member.MemberNumber);
        Assert.Equal(1, member.CompatibleAloneAttractionCount);
        ParkFitCriticalSourceDto source = Assert.Single(parkDto.CriticalSources);
        Assert.Equal("https://example.test/access", source.Url);
        Assert.Equal(EvaluationTimestamp.AddDays(-10), source.VerifiedAtUtc);
        Assert.Equal("Taille minimale de 100 cm.", Assert.Single(source.Summaries).Value);
    }

    private static ParkFitScore BuildScore()
    {
        IReadOnlyCollection<ParkFitSubscore> subscores = new[]
        {
            new ParkFitSubscore(
                ParkFitSubscoreKind.GroupCompatibility,
                ParkFitSubscoreState.Known,
                100m,
                100m,
                ParkFitDataConfidence.High,
                evaluationDate: EvaluationDate),
            new ParkFitSubscore(
                ParkFitSubscoreKind.PreferenceCoverage,
                ParkFitSubscoreState.Known,
                100m,
                100m,
                ParkFitDataConfidence.High),
            BuildNotApplicable(ParkFitSubscoreKind.TravelConvenience),
            BuildNotApplicable(ParkFitSubscoreKind.IndoorResilience),
            BuildNotApplicable(ParkFitSubscoreKind.BudgetFit),
        };
        return new ParkFitScoreEvaluator().Evaluate(
            subscores,
            new ParkFitHardFilterEvaluation(EvaluationDate, 0, 0, 0),
            new ParkFitDateAvailability(ParkFitDateAvailabilityState.Available, EvaluationDate),
            ParkFitUnknownDataPolicy.KeepWithWarning,
            EvaluationDate,
            EvaluationTimestamp);
    }

    private static ParkFitSubscore BuildNotApplicable(ParkFitSubscoreKind kind)
    {
        return new ParkFitSubscore(
            kind,
            ParkFitSubscoreState.NotApplicable,
            null,
            0m,
            ParkFitDataConfidence.Unknown);
    }

    private static AttractionAccessCondition BuildEvidence()
    {
        return new AttractionAccessCondition
        {
            Type = AttractionAccessConditionType.MinHeight,
            Value = 100,
            Unit = AttractionAccessConditionUnit.Centimeter,
            SourceKind = AttractionAccessConditionSourceKind.Official,
            SourceUrl = "https://example.test/access",
            CollectedAtUtc = EvaluationTimestamp.AddDays(-20),
            VerifiedAtUtc = EvaluationTimestamp.AddDays(-10),
            SourceLanguageCode = "fr",
            SourceSummary = new List<LocalizedText>
            {
                new LocalizedText("fr", "Taille minimale de 100 cm."),
            },
            SourceConfidence = AttractionAccessConditionConfidence.High,
            Scope = AttractionAccessConditionScope.Attraction,
        };
    }
}
