using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Trips;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Trips;

public sealed class TripProgramCoherenceEvaluatorTests
{
    private static readonly DateTime NowUtc = new DateTime(2026, 9, 18, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Evaluate_WhenOfficialFactsConflictWithDay_ReturnsExplicitIssuesWithoutChangingFacts()
    {
        TripProgramDayFact day = new TripProgramDayFact(
            "day-1",
            new DateOnly(2026, 10, 4),
            "park-1",
            TripParkCandidateState.Proposed,
            true,
            ParkStatus.Operating,
            false,
            NowUtc.AddDays(-100),
            NowUtc.AddDays(-120));

        IReadOnlyCollection<TripProgramCoherenceIssue> issues = new TripProgramCoherenceEvaluator().Evaluate(
            TripDateProposal.Fixed(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 3)),
            new[] { day },
            Array.Empty<TripProgramAttractionFact>(),
            NowUtc);

        Assert.Contains(issues, issue => issue.Code == TripProgramCoherenceCode.DateOutsideProposal);
        Assert.Contains(issues, issue => issue.Code == TripProgramCoherenceCode.CandidateNotSelected);
        Assert.Contains(issues, issue => issue.Code == TripProgramCoherenceCode.OpeningHoursClosed);
        Assert.Contains(issues, issue => issue.Code == TripProgramCoherenceCode.OpeningHoursStale);
        Assert.Contains(issues, issue => issue.Code == TripProgramCoherenceCode.OpeningHoursVerifiedAfterPlanning);
        Assert.Equal(TripParkCandidateState.Proposed, day.CandidateState);
    }

    [Fact]
    public void Evaluate_WhenTwoParksShareDate_ReturnsSingleCollisionIssue()
    {
        DateOnly date = new DateOnly(2026, 10, 2);
        TripProgramDayFact[] days =
        {
            CreateValidDay("day-1", date, "park-1"),
            CreateValidDay("day-2", date, "park-2"),
        };

        IReadOnlyCollection<TripProgramCoherenceIssue> issues = new TripProgramCoherenceEvaluator().Evaluate(
            TripDateProposal.Fixed(date),
            days,
            Array.Empty<TripProgramAttractionFact>(),
            NowUtc);

        Assert.Single(issues, issue => issue.Code == TripProgramCoherenceCode.MultipleParksSameDate);
    }

    [Fact]
    public void Evaluate_WhenSelectedAttractionClosedAndConstraintNewer_ReturnsBothIssues()
    {
        TripProgramAttractionFact attraction = new TripProgramAttractionFact(
            "item-1",
            "park-1",
            TripItemDecisionStatus.Retained,
            true,
            ParkItemStatusNormalizer.TemporarilyClosed,
            1,
            NowUtc.AddMinutes(-1),
            NowUtc.AddDays(-1));

        IReadOnlyCollection<TripProgramCoherenceIssue> issues = new TripProgramCoherenceEvaluator().Evaluate(
            TripDateProposal.None(),
            Array.Empty<TripProgramDayFact>(),
            new[] { attraction },
            NowUtc);

        Assert.Contains(issues, issue => issue.Code == TripProgramCoherenceCode.AttractionClosed);
        Assert.Contains(issues, issue => issue.Code == TripProgramCoherenceCode.NewMemberConstraint);
    }

    [Fact]
    public void Evaluate_WhenDecisionExcluded_IgnoresAttractionChanges()
    {
        TripProgramAttractionFact attraction = new TripProgramAttractionFact(
            "item-1",
            "park-1",
            TripItemDecisionStatus.Excluded,
            false,
            ParkItemStatusNormalizer.ClosedDefinitively,
            1,
            NowUtc,
            NowUtc.AddDays(-1));

        IReadOnlyCollection<TripProgramCoherenceIssue> issues = new TripProgramCoherenceEvaluator().Evaluate(
            TripDateProposal.None(),
            Array.Empty<TripProgramDayFact>(),
            new[] { attraction },
            NowUtc);

        Assert.Empty(issues);
    }

    private static TripProgramDayFact CreateValidDay(string id, DateOnly date, string parkId)
    {
        return new TripProgramDayFact(
            id,
            date,
            parkId,
            TripParkCandidateState.Selected,
            true,
            ParkStatus.Operating,
            true,
            NowUtc,
            NowUtc);
    }
}
