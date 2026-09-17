using AmusementPark.Core.Domain.Trips;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Trips;

public sealed class TripProgramRulesTests
{
    private static readonly DateTime CreatedAtUtc = new(2027, 2, 3, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ValidateCandidateDates_ShouldRejectADateOutsideTheProposal()
    {
        TripDateProposal proposal = TripDateProposal.Range(
            new DateOnly(2027, 7, 8),
            new DateOnly(2027, 7, 10));

        TripPlanValidationException exception = Assert.Throws<TripPlanValidationException>(() =>
            TripProgramRules.ValidateCandidateDates(
                proposal,
                new[] { new DateOnly(2027, 7, 11) }));

        Assert.Equal(TripPlanErrorCodes.InvalidCandidate, exception.Code);
    }

    [Fact]
    public void ValidateCandidateStateAgainstDays_ShouldKeepAnAssignedParkSelected()
    {
        DateTime nowUtc = new(2027, 2, 3, 10, 0, 0, DateTimeKind.Utc);
        TripPlanId tripId = TripPlanId.New();
        TripParkCandidate candidate = TripParkCandidate.Create(
            TripParkCandidateId.New(),
            tripId,
            "park-1",
            Array.Empty<DateOnly>(),
            TripParkCandidateSource.Manual,
            null,
            null,
            TripMemberId.New(),
            TripParkCandidate.SortPositionStep,
            nowUtc);
        candidate.ChangeState(TripParkCandidateState.Selected, nowUtc);
        TripDayPlan day = TripDayPlan.Create(
            TripDayPlanId.New(),
            tripId,
            new DateOnly(2027, 7, 8),
            candidate.Id,
            candidate.ParkId,
            null,
            null,
            Array.Empty<TripDayBlock>(),
            nowUtc);

        TripPlanValidationException exception = Assert.Throws<TripPlanValidationException>(() =>
            TripProgramRules.ValidateCandidateStateAgainstDays(
                candidate,
                TripParkCandidateState.Rejected,
                new[] { day }));

        Assert.Equal(TripPlanErrorCodes.InvalidCandidate, exception.Code);
    }

    [Fact]
    public void ValidateDayDate_ShouldRequireAFixedTripPeriod()
    {
        TripPlanValidationException exception = Assert.Throws<TripPlanValidationException>(() =>
            TripProgramRules.ValidateDayDate(
                TripDateProposal.Range(new DateOnly(2027, 7, 8), new DateOnly(2027, 7, 10)),
                new DateOnly(2027, 7, 9)));

        Assert.Equal(TripPlanErrorCodes.InvalidDayPlan, exception.Code);
    }

    [Fact]
    public void ValidateDayCandidate_ShouldRequireASelectedCandidateProposedForThatDate()
    {
        TripParkCandidate candidate = TripParkCandidate.Create(
            TripParkCandidateId.New(),
            TripPlanId.New(),
            "park-1",
            new[] { new DateOnly(2027, 7, 8) },
            TripParkCandidateSource.Manual,
            null,
            null,
            TripMemberId.New(),
            1024,
            CreatedAtUtc);
        candidate.ChangeState(TripParkCandidateState.Selected, CreatedAtUtc.AddMinutes(1));

        TripPlanValidationException exception = Assert.Throws<TripPlanValidationException>(() =>
            TripProgramRules.ValidateDayCandidate(candidate, new DateOnly(2027, 7, 9)));

        Assert.Equal(TripPlanErrorCodes.InvalidDayPlan, exception.Code);
    }
}
