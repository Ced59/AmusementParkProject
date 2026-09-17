using AmusementPark.Core.Domain.Trips;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Trips;

public sealed class TripDateProposalTests
{
    [Fact]
    public void Range_ShouldRejectAnInvertedInterval()
    {
        TripPlanValidationException exception = Assert.Throws<TripPlanValidationException>(() =>
            TripDateProposal.Range(new DateOnly(2027, 6, 8), new DateOnly(2027, 6, 1)));

        Assert.Equal(TripPlanErrorCodes.InvalidDateProposal, exception.Code);
    }

    [Fact]
    public void Range_ShouldAcceptExactlyTheMaximumInclusiveDayCount()
    {
        DateOnly startDate = new(2027, 1, 1);

        TripDateProposal proposal = TripDateProposal.Range(
            startDate,
            startDate.AddDays(TripDateProposal.MaximumRangeDays - 1));

        Assert.Equal(startDate, proposal.StartDate);
        Assert.Equal(startDate.AddDays(TripDateProposal.MaximumRangeDays - 1), proposal.EndDate);
    }

    [Fact]
    public void Range_ShouldRejectOneMoreThanTheMaximumInclusiveDayCount()
    {
        DateOnly startDate = new(2027, 1, 1);

        TripPlanValidationException exception = Assert.Throws<TripPlanValidationException>(() =>
            TripDateProposal.Range(startDate, startDate.AddDays(TripDateProposal.MaximumRangeDays)));

        Assert.Equal(TripPlanErrorCodes.InvalidDateProposal, exception.Code);
    }

    [Fact]
    public void Candidates_ShouldNormalizeDistinctChronologicalDates()
    {
        TripDateProposal proposal = TripDateProposal.Candidates(new[]
        {
            new DateOnly(2027, 7, 10),
            new DateOnly(2027, 7, 8),
            new DateOnly(2027, 7, 10),
        });

        Assert.Equal(
            new[] { new DateOnly(2027, 7, 8), new DateOnly(2027, 7, 10) },
            proposal.CandidateDates);
    }

    [Fact]
    public void Restore_ShouldRejectFieldsFromAnotherMode()
    {
        Assert.Throws<TripPlanValidationException>(() => TripDateProposal.Restore(
            TripDateProposalKind.Fixed,
            new DateOnly(2027, 7, 8),
            new DateOnly(2027, 7, 9),
            Array.Empty<DateOnly>()));
    }
}
