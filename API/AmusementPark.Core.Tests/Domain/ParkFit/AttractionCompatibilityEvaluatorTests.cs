using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.ParkFit;

public sealed class AttractionCompatibilityEvaluatorTests
{
    private static readonly DateTime EvaluationTimestamp =
        new DateTime(2026, 9, 14, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly EvaluationDate = DateOnly.FromDateTime(EvaluationTimestamp);
    private static readonly TimeSpan MaximumEvidenceAge = TimeSpan.FromDays(365);

    private readonly AttractionCompatibilityEvaluator evaluator =
        new AttractionCompatibilityEvaluator();

    [Fact]
    public void Evaluate_WhenThereIsNoCondition_ShouldReturnNotApplicable()
    {
        AttractionCompatibility result = this.Evaluate(
            new ParkFitMemberProfile(heightCentimeters: 120));

        Assert.Equal(AttractionCompatibilityState.NotApplicable, result.State);
        Assert.Equal(ParkFitDataConfidence.Unknown, result.Confidence);
        Assert.Contains(
            result.Reasons,
            reason => reason.Code == AttractionCompatibilityReasonCode.NoApplicableCondition);
    }

    [Theory]
    [InlineData(-30, -1)]
    [InlineData(1, 30)]
    public void Evaluate_WhenConditionIsOutsideTheEvaluationDate_ShouldReturnNotApplicable(
        int effectiveFromOffsetDays,
        int effectiveToOffsetDays)
    {
        AttractionAccessCondition condition = BuildHeightCondition(
            AttractionAccessConditionType.MinHeight,
            100);
        condition.EffectiveFrom = EvaluationDate.AddDays(effectiveFromOffsetDays);
        condition.EffectiveTo = EvaluationDate.AddDays(effectiveToOffsetDays);

        AttractionCompatibility result = this.Evaluate(
            new ParkFitMemberProfile(heightCentimeters: 120),
            condition);

        Assert.Equal(AttractionCompatibilityState.NotApplicable, result.State);
    }

    [Theory]
    [InlineData(100, AttractionCompatibilityState.CompatibleAlone)]
    [InlineData(99, AttractionCompatibilityState.Incompatible)]
    public void Evaluate_MinimumHeight_ShouldTreatTheExactThresholdAsCompatible(
        int heightCentimeters,
        AttractionCompatibilityState expectedState)
    {
        AttractionCompatibility result = this.Evaluate(
            new ParkFitMemberProfile(heightCentimeters: heightCentimeters),
            BuildHeightCondition(AttractionAccessConditionType.MinHeight, 100));

        Assert.Equal(expectedState, result.State);
    }

    [Theory]
    [InlineData(200, AttractionCompatibilityState.CompatibleAlone)]
    [InlineData(201, AttractionCompatibilityState.Incompatible)]
    public void Evaluate_MaximumHeight_ShouldTreatTheExactThresholdAsCompatible(
        int heightCentimeters,
        AttractionCompatibilityState expectedState)
    {
        AttractionCompatibility result = this.Evaluate(
            new ParkFitMemberProfile(heightCentimeters: heightCentimeters),
            BuildHeightCondition(AttractionAccessConditionType.MaxHeight, 200));

        Assert.Equal(expectedState, result.State);
    }

    [Fact]
    public void Evaluate_WhenHeightUnitIsInches_ShouldCompareInCentimeters()
    {
        AttractionAccessCondition condition = BuildHeightCondition(
            AttractionAccessConditionType.MinHeight,
            40);
        condition.Unit = AttractionAccessConditionUnit.Inch;

        AttractionCompatibility result = this.Evaluate(
            new ParkFitMemberProfile(heightCentimeters: 102),
            condition);

        Assert.Equal(AttractionCompatibilityState.CompatibleAlone, result.State);
    }

    [Theory]
    [InlineData(99, AttractionCompatibilityState.Incompatible)]
    [InlineData(100, AttractionCompatibilityState.CompatibleAlone)]
    [InlineData(200, AttractionCompatibilityState.CompatibleAlone)]
    [InlineData(201, AttractionCompatibilityState.Incompatible)]
    public void Evaluate_CombinedMinimumAndMaximumHeight_ShouldApplyBothInclusiveBounds(
        int heightCentimeters,
        AttractionCompatibilityState expectedState)
    {
        AttractionCompatibility result = this.Evaluate(
            new ParkFitMemberProfile(heightCentimeters: heightCentimeters),
            BuildHeightCondition(AttractionAccessConditionType.MinHeight, 100),
            BuildHeightCondition(AttractionAccessConditionType.MaxHeight, 200));

        Assert.Equal(expectedState, result.State);
    }

    [Theory]
    [InlineData(99, AttractionCompatibilityState.Incompatible)]
    [InlineData(100, AttractionCompatibilityState.CompatibleWithCompanion)]
    public void Evaluate_OnlyAccompaniedMinimum_ShouldApplyItsInclusiveLowerBound(
        int heightCentimeters,
        AttractionCompatibilityState expectedState)
    {
        AttractionCompatibility result = this.Evaluate(
            new ParkFitMemberProfile(
                heightCentimeters,
                canBeAccompanied: true),
            BuildAccompaniedHeightCondition(100));

        Assert.Equal(expectedState, result.State);
    }

    [Theory]
    [InlineData(99, true, AttractionCompatibilityState.Incompatible)]
    [InlineData(100, true, AttractionCompatibilityState.CompatibleWithCompanion)]
    [InlineData(119, true, AttractionCompatibilityState.CompatibleWithCompanion)]
    [InlineData(120, null, AttractionCompatibilityState.CompatibleAlone)]
    public void Evaluate_PairedHeightThresholds_ShouldDistinguishAloneAndAccompaniedAccess(
        int heightCentimeters,
        bool? canBeAccompanied,
        AttractionCompatibilityState expectedState)
    {
        AttractionCompatibility result = this.Evaluate(
            new ParkFitMemberProfile(
                heightCentimeters,
                canBeAccompanied: canBeAccompanied,
                availableCompanionAgeRange: canBeAccompanied == true
                    ? new ParkFitAgeRange(18, 70)
                    : null),
            BuildHeightCondition(AttractionAccessConditionType.MinHeight, 120),
            BuildAccompaniedHeightCondition(100, minimumCompanionAge: 16));

        Assert.Equal(expectedState, result.State);
    }

    [Theory]
    [InlineData(null, AttractionCompatibilityState.Unknown)]
    [InlineData(false, AttractionCompatibilityState.Incompatible)]
    public void Evaluate_WhenAccompanimentIsNeeded_ShouldNotAssumeItIsAvailable(
        bool? canBeAccompanied,
        AttractionCompatibilityState expectedState)
    {
        AttractionCompatibility result = this.Evaluate(
            new ParkFitMemberProfile(
                heightCentimeters: 110,
                canBeAccompanied: canBeAccompanied),
            BuildHeightCondition(AttractionAccessConditionType.MinHeight, 120),
            BuildAccompaniedHeightCondition(100));

        Assert.Equal(expectedState, result.State);
    }

    [Theory]
    [InlineData(10, 15, AttractionCompatibilityState.Incompatible)]
    [InlineData(15, 16, AttractionCompatibilityState.Unknown)]
    [InlineData(16, 70, AttractionCompatibilityState.CompatibleWithCompanion)]
    public void Evaluate_WhenCompanionAgeIsRestricted_ShouldKeepAgeBandUncertainty(
        int companionMinimumAge,
        int companionMaximumAge,
        AttractionCompatibilityState expectedState)
    {
        AttractionCompatibility result = this.Evaluate(
            new ParkFitMemberProfile(
                110,
                canBeAccompanied: true,
                availableCompanionAgeRange: new ParkFitAgeRange(
                    companionMinimumAge,
                    companionMaximumAge)),
            BuildHeightCondition(AttractionAccessConditionType.MinHeight, 120),
            BuildAccompaniedHeightCondition(100, minimumCompanionAge: 16));

        Assert.Equal(expectedState, result.State);
        Assert.Contains(
            result.Reasons,
            reason => reason.MinimumCompanionAge == 16);
    }

    [Fact]
    public void Evaluate_WhenHeightIsMissing_ShouldReturnUnknown()
    {
        AttractionCompatibility result = this.Evaluate(
            new ParkFitMemberProfile(),
            BuildHeightCondition(AttractionAccessConditionType.MinHeight, 100));

        Assert.Equal(AttractionCompatibilityState.Unknown, result.State);
        Assert.Contains(
            result.Reasons,
            reason => reason.Code == AttractionCompatibilityReasonCode.HeightMissing);
    }

    [Fact]
    public void Evaluate_WhenHeightIsMissing_ShouldSelectAStableReasonCondition()
    {
        ParkFitMemberProfile profile = new ParkFitMemberProfile();
        AttractionAccessCondition minimum = BuildHeightCondition(
            AttractionAccessConditionType.MinHeight,
            100);
        AttractionAccessCondition maximum = BuildHeightCondition(
            AttractionAccessConditionType.MaxHeight,
            200);

        AttractionCompatibility first = this.Evaluate(profile, minimum, maximum);
        AttractionCompatibility second = this.Evaluate(profile, maximum, minimum);

        Assert.Equal(
            first.Reasons.Select(static reason => new
            {
                reason.Code,
                reason.ConditionType,
                reason.RequiredValue,
            }),
            second.Reasons.Select(static reason => new
            {
                reason.Code,
                reason.ConditionType,
                reason.RequiredValue,
            }));
    }

    [Theory]
    [InlineData(12, 12, AttractionCompatibilityState.Incompatible)]
    [InlineData(12, 14, AttractionCompatibilityState.Unknown)]
    [InlineData(14, 14, AttractionCompatibilityState.CompatibleAlone)]
    public void Evaluate_MinimumAge_ShouldPreserveAgeBandAmbiguity(
        int minimumAge,
        int maximumAge,
        AttractionCompatibilityState expectedState)
    {
        AttractionCompatibility result = this.Evaluate(
            new ParkFitMemberProfile(ageRange: new ParkFitAgeRange(minimumAge, maximumAge)),
            BuildAgeCondition(AttractionAccessConditionType.MinAge, 14));

        Assert.Equal(expectedState, result.State);
    }

    [Fact]
    public void Evaluate_PairedAgeThresholds_ShouldAllowTheWholeBandWithCompanion()
    {
        AttractionCompatibility result = this.Evaluate(
            new ParkFitMemberProfile(
                ageRange: new ParkFitAgeRange(10, 13),
                canBeAccompanied: true,
                availableCompanionAgeRange: new ParkFitAgeRange(18, 70)),
            BuildAgeCondition(AttractionAccessConditionType.MinAge, 14),
            BuildAccompaniedAgeCondition(10, minimumCompanionAge: 18));

        Assert.Equal(AttractionCompatibilityState.CompatibleWithCompanion, result.State);
    }

    [Fact]
    public void Evaluate_WhenAgeBandCrossesTheAloneThresholdAndCompanionIsUnavailable_ShouldRemainUnknown()
    {
        AttractionCompatibility result = this.Evaluate(
            new ParkFitMemberProfile(
                ageRange: new ParkFitAgeRange(10, 15),
                canBeAccompanied: false),
            BuildAgeCondition(AttractionAccessConditionType.MinAge, 14),
            BuildAccompaniedAgeCondition(10, minimumCompanionAge: 18));

        Assert.Equal(AttractionCompatibilityState.Unknown, result.State);
        Assert.Contains(
            result.Reasons,
            reason => reason.Code == AttractionCompatibilityReasonCode.AgeRangeCrossesThreshold);
        Assert.Contains(
            result.Reasons,
            reason => reason.Code == AttractionCompatibilityReasonCode.AccompanimentUnavailable);
    }

    [Fact]
    public void Evaluate_WhenAgeRangeIsMissing_ShouldReturnUnknown()
    {
        AttractionCompatibility result = this.Evaluate(
            new ParkFitMemberProfile(),
            BuildAgeCondition(AttractionAccessConditionType.MinAge, 14));

        Assert.Equal(AttractionCompatibilityState.Unknown, result.State);
        Assert.Contains(
            result.Reasons,
            reason => reason.Code == AttractionCompatibilityReasonCode.AgeRangeMissing);
    }

    [Fact]
    public void Evaluate_WhenHeightRangeIsContradictory_ShouldReturnUnknown()
    {
        AttractionCompatibility result = this.Evaluate(
            new ParkFitMemberProfile(heightCentimeters: 130),
            BuildHeightCondition(AttractionAccessConditionType.MinHeight, 150),
            BuildHeightCondition(AttractionAccessConditionType.MaxHeight, 120));

        Assert.Equal(AttractionCompatibilityState.Unknown, result.State);
        Assert.Contains(
            result.Reasons,
            reason => reason.Code == AttractionCompatibilityReasonCode.ConflictingConditions);
    }

    [Theory]
    [InlineData(AttractionAccessConditionType.MinHeight, AttractionAccessConditionType.MinHeightAccompanied)]
    [InlineData(AttractionAccessConditionType.MinAge, AttractionAccessConditionType.MinAgeAccompanied)]
    public void Evaluate_WhenAccompaniedThresholdIsStricterThanAlone_ShouldReturnUnknown(
        AttractionAccessConditionType aloneType,
        AttractionAccessConditionType accompaniedType)
    {
        AttractionAccessCondition alone = aloneType == AttractionAccessConditionType.MinHeight
            ? BuildHeightCondition(aloneType, 100)
            : BuildAgeCondition(aloneType, 10);
        AttractionAccessCondition accompanied = accompaniedType == AttractionAccessConditionType.MinHeightAccompanied
            ? BuildAccompaniedHeightCondition(120)
            : BuildAccompaniedAgeCondition(12);
        ParkFitMemberProfile profile = aloneType == AttractionAccessConditionType.MinHeight
            ? new ParkFitMemberProfile(
                110,
                canBeAccompanied: true,
                availableCompanionAgeRange: new ParkFitAgeRange(18, 70))
            : new ParkFitMemberProfile(
                ageRange: new ParkFitAgeRange(11, 11),
                canBeAccompanied: true,
                availableCompanionAgeRange: new ParkFitAgeRange(18, 70));

        AttractionCompatibility result = this.Evaluate(profile, alone, accompanied);

        Assert.Equal(AttractionCompatibilityState.Unknown, result.State);
    }

    [Fact]
    public void Evaluate_WhenEvidenceIsStale_ShouldReturnUnknownWithExactEvidenceIssue()
    {
        AttractionAccessCondition condition = BuildHeightCondition(
            AttractionAccessConditionType.MinHeight,
            100);
        condition.VerifiedAtUtc = EvaluationTimestamp.AddDays(-400);

        AttractionCompatibility result = this.Evaluate(
            new ParkFitMemberProfile(heightCentimeters: 120),
            condition);

        Assert.Equal(AttractionCompatibilityState.Unknown, result.State);
        AttractionCompatibilityReason reason = Assert.Single(result.Reasons);
        Assert.Equal(
            AttractionCompatibilityReasonCode.ConditionEvidenceUnusable,
            reason.Code);
        Assert.Contains(
            AttractionAccessConditionEvidenceIssue.VerificationStale,
            reason.EvidenceIssues);
        Assert.Single(result.Sources);
    }

    [Fact]
    public void Evaluate_WhenAccompaniedHeightFallbackIsStale_ShouldNotRejectFromTheAloneThreshold()
    {
        AttractionAccessCondition accompanied = BuildAccompaniedHeightCondition(100, 16);
        accompanied.VerifiedAtUtc = EvaluationTimestamp.AddDays(-400);

        AttractionCompatibility result = this.Evaluate(
            new ParkFitMemberProfile(
                110,
                canBeAccompanied: true,
                availableCompanionAgeRange: new ParkFitAgeRange(18, 70)),
            BuildHeightCondition(AttractionAccessConditionType.MinHeight, 120),
            accompanied);

        Assert.Equal(AttractionCompatibilityState.Unknown, result.State);
        Assert.Contains(
            result.Reasons,
            reason => reason.Code == AttractionCompatibilityReasonCode.ConditionEvidenceUnusable);
        Assert.DoesNotContain(
            result.Reasons,
            reason => reason.Code == AttractionCompatibilityReasonCode.BelowMinimumHeight);
    }

    [Fact]
    public void Evaluate_WhenAccompaniedAgeFallbackIsStale_ShouldNotRejectFromTheAloneThreshold()
    {
        AttractionAccessCondition accompanied = BuildAccompaniedAgeCondition(10, 18);
        accompanied.VerifiedAtUtc = EvaluationTimestamp.AddDays(-400);

        AttractionCompatibility result = this.Evaluate(
            new ParkFitMemberProfile(
                ageRange: new ParkFitAgeRange(12, 12),
                canBeAccompanied: true,
                availableCompanionAgeRange: new ParkFitAgeRange(18, 70)),
            BuildAgeCondition(AttractionAccessConditionType.MinAge, 14),
            accompanied);

        Assert.Equal(AttractionCompatibilityState.Unknown, result.State);
        Assert.Contains(
            result.Reasons,
            reason => reason.Code == AttractionCompatibilityReasonCode.ConditionEvidenceUnusable);
        Assert.DoesNotContain(
            result.Reasons,
            reason => reason.Code == AttractionCompatibilityReasonCode.BelowMinimumAge);
    }

    [Fact]
    public void Evaluate_WhenStaleHeightFallbackCannotBeUsed_ShouldKeepTheCertainSoloRejection()
    {
        AttractionAccessCondition accompanied = BuildAccompaniedHeightCondition(100, 16);
        accompanied.VerifiedAtUtc = EvaluationTimestamp.AddDays(-400);

        AttractionCompatibility result = this.Evaluate(
            new ParkFitMemberProfile(
                110,
                canBeAccompanied: false),
            BuildHeightCondition(AttractionAccessConditionType.MinHeight, 120),
            accompanied);

        Assert.Equal(AttractionCompatibilityState.Incompatible, result.State);
        Assert.Contains(
            result.Reasons,
            reason => reason.Code == AttractionCompatibilityReasonCode.BelowMinimumHeight);
    }

    [Fact]
    public void Evaluate_WhenStaleAgeFallbackCannotBeUsed_ShouldKeepTheCertainSoloRejection()
    {
        AttractionAccessCondition accompanied = BuildAccompaniedAgeCondition(10, 18);
        accompanied.VerifiedAtUtc = EvaluationTimestamp.AddDays(-400);

        AttractionCompatibility result = this.Evaluate(
            new ParkFitMemberProfile(
                ageRange: new ParkFitAgeRange(12, 12),
                canBeAccompanied: false),
            BuildAgeCondition(AttractionAccessConditionType.MinAge, 14),
            accompanied);

        Assert.Equal(AttractionCompatibilityState.Incompatible, result.State);
        Assert.Contains(
            result.Reasons,
            reason => reason.Code == AttractionCompatibilityReasonCode.BelowMinimumAge);
    }

    [Fact]
    public void Evaluate_WhenASecondarySourceIsTheOnlyEvidence_ShouldReturnUnknown()
    {
        AttractionAccessCondition condition = BuildHeightCondition(
            AttractionAccessConditionType.MinHeight,
            100);
        condition.SourceKind = AttractionAccessConditionSourceKind.VerifiedSecondary;

        AttractionCompatibility result = this.Evaluate(
            new ParkFitMemberProfile(heightCentimeters: 120),
            condition);

        Assert.Equal(AttractionCompatibilityState.Unknown, result.State);
        Assert.Contains(
            result.Reasons,
            reason => reason.EvidenceIssues.Contains(
                AttractionAccessConditionEvidenceIssue.SourceNotDecisionEligible));
    }

    [Fact]
    public void Evaluate_WhenEffectivePeriodIsInvalid_ShouldReturnUnknownInsteadOfIgnoringIt()
    {
        AttractionAccessCondition condition = BuildHeightCondition(
            AttractionAccessConditionType.MinHeight,
            100);
        condition.EffectiveFrom = EvaluationDate.AddDays(10);
        condition.EffectiveTo = EvaluationDate.AddDays(-10);

        AttractionCompatibility result = this.Evaluate(
            new ParkFitMemberProfile(heightCentimeters: 120),
            condition);

        Assert.Equal(AttractionCompatibilityState.Unknown, result.State);
        Assert.Contains(
            result.Reasons,
            reason => reason.EvidenceIssues.Contains(
                AttractionAccessConditionEvidenceIssue.InvalidEffectivePeriod));
    }

    [Fact]
    public void Evaluate_WhenReliableRuleIsViolated_ShouldKeepIncompatibleDespiteAnotherUnknownRule()
    {
        AttractionAccessCondition staleCondition = BuildHeightCondition(
            AttractionAccessConditionType.MaxHeight,
            200);
        staleCondition.VerifiedAtUtc = EvaluationTimestamp.AddDays(-400);

        AttractionCompatibility result = this.Evaluate(
            new ParkFitMemberProfile(heightCentimeters: 90),
            BuildHeightCondition(AttractionAccessConditionType.MinHeight, 100),
            staleCondition);

        Assert.Equal(AttractionCompatibilityState.Incompatible, result.State);
        Assert.Contains(
            result.Reasons,
            reason => reason.Code == AttractionCompatibilityReasonCode.BelowMinimumHeight);
        Assert.Contains(
            result.Reasons,
            reason => reason.Code == AttractionCompatibilityReasonCode.ConditionEvidenceUnusable);
    }

    [Fact]
    public void Evaluate_WhenConditionDefinitionIsInvalid_ShouldReturnUnknown()
    {
        AttractionAccessCondition condition = BuildHeightCondition(
            AttractionAccessConditionType.MinHeight,
            100);
        condition.Unit = AttractionAccessConditionUnit.Year;

        AttractionCompatibility result = this.Evaluate(
            new ParkFitMemberProfile(heightCentimeters: 120),
            condition);

        Assert.Equal(AttractionCompatibilityState.Unknown, result.State);
        Assert.Contains(
            result.Reasons,
            reason => reason.Code == AttractionCompatibilityReasonCode.ConditionDefinitionUnusable
                && reason.SemanticIssues.Contains(
                    AttractionAccessConditionSemanticIssue.InvalidUnit));
    }

    [Fact]
    public void Evaluate_WhenCompanionAgeHasNoAccompanimentSignal_ShouldReturnUnknown()
    {
        AttractionAccessCondition condition = BuildHeightCondition(
            AttractionAccessConditionType.MinHeight,
            100);
        condition.MinimumCompanionAge = 18;

        AttractionCompatibility result = this.Evaluate(
            new ParkFitMemberProfile(heightCentimeters: 120),
            condition);

        Assert.Equal(AttractionCompatibilityState.Unknown, result.State);
        Assert.Contains(
            result.Reasons,
            reason => reason.SemanticIssues.Contains(
                AttractionAccessConditionSemanticIssue.InconsistentAccompaniment));
    }

    [Fact]
    public void Evaluate_WhenConditionTargetsOneVehicle_ShouldNotPromiseAttractionWideAccess()
    {
        AttractionAccessCondition condition = BuildHeightCondition(
            AttractionAccessConditionType.MinHeight,
            100);
        condition.Scope = AttractionAccessConditionScope.Vehicle;
        condition.ScopeDetail = "vehicle-a";

        AttractionCompatibility result = this.Evaluate(
            new ParkFitMemberProfile(heightCentimeters: 120),
            condition);

        Assert.Equal(AttractionCompatibilityState.Unknown, result.State);
        Assert.Contains(
            result.Reasons,
            reason => reason.Code == AttractionCompatibilityReasonCode.ScopedConditionRequiresConfiguration);
    }

    [Theory]
    [InlineData(AttractionAccessConditionType.PregnancyRestriction)]
    [InlineData(AttractionAccessConditionType.HeartRestriction)]
    [InlineData(AttractionAccessConditionType.BackNeckRestriction)]
    [InlineData(AttractionAccessConditionType.WheelchairTransferRequired)]
    [InlineData(AttractionAccessConditionType.AccessPassRequired)]
    [InlineData(AttractionAccessConditionType.Custom)]
    public void Evaluate_WhenRestrictionNeedsAPersonalConfirmation_ShouldReturnUnknown(
        AttractionAccessConditionType type)
    {
        AttractionAccessCondition condition = BuildCondition(type, null, null);
        if (type == AttractionAccessConditionType.Custom)
        {
            condition.CustomTypeKey = "operator-specific-rule";
        }

        AttractionCompatibility result = this.Evaluate(new ParkFitMemberProfile(), condition);

        Assert.Equal(AttractionCompatibilityState.Unknown, result.State);
        Assert.Contains(
            result.Reasons,
            reason => reason.Code == AttractionCompatibilityReasonCode.PersonalRestrictionRequiresConfirmation);
    }

    [Fact]
    public void Evaluate_ShouldExposeMethodConfidenceOldestVerificationAndDeduplicatedSources()
    {
        AttractionAccessCondition minimum = BuildHeightCondition(
            AttractionAccessConditionType.MinHeight,
            100);
        minimum.SourceConfidence = AttractionAccessConditionConfidence.High;
        minimum.VerifiedAtUtc = EvaluationTimestamp.AddDays(-10);
        AttractionAccessCondition maximum = BuildHeightCondition(
            AttractionAccessConditionType.MaxHeight,
            200);
        maximum.SourceConfidence = AttractionAccessConditionConfidence.Medium;
        maximum.VerifiedAtUtc = EvaluationTimestamp.AddDays(-20);

        AttractionCompatibility result = this.Evaluate(
            new ParkFitMemberProfile(heightCentimeters: 120),
            minimum,
            maximum);

        Assert.Equal(AttractionCompatibilityEvaluator.MethodVersion, result.MethodVersion);
        Assert.Equal(ParkFitDataConfidence.Medium, result.Confidence);
        Assert.Equal(EvaluationTimestamp.AddDays(-20), result.LastVerifiedAtUtc);
        Assert.Equal(EvaluationTimestamp, result.EvaluatedAtUtc);
        Assert.Equal(EvaluationDate, result.EvaluationDate);
        Assert.Equal(2, result.Sources.Count);
    }

    [Fact]
    public void Evaluate_WhenConditionsShareASource_ShouldMergeEveryDistinctSummaryDeterministically()
    {
        AttractionAccessCondition minimum = BuildHeightCondition(
            AttractionAccessConditionType.MinHeight,
            100);
        minimum.SourceSummary = new List<LocalizedText>
        {
            new LocalizedText("fr", "Taille minimale officielle."),
        };
        AttractionAccessCondition maximum = BuildHeightCondition(
            AttractionAccessConditionType.MaxHeight,
            200);
        maximum.SourceUrl = minimum.SourceUrl;
        maximum.SourceSummary = new List<LocalizedText>
        {
            new LocalizedText("fr", "Taille maximale officielle."),
        };

        AttractionCompatibility first = this.Evaluate(
            new ParkFitMemberProfile(heightCentimeters: 120),
            minimum,
            maximum);
        AttractionCompatibility second = this.Evaluate(
            new ParkFitMemberProfile(heightCentimeters: 120),
            maximum,
            minimum);

        AttractionCompatibilitySourceReference firstSource = Assert.Single(first.Sources);
        AttractionCompatibilitySourceReference secondSource = Assert.Single(second.Sources);
        Assert.Equal(2, firstSource.Summaries.Count);
        Assert.Equal(
            firstSource.Summaries.Select(static summary => summary.Value),
            secondSource.Summaries.Select(static summary => summary.Value));
    }

    [Fact]
    public void Evaluate_WhenSourceLocationsMatch_ShouldOrderDistinctLanguagesDeterministically()
    {
        AttractionAccessCondition french = BuildHeightCondition(
            AttractionAccessConditionType.MinHeight,
            100);
        french.SourceLanguageCode = "fr";
        AttractionAccessCondition english = BuildHeightCondition(
            AttractionAccessConditionType.MaxHeight,
            200);
        english.SourceUrl = french.SourceUrl;
        english.SourceLanguageCode = "en";

        AttractionCompatibility first = this.Evaluate(
            new ParkFitMemberProfile(heightCentimeters: 120),
            french,
            english);
        AttractionCompatibility second = this.Evaluate(
            new ParkFitMemberProfile(heightCentimeters: 120),
            english,
            french);

        Assert.Equal(
            new[] { "en", "fr" },
            first.Sources.Select(static source => source.LanguageCode));
        Assert.Equal(
            first.Sources.Select(static source => source.LanguageCode),
            second.Sources.Select(static source => source.LanguageCode));
    }

    [Fact]
    public void Evaluate_ShouldBeInvariantToConditionOrder()
    {
        ParkFitMemberProfile profile = new ParkFitMemberProfile(
            110,
            canBeAccompanied: true,
            availableCompanionAgeRange: new ParkFitAgeRange(18, 70));
        AttractionAccessCondition alone = BuildHeightCondition(
            AttractionAccessConditionType.MinHeight,
            120);
        AttractionAccessCondition accompanied = BuildAccompaniedHeightCondition(100, 16);

        AttractionCompatibility first = this.Evaluate(profile, alone, accompanied);
        AttractionCompatibility second = this.Evaluate(profile, accompanied, alone);

        Assert.Equal(first.State, second.State);
        Assert.Equal(
            first.Reasons.Select(SerializeReason),
            second.Reasons.Select(SerializeReason));
    }

    [Fact]
    public void Evaluate_EqualHeightThresholds_ShouldUseTheStrictestCompanionAgeDeterministically()
    {
        ParkFitMemberProfile profile = new ParkFitMemberProfile(
            110,
            canBeAccompanied: true,
            availableCompanionAgeRange: new ParkFitAgeRange(18, 70));
        AttractionAccessCondition minimumAge16 = BuildAccompaniedHeightCondition(100, 16);
        AttractionAccessCondition minimumAge18 = BuildAccompaniedHeightCondition(100, 18);

        AttractionCompatibility first = this.Evaluate(profile, minimumAge16, minimumAge18);
        AttractionCompatibility second = this.Evaluate(profile, minimumAge18, minimumAge16);

        Assert.Equal(
            18,
            first.Reasons.Single(
                reason => reason.Code == AttractionCompatibilityReasonCode.HeightRequirementMet)
                .MinimumCompanionAge);
        Assert.Equal(
            first.Reasons.Select(static reason => reason.MinimumCompanionAge),
            second.Reasons.Select(static reason => reason.MinimumCompanionAge));
    }

    [Fact]
    public void Evaluate_EqualAgeThresholds_ShouldUseTheStrictestCompanionAgeDeterministically()
    {
        ParkFitMemberProfile profile = new ParkFitMemberProfile(
            ageRange: new ParkFitAgeRange(10, 10),
            canBeAccompanied: true,
            availableCompanionAgeRange: new ParkFitAgeRange(18, 70));
        AttractionAccessCondition minimumAge16 = BuildAccompaniedAgeCondition(10, 16);
        AttractionAccessCondition minimumAge18 = BuildAccompaniedAgeCondition(10, 18);

        AttractionCompatibility first = this.Evaluate(profile, minimumAge16, minimumAge18);
        AttractionCompatibility second = this.Evaluate(profile, minimumAge18, minimumAge16);

        Assert.Equal(
            18,
            first.Reasons.Single(
                reason => reason.Code == AttractionCompatibilityReasonCode.AgeRequirementMet)
                .MinimumCompanionAge);
        Assert.Equal(
            first.Reasons.Select(static reason => reason.MinimumCompanionAge),
            second.Reasons.Select(static reason => reason.MinimumCompanionAge));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(301)]
    public void MemberProfile_WhenHeightIsOutsideSupportedRange_ShouldRejectIt(int heightCentimeters)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ParkFitMemberProfile(heightCentimeters: heightCentimeters));
    }

    [Fact]
    public void MemberProfile_WhenCompanionAgeIsProvidedWithoutAvailability_ShouldRejectIt()
    {
        Assert.Throws<ArgumentException>(
            () => new ParkFitMemberProfile(
                canBeAccompanied: null,
                availableCompanionAgeRange: new ParkFitAgeRange(18, 70)));
    }

    [Theory]
    [InlineData(-1, 10)]
    [InlineData(20, 10)]
    [InlineData(0, 131)]
    public void AgeRange_WhenBoundsAreInvalid_ShouldRejectThem(int minimumYears, int maximumYears)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ParkFitAgeRange(minimumYears, maximumYears));
    }

    [Fact]
    public void Evaluate_WhenTimestampIsNotUtc_ShouldRejectIt()
    {
        Assert.Throws<ArgumentException>(() => this.evaluator.Evaluate(
            new ParkFitMemberProfile(),
            Array.Empty<AttractionAccessCondition>(),
            EvaluationDate,
            DateTime.SpecifyKind(EvaluationTimestamp, DateTimeKind.Local),
            MaximumEvidenceAge));
    }

    private AttractionCompatibility Evaluate(
        ParkFitMemberProfile profile,
        params AttractionAccessCondition[] conditions)
    {
        return this.evaluator.Evaluate(
            profile,
            conditions,
            EvaluationDate,
            EvaluationTimestamp,
            MaximumEvidenceAge);
    }

    private static string SerializeReason(AttractionCompatibilityReason reason)
    {
        return string.Join(
            '|',
            reason.Code,
            reason.ConditionType,
            reason.RequiredValue,
            reason.MinimumCompanionAge,
            reason.Unit,
            reason.Scope,
            reason.ScopeDetail,
            string.Join(',', reason.EvidenceIssues),
            string.Join(',', reason.SemanticIssues));
    }

    private static AttractionAccessCondition BuildHeightCondition(
        AttractionAccessConditionType type,
        double value)
    {
        return BuildCondition(type, value, AttractionAccessConditionUnit.Centimeter);
    }

    private static AttractionAccessCondition BuildAgeCondition(
        AttractionAccessConditionType type,
        double value)
    {
        return BuildCondition(type, value, AttractionAccessConditionUnit.Year);
    }

    private static AttractionAccessCondition BuildAccompaniedHeightCondition(
        double value,
        int? minimumCompanionAge = null)
    {
        AttractionAccessCondition condition = BuildHeightCondition(
            AttractionAccessConditionType.MinHeightAccompanied,
            value);
        condition.RequiresAccompaniment = true;
        condition.MinimumCompanionAge = minimumCompanionAge;
        return condition;
    }

    private static AttractionAccessCondition BuildAccompaniedAgeCondition(
        double value,
        int? minimumCompanionAge = null)
    {
        AttractionAccessCondition condition = BuildAgeCondition(
            AttractionAccessConditionType.MinAgeAccompanied,
            value);
        condition.RequiresAccompaniment = true;
        condition.MinimumCompanionAge = minimumCompanionAge;
        return condition;
    }

    private static AttractionAccessCondition BuildCondition(
        AttractionAccessConditionType type,
        double? value,
        AttractionAccessConditionUnit? unit)
    {
        return new AttractionAccessCondition
        {
            Type = type,
            Value = value,
            Unit = unit,
            SourceKind = AttractionAccessConditionSourceKind.Official,
            SourceUrl = $"https://example.test/access/{type}",
            CollectedAtUtc = EvaluationTimestamp.AddDays(-30),
            VerifiedAtUtc = EvaluationTimestamp.AddDays(-15),
            SourceLanguageCode = "fr",
            SourceSummary = new List<LocalizedText>
            {
                new LocalizedText("fr", "Règle officielle de test."),
            },
            SourceConfidence = AttractionAccessConditionConfidence.High,
            Scope = AttractionAccessConditionScope.Attraction,
        };
    }
}
