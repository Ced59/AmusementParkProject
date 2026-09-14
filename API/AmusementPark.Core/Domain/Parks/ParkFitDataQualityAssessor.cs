namespace AmusementPark.Core.Domain.Parks;

/// <summary>
/// Calcule un audit FIT déterministe à partir des faits canoniques du parc.
/// </summary>
public sealed class ParkFitDataQualityAssessor
{
    public const int MaximumIssueSamples = 8;

    private readonly ParkOpeningHoursAdminStatusResolver openingHoursStatusResolver =
        new ParkOpeningHoursAdminStatusResolver();
    private readonly ParkFitDataQualityItemAssessor itemAssessor =
        new ParkFitDataQualityItemAssessor();

    public ParkFitDataQualityAssessment Assess(
        Park park,
        IReadOnlyCollection<ParkItem> parkItems,
        ParkOpeningHoursScheduleSummary? openingHoursSummary,
        DateTime evaluatedAtUtc,
        TimeSpan maximumVerificationAge,
        bool suspended = false,
        DateOnly? applicabilityDate = null)
    {
        ArgumentNullException.ThrowIfNull(park);
        ArgumentNullException.ThrowIfNull(parkItems);

        if (evaluatedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The evaluation timestamp must use UTC.", nameof(evaluatedAtUtc));
        }

        if (maximumVerificationAge < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumVerificationAge));
        }

        List<ParkItem> attractions = parkItems
            .Where(static item => item.IsVisible && item.Category == ParkItemCategory.Attraction)
            .OrderBy(static item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        List<ParkFitDataQualityItemAssessment> itemAssessments = attractions
            .Select(item => this.itemAssessor.Assess(
                item,
                evaluatedAtUtc,
                maximumVerificationAge,
                applicabilityDate ?? DateOnly.FromDateTime(evaluatedAtUtc)))
            .ToList();

        int withConditionsCount = itemAssessments.Count(static item => item.ConditionCount > 0);
        int conditionCount = itemAssessments.Sum(static item => item.ConditionCount);
        int eligibleConditionCount = itemAssessments.Sum(static item => item.DecisionEligibleConditionCount);
        int eligibleAttractionCount = itemAssessments.Count(static item => item.Issues.Count == 0);
        int coveragePercent = attractions.Count == 0
            ? 0
            : (int)Math.Round(eligibleAttractionCount * 100m / attractions.Count, MidpointRounding.AwayFromZero);
        ParkOpeningHoursAdminStatus openingHoursStatus =
            this.openingHoursStatusResolver.Resolve(openingHoursSummary, evaluatedAtUtc);
        HashSet<ParkFitDataQualityIssue> issues = BuildParkIssues(
            park,
            attractions.Count,
            itemAssessments,
            eligibleAttractionCount,
            openingHoursStatus);
        ParkFitDataQualityStatus status = ResolveStatus(
            park,
            suspended,
            attractions.Count,
            eligibleAttractionCount,
            openingHoursStatus,
            issues);

        return new ParkFitDataQualityAssessment
        {
            ParkId = park.Id,
            ParkName = park.Name?.Trim() ?? string.Empty,
            Status = status,
            CoveragePercent = coveragePercent,
            VisibleAttractionCount = attractions.Count,
            AttractionWithConditionsCount = withConditionsCount,
            DecisionEligibleAttractionCount = eligibleAttractionCount,
            ConditionCount = conditionCount,
            DecisionEligibleConditionCount = eligibleConditionCount,
            IssueItemCount = itemAssessments.Count(static item => item.Issues.Count > 0),
            MissingSourceItemCount = CountItemsWithAnyIssue(
                itemAssessments,
                ParkFitDataQualityIssue.MissingAuthoritativeSource,
                ParkFitDataQualityIssue.MissingAccessibilitySource),
            MissingTimestampItemCount = CountItemsWithAnyIssue(
                itemAssessments,
                ParkFitDataQualityIssue.MissingEvidenceTimestamp),
            StaleEvidenceItemCount = CountItemsWithAnyIssue(
                itemAssessments,
                ParkFitDataQualityIssue.StaleEvidence),
            AmbiguousItemCount = CountItemsWithAnyIssue(
                itemAssessments,
                ParkFitDataQualityIssue.AmbiguousRestriction),
            LastVerifiedAtUtc = itemAssessments
                .Select(static item => item.LastVerifiedAtUtc)
                .Max(),
            Issues = issues.OrderBy(static issue => issue).ToList(),
            IssueSamples = itemAssessments
                .Where(static item => item.Issues.Count > 0)
                .Take(MaximumIssueSamples)
                .ToList(),
        };
    }

    private static HashSet<ParkFitDataQualityIssue> BuildParkIssues(
        Park park,
        int attractionCount,
        IReadOnlyCollection<ParkFitDataQualityItemAssessment> itemAssessments,
        int eligibleAttractionCount,
        ParkOpeningHoursAdminStatus openingHoursStatus)
    {
        HashSet<ParkFitDataQualityIssue> issues = new HashSet<ParkFitDataQualityIssue>();

        if (!park.IsPubliclyDiscoverable())
        {
            issues.Add(ParkFitDataQualityIssue.NotPubliclyDiscoverable);
        }

        if (!DataCompletenessScoringRules.HasValidPosition(park.Position))
        {
            issues.Add(ParkFitDataQualityIssue.MissingCoordinates);
        }

        if (!HasValidParkType(park))
        {
            issues.Add(ParkFitDataQualityIssue.MissingParkType);
        }

        if (DataCompletenessScoringRules.CountPublicLanguagesWithText(park.Descriptions) == 0)
        {
            issues.Add(ParkFitDataQualityIssue.MissingSupportedLanguageContent);
        }

        if (attractionCount == 0)
        {
            issues.Add(ParkFitDataQualityIssue.NoVisibleAttractions);
        }

        if (openingHoursStatus == ParkOpeningHoursAdminStatus.NotConfigured)
        {
            issues.Add(ParkFitDataQualityIssue.MissingOpeningCalendar);
        }
        else if (openingHoursStatus != ParkOpeningHoursAdminStatus.UpToDate)
        {
            issues.Add(ParkFitDataQualityIssue.OpeningCalendarStale);
        }

        foreach (ParkFitDataQualityIssue itemIssue in itemAssessments.SelectMany(static item => item.Issues))
        {
            issues.Add(itemIssue);
        }

        if (eligibleAttractionCount < attractionCount)
        {
            issues.Add(ParkFitDataQualityIssue.IncompleteRestrictionCoverage);
        }

        return issues;
    }

    private static ParkFitDataQualityStatus ResolveStatus(
        Park park,
        bool suspended,
        int attractionCount,
        int eligibleAttractionCount,
        ParkOpeningHoursAdminStatus openingHoursStatus,
        IReadOnlySet<ParkFitDataQualityIssue> issues)
    {
        if (suspended)
        {
            return ParkFitDataQualityStatus.Suspended;
        }

        if (!park.IsPubliclyDiscoverable())
        {
            return ParkFitDataQualityStatus.NotAssessed;
        }

        if (!DataCompletenessScoringRules.HasValidPosition(park.Position)
            || !HasValidParkType(park)
            || attractionCount == 0)
        {
            return ParkFitDataQualityStatus.Insufficient;
        }

        if (eligibleAttractionCount == attractionCount
            && openingHoursStatus == ParkOpeningHoursAdminStatus.UpToDate
            && issues.Count == 0)
        {
            return ParkFitDataQualityStatus.EligibleForFitComparison;
        }

        HashSet<ParkFitDataQualityIssue> nonFreshnessIssues = issues
            .Where(static issue => issue is not ParkFitDataQualityIssue.StaleEvidence
                and not ParkFitDataQualityIssue.OpeningCalendarStale
                and not ParkFitDataQualityIssue.IncompleteRestrictionCoverage)
            .ToHashSet();
        if (nonFreshnessIssues.Count == 0)
        {
            return ParkFitDataQualityStatus.TemporarilyStale;
        }

        return eligibleAttractionCount > 0
            ? ParkFitDataQualityStatus.EligibleForDiscoveryOnly
            : ParkFitDataQualityStatus.Insufficient;
    }

    private static int CountItemsWithAnyIssue(
        IEnumerable<ParkFitDataQualityItemAssessment> itemAssessments,
        params ParkFitDataQualityIssue[] issues)
    {
        HashSet<ParkFitDataQualityIssue> issueSet = issues.ToHashSet();
        return itemAssessments.Count(item => item.Issues.Any(issueSet.Contains));
    }

    private static bool HasValidParkType(Park park)
    {
        return park.Type.HasValue && Enum.IsDefined(park.Type.Value);
    }

}
