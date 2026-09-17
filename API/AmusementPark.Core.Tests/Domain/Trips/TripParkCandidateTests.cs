using AmusementPark.Core.Domain.Trips;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Trips;

public sealed class TripParkCandidateTests
{
    private static readonly DateTime CreatedAtUtc = new(2027, 2, 3, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ShouldNormalizeDatesAndCollectiveNote()
    {
        TripParkCandidate candidate = CreateCandidate(
            new[] { new DateOnly(2027, 7, 9), new DateOnly(2027, 7, 8), new DateOnly(2027, 7, 8) },
            "  Parc prioritaire  ");

        Assert.Equal(
            new[] { new DateOnly(2027, 7, 8), new DateOnly(2027, 7, 9) },
            candidate.CandidateDates);
        Assert.Equal("Parc prioritaire", candidate.CollectiveNote);
        Assert.Equal(TripParkCandidateState.Proposed, candidate.State);
        Assert.Equal(1, candidate.Version);
    }

    [Fact]
    public void FineMutations_ShouldIncrementVersionOnlyForChangedValues()
    {
        TripParkCandidate candidate = CreateCandidate(Array.Empty<DateOnly>(), null);

        candidate.ChangeState(TripParkCandidateState.Proposed, CreatedAtUtc.AddMinutes(1));
        Assert.Equal(1, candidate.Version);

        candidate.ChangeState(TripParkCandidateState.Shortlisted, CreatedAtUtc.AddMinutes(2));
        Assert.Equal(2, candidate.Version);
        Assert.Equal(TripParkCandidateState.Shortlisted, candidate.State);

        candidate.MoveTo(2048, CreatedAtUtc.AddMinutes(3));
        Assert.Equal(3, candidate.Version);
        Assert.Equal(2048, candidate.SortPosition);
    }

    private static TripParkCandidate CreateCandidate(
        IReadOnlyCollection<DateOnly> dates,
        string? note)
    {
        return TripParkCandidate.Create(
            TripParkCandidateId.New(),
            TripPlanId.New(),
            "park-1",
            dates,
            TripParkCandidateSource.Manual,
            note,
            null,
            TripMemberId.New(),
            1024,
            CreatedAtUtc);
    }
}
