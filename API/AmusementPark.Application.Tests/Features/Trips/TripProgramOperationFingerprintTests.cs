using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Services;
using AmusementPark.Core.Domain.Trips;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Trips;

public sealed class TripProgramOperationFingerprintTests
{
    [Fact]
    public void BuildDayRequestHash_ShouldKeepFieldBoundariesUnambiguous()
    {
        DateOnly date = new(2027, 7, 8);
        TripDayBlockId blockId = TripDayBlockId.New();
        TripParkCandidateId candidateId = TripParkCandidateId.New();
        TripDayPlanInput first = CreateInput(candidateId, blockId, "a|b", "c");
        TripDayPlanInput second = CreateInput(candidateId, blockId, "a", "b|c");

        string firstHash = TripProgramOperationFingerprint.BuildDayRequestHash(date, first);
        string secondHash = TripProgramOperationFingerprint.BuildDayRequestHash(date, second);

        Assert.NotEqual(firstHash, secondHash);
    }

    private static TripDayPlanInput CreateInput(
        TripParkCandidateId candidateId,
        TripDayBlockId blockId,
        string title,
        string details)
    {
        return new TripDayPlanInput(
            candidateId.Value,
            null,
            null,
            new[]
            {
                new TripDayBlockInput(
                    blockId.Value,
                    TripDayBlockType.Note,
                    title,
                    details,
                    null,
                    TripDayBlock.SortPositionStep),
            });
    }
}
