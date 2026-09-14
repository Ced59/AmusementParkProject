using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Parks;

public sealed class ParkFitDataQualityAssessorTests
{
    private static readonly DateTime EvaluationTimestamp =
        new DateTime(2026, 9, 14, 12, 0, 0, DateTimeKind.Utc);

    private readonly ParkFitDataQualityAssessor assessor = new ParkFitDataQualityAssessor();

    [Fact]
    public void Assess_WhenEveryRequiredFactIsCurrent_ShouldMakeParkEligibleForComparison()
    {
        Park park = BuildDiscoverablePark();
        ParkItem attraction = BuildAttraction(BuildCondition());

        ParkFitDataQualityAssessment result = this.assessor.Assess(
            park,
            new[] { attraction },
            BuildCurrentCalendar(),
            EvaluationTimestamp,
            TimeSpan.FromDays(365));

        Assert.Equal(ParkFitDataQualityStatus.EligibleForFitComparison, result.Status);
        Assert.Equal(100, result.CoveragePercent);
        Assert.Equal(1, result.DecisionEligibleAttractionCount);
        Assert.Equal(1, result.DecisionEligibleConditionCount);
        Assert.Empty(result.Issues);
        Assert.Empty(result.IssueSamples);
    }

    [Fact]
    public void Assess_WhenAttractionHasNoConditions_ShouldExposeActionableMissingData()
    {
        Park park = BuildDiscoverablePark();
        ParkItem attraction = BuildAttraction();

        ParkFitDataQualityAssessment result = this.assessor.Assess(
            park,
            new[] { attraction },
            null,
            EvaluationTimestamp,
            TimeSpan.FromDays(365));

        Assert.Equal(ParkFitDataQualityStatus.Insufficient, result.Status);
        Assert.Equal(0, result.CoveragePercent);
        Assert.Contains(ParkFitDataQualityIssue.MissingOpeningCalendar, result.Issues);
        Assert.Contains(ParkFitDataQualityIssue.MissingAccessConditions, result.Issues);
        Assert.Contains(ParkFitDataQualityIssue.IncompleteRestrictionCoverage, result.Issues);
        ParkFitDataQualityItemAssessment sample = Assert.Single(result.IssueSamples);
        Assert.Equal(attraction.Id, sample.ParkItemId);
        Assert.Contains(ParkFitDataQualityIssue.MissingAccessConditions, sample.Issues);
    }

    [Fact]
    public void Assess_WhenOnlyRestrictionEvidenceIsStale_ShouldUseTemporarilyStaleStatus()
    {
        Park park = BuildDiscoverablePark();
        AttractionAccessCondition condition = BuildCondition();
        condition.CollectedAtUtc = EvaluationTimestamp.AddDays(-500);
        condition.VerifiedAtUtc = EvaluationTimestamp.AddDays(-400);

        ParkFitDataQualityAssessment result = this.assessor.Assess(
            park,
            new[] { BuildAttraction(condition) },
            BuildCurrentCalendar(),
            EvaluationTimestamp,
            TimeSpan.FromDays(365));

        Assert.Equal(ParkFitDataQualityStatus.TemporarilyStale, result.Status);
        Assert.Equal(1, result.StaleEvidenceItemCount);
        Assert.Contains(ParkFitDataQualityIssue.StaleEvidence, result.Issues);
    }

    [Fact]
    public void Assess_WhenMinimumHeightExceedsMaximum_ShouldFlagAmbiguity()
    {
        Park park = BuildDiscoverablePark();
        AttractionAccessCondition minimum = BuildCondition();
        minimum.Value = 150;
        AttractionAccessCondition maximum = BuildCondition();
        maximum.Type = AttractionAccessConditionType.MaxHeight;
        maximum.Value = 120;

        ParkFitDataQualityAssessment result = this.assessor.Assess(
            park,
            new[] { BuildAttraction(minimum, maximum) },
            BuildCurrentCalendar(),
            EvaluationTimestamp,
            TimeSpan.FromDays(365));

        Assert.Equal(ParkFitDataQualityStatus.Insufficient, result.Status);
        Assert.Equal(1, result.AmbiguousItemCount);
        Assert.Contains(ParkFitDataQualityIssue.AmbiguousRestriction, result.Issues);
    }

    [Fact]
    public void Assess_WhenMixedHeightUnitsDescribeAValidRange_ShouldNotFlagAmbiguity()
    {
        AttractionAccessCondition minimum = BuildCondition();
        minimum.Value = 130;
        AttractionAccessCondition maximum = BuildCondition();
        maximum.Type = AttractionAccessConditionType.MaxHeight;
        maximum.Value = 55;
        maximum.Unit = AttractionAccessConditionUnit.Inch;

        ParkFitDataQualityAssessment result = this.assessor.Assess(
            BuildDiscoverablePark(),
            new[] { BuildAttraction(minimum, maximum) },
            BuildCurrentCalendar(),
            EvaluationTimestamp,
            TimeSpan.FromDays(365));

        Assert.Equal(ParkFitDataQualityStatus.EligibleForFitComparison, result.Status);
        Assert.DoesNotContain(ParkFitDataQualityIssue.AmbiguousRestriction, result.Issues);
    }

    [Fact]
    public void Assess_WhenMixedHeightUnitsDescribeAContradiction_ShouldFlagAmbiguity()
    {
        AttractionAccessCondition minimum = BuildCondition();
        minimum.Value = 55;
        minimum.Unit = AttractionAccessConditionUnit.Inch;
        AttractionAccessCondition maximum = BuildCondition();
        maximum.Type = AttractionAccessConditionType.MaxHeight;
        maximum.Value = 130;

        ParkFitDataQualityAssessment result = this.assessor.Assess(
            BuildDiscoverablePark(),
            new[] { BuildAttraction(minimum, maximum) },
            BuildCurrentCalendar(),
            EvaluationTimestamp,
            TimeSpan.FromDays(365));

        Assert.Equal(ParkFitDataQualityStatus.Insufficient, result.Status);
        Assert.Contains(ParkFitDataQualityIssue.AmbiguousRestriction, result.Issues);
    }

    [Fact]
    public void Assess_WhenContradictoryHeightRulesTargetDifferentVehicles_ShouldNotFlagAmbiguity()
    {
        AttractionAccessCondition minimum = BuildCondition();
        minimum.Value = 150;
        minimum.Scope = AttractionAccessConditionScope.Vehicle;
        minimum.ScopeDetail = "vehicle-a";
        AttractionAccessCondition maximum = BuildCondition();
        maximum.Type = AttractionAccessConditionType.MaxHeight;
        maximum.Value = 120;
        maximum.Scope = AttractionAccessConditionScope.Vehicle;
        maximum.ScopeDetail = "vehicle-b";

        ParkFitDataQualityAssessment result = this.assessor.Assess(
            BuildDiscoverablePark(),
            new[] { BuildAttraction(minimum, maximum) },
            BuildCurrentCalendar(),
            EvaluationTimestamp,
            TimeSpan.FromDays(365));

        Assert.Equal(ParkFitDataQualityStatus.EligibleForFitComparison, result.Status);
        Assert.DoesNotContain(ParkFitDataQualityIssue.AmbiguousRestriction, result.Issues);
    }

    [Fact]
    public void Assess_WhenContradictoryHeightRulesHaveDisjointPeriods_ShouldNotFlagAmbiguity()
    {
        AttractionAccessCondition minimum = BuildCondition();
        minimum.Value = 150;
        minimum.EffectiveFrom = new DateOnly(2026, 1, 1);
        minimum.EffectiveTo = new DateOnly(2026, 6, 30);
        AttractionAccessCondition maximum = BuildCondition();
        maximum.Type = AttractionAccessConditionType.MaxHeight;
        maximum.Value = 120;
        maximum.EffectiveFrom = new DateOnly(2026, 7, 1);
        maximum.EffectiveTo = new DateOnly(2026, 12, 31);

        ParkFitDataQualityAssessment result = this.assessor.Assess(
            BuildDiscoverablePark(),
            new[] { BuildAttraction(minimum, maximum) },
            BuildCurrentCalendar(),
            EvaluationTimestamp,
            TimeSpan.FromDays(365));

        Assert.Equal(ParkFitDataQualityStatus.EligibleForFitComparison, result.Status);
        Assert.DoesNotContain(ParkFitDataQualityIssue.AmbiguousRestriction, result.Issues);
    }

    [Fact]
    public void Assess_WhenRecommendationStructureIsIncomplete_ShouldRemainInsufficient()
    {
        Park park = BuildDiscoverablePark();
        park.Type = null;
        park.Descriptions.Clear();
        ParkItem attraction = BuildAttraction(BuildCondition());
        attraction.Type = ParkItemType.Attraction;
        attraction.AttractionDetails!.IsIndoor = null;
        attraction.AttractionDetails.IsAccessibleForReducedMobility = true;
        attraction.AttractionDetails.SourceUrl = null;

        ParkFitDataQualityAssessment result = this.assessor.Assess(
            park,
            new[] { attraction },
            BuildCurrentCalendar(),
            EvaluationTimestamp,
            TimeSpan.FromDays(365));

        Assert.Equal(ParkFitDataQualityStatus.Insufficient, result.Status);
        Assert.Contains(ParkFitDataQualityIssue.MissingParkType, result.Issues);
        Assert.Contains(ParkFitDataQualityIssue.MissingSupportedLanguageContent, result.Issues);
        Assert.Contains(ParkFitDataQualityIssue.MissingPreciseAttractionType, result.Issues);
        Assert.Contains(ParkFitDataQualityIssue.MissingIndoorOutdoorClassification, result.Issues);
        Assert.Contains(ParkFitDataQualityIssue.MissingAccessibilitySource, result.Issues);
        Assert.Equal(1, result.MissingSourceItemCount);
    }

    [Fact]
    public void Assess_WhenAccessConditionCannotBeInterpreted_ShouldNotCountItAsEligible()
    {
        AttractionAccessCondition condition = BuildCondition();
        condition.Type = AttractionAccessConditionType.MinAge;
        condition.Value = null;
        condition.Unit = AttractionAccessConditionUnit.Year;

        ParkFitDataQualityAssessment result = this.assessor.Assess(
            BuildDiscoverablePark(),
            new[] { BuildAttraction(condition) },
            BuildCurrentCalendar(),
            EvaluationTimestamp,
            TimeSpan.FromDays(365));

        Assert.Equal(ParkFitDataQualityStatus.Insufficient, result.Status);
        Assert.Equal(0, result.DecisionEligibleConditionCount);
        Assert.Contains(ParkFitDataQualityIssue.AmbiguousRestriction, result.Issues);
    }

    [Fact]
    public void Assess_WhenCoordinatesUsePlaceholderOrigin_ShouldTreatThemAsMissing()
    {
        Park park = BuildDiscoverablePark();
        park.SetPosition(0, 0);

        ParkFitDataQualityAssessment result = this.assessor.Assess(
            park,
            new[] { BuildAttraction(BuildCondition()) },
            BuildCurrentCalendar(),
            EvaluationTimestamp,
            TimeSpan.FromDays(365));

        Assert.Equal(ParkFitDataQualityStatus.Insufficient, result.Status);
        Assert.Contains(ParkFitDataQualityIssue.MissingCoordinates, result.Issues);
    }

    private static Park BuildDiscoverablePark()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Parc témoin",
            IsVisible = true,
            Type = ParkType.ThemePark,
            Status = ParkStatus.Operating,
            AdminReviewStatus = AdminReviewStatus.Validated,
            Descriptions = new List<LocalizedText>
            {
                new LocalizedText("fr", "Parc témoin pour les essais."),
            },
        };
        park.SetPosition(50.0, 3.0);
        return park;
    }

    private static ParkItem BuildAttraction(params AttractionAccessCondition[] conditions)
    {
        return new ParkItem
        {
            Id = "item-1",
            ParkId = "park-1",
            Name = "Attraction témoin",
            Category = ParkItemCategory.Attraction,
            Type = ParkItemType.FamilyRide,
            IsVisible = true,
            AttractionDetails = new AttractionDetails
            {
                IsIndoor = false,
                AccessConditions = conditions.ToList(),
            },
        };
    }

    private static AttractionAccessCondition BuildCondition()
    {
        return new AttractionAccessCondition
        {
            Type = AttractionAccessConditionType.MinHeight,
            Value = 100,
            Unit = AttractionAccessConditionUnit.Centimeter,
            SourceKind = AttractionAccessConditionSourceKind.Official,
            SourceUrl = "https://example.test/access",
            CollectedAtUtc = EvaluationTimestamp.AddDays(-30),
            VerifiedAtUtc = EvaluationTimestamp.AddDays(-15),
            SourceLanguageCode = "fr",
            SourceSummary = new List<LocalizedText>
            {
                new LocalizedText("fr", "Taille minimale de 100 cm."),
            },
            SourceConfidence = AttractionAccessConditionConfidence.High,
            Scope = AttractionAccessConditionScope.Attraction,
        };
    }

    private static ParkOpeningHoursScheduleSummary BuildCurrentCalendar()
    {
        DateOnly today = DateOnly.FromDateTime(EvaluationTimestamp);
        return new ParkOpeningHoursScheduleSummary
        {
            ParkId = "park-1",
            TimeZoneId = "UTC",
            HasScheduleData = true,
            LastDate = today.AddDays(60),
            CoverageSegments = new[]
            {
                new ParkOpeningHoursCoverageSegmentSummary
                {
                    StartDate = today,
                    EndDate = today.AddDays(60),
                },
            },
        };
    }
}
