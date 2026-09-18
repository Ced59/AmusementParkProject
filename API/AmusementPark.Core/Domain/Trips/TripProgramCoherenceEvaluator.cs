using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Core.Domain.Trips;

public sealed class TripProgramCoherenceEvaluator
{
    public static readonly TimeSpan OpeningHoursFreshness = TimeSpan.FromDays(90);

    public IReadOnlyCollection<TripProgramCoherenceIssue> Evaluate(
        TripDateProposal dateProposal,
        IReadOnlyCollection<TripProgramDayFact> days,
        IReadOnlyCollection<TripProgramAttractionFact> attractions,
        DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(dateProposal);
        ArgumentNullException.ThrowIfNull(days);
        ArgumentNullException.ThrowIfNull(attractions);
        if (nowUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The coherence evaluation time must be expressed in UTC.", nameof(nowUtc));
        }

        List<TripProgramCoherenceIssue> issues = new List<TripProgramCoherenceIssue>();
        foreach (IGrouping<DateOnly, TripProgramDayFact> group in days.GroupBy(static day => day.LocalDate))
        {
            if (group.Select(static day => day.ParkId).Distinct(StringComparer.Ordinal).Count() > 1)
            {
                issues.Add(new TripProgramCoherenceIssue(
                    TripProgramCoherenceCode.MultipleParksSameDate,
                    TripProgramCoherenceSeverity.Critical,
                    group.Key,
                    null,
                    null));
            }
        }

        foreach (TripProgramDayFact day in days)
        {
            EvaluateDay(dateProposal, day, nowUtc, issues);
        }

        foreach (TripProgramAttractionFact attraction in attractions)
        {
            EvaluateAttraction(attraction, issues);
        }

        return issues
            .OrderByDescending(static issue => issue.Severity)
            .ThenBy(static issue => issue.LocalDate)
            .ThenBy(static issue => issue.Code)
            .ThenBy(static issue => issue.ParkId, StringComparer.Ordinal)
            .ThenBy(static issue => issue.ParkItemId, StringComparer.Ordinal)
            .ToArray();
    }

    private static void EvaluateDay(
        TripDateProposal dateProposal,
        TripProgramDayFact day,
        DateTime nowUtc,
        ICollection<TripProgramCoherenceIssue> issues)
    {
        if (!Contains(dateProposal, day.LocalDate))
        {
            AddDayIssue(issues, TripProgramCoherenceCode.DateOutsideProposal, TripProgramCoherenceSeverity.Critical, day);
        }

        if (!day.CandidateState.HasValue)
        {
            AddDayIssue(issues, TripProgramCoherenceCode.CandidateMissing, TripProgramCoherenceSeverity.Critical, day);
        }
        else if (day.CandidateState.Value != TripParkCandidateState.Selected)
        {
            AddDayIssue(issues, TripProgramCoherenceCode.CandidateNotSelected, TripProgramCoherenceSeverity.Attention, day);
        }

        if (!day.IsParkAvailable || !day.ParkStatus.HasValue)
        {
            AddDayIssue(issues, TripProgramCoherenceCode.ParkUnavailable, TripProgramCoherenceSeverity.Critical, day);
        }
        else if (!day.ParkStatus.Value.IsOpenToVisitors())
        {
            AddDayIssue(issues, TripProgramCoherenceCode.ParkNotOperating, TripProgramCoherenceSeverity.Critical, day);
        }

        if (!day.IsOpenOnDate.HasValue)
        {
            AddDayIssue(issues, TripProgramCoherenceCode.OpeningHoursUnknown, TripProgramCoherenceSeverity.Attention, day);
        }
        else if (!day.IsOpenOnDate.Value)
        {
            AddDayIssue(issues, TripProgramCoherenceCode.OpeningHoursClosed, TripProgramCoherenceSeverity.Critical, day);
        }

        if (day.OpeningHoursVerifiedAtUtc.HasValue)
        {
            if (nowUtc - day.OpeningHoursVerifiedAtUtc.Value > OpeningHoursFreshness)
            {
                AddDayIssue(issues, TripProgramCoherenceCode.OpeningHoursStale, TripProgramCoherenceSeverity.Attention, day);
            }

            if (day.OpeningHoursVerifiedAtUtc.Value > day.DayPlanUpdatedAtUtc)
            {
                AddDayIssue(
                    issues,
                    TripProgramCoherenceCode.OpeningHoursVerifiedAfterPlanning,
                    TripProgramCoherenceSeverity.Information,
                    day);
            }
        }
    }

    private static void EvaluateAttraction(
        TripProgramAttractionFact attraction,
        ICollection<TripProgramCoherenceIssue> issues)
    {
        if (attraction.DecisionStatus is TripItemDecisionStatus.Review or TripItemDecisionStatus.Excluded)
        {
            return;
        }

        if (!attraction.IsAvailable)
        {
            AddAttractionIssue(
                issues,
                TripProgramCoherenceCode.AttractionUnavailable,
                TripProgramCoherenceSeverity.Critical,
                attraction);
        }

        if (ParkItemStatusNormalizer.IsClosedForPublicBrowsing(attraction.OfficialStatus))
        {
            AddAttractionIssue(
                issues,
                TripProgramCoherenceCode.AttractionClosed,
                TripProgramCoherenceSeverity.Critical,
                attraction);
        }

        if (attraction.NotForMeCount > 0
            && attraction.LatestNotForMeAtUtc > attraction.DecisionUpdatedAtUtc)
        {
            AddAttractionIssue(
                issues,
                TripProgramCoherenceCode.NewMemberConstraint,
                TripProgramCoherenceSeverity.Attention,
                attraction);
        }
    }

    private static bool Contains(TripDateProposal proposal, DateOnly date)
    {
        return proposal.Kind switch
        {
            TripDateProposalKind.None => false,
            TripDateProposalKind.Fixed or TripDateProposalKind.Range =>
                date >= proposal.StartDate!.Value && date <= proposal.EndDate!.Value,
            TripDateProposalKind.Candidates => proposal.CandidateDates.Contains(date),
            _ => false,
        };
    }

    private static void AddDayIssue(
        ICollection<TripProgramCoherenceIssue> issues,
        TripProgramCoherenceCode code,
        TripProgramCoherenceSeverity severity,
        TripProgramDayFact day)
    {
        issues.Add(new TripProgramCoherenceIssue(code, severity, day.LocalDate, day.ParkId, null));
    }

    private static void AddAttractionIssue(
        ICollection<TripProgramCoherenceIssue> issues,
        TripProgramCoherenceCode code,
        TripProgramCoherenceSeverity severity,
        TripProgramAttractionFact attraction)
    {
        issues.Add(new TripProgramCoherenceIssue(code, severity, null, attraction.ParkId, attraction.ParkItemId));
    }
}
