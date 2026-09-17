using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.WebAPI.Contracts.Trips;
using AmusementPark.WebAPI.Mappers;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Mappers;

public sealed class TripPlanHttpMapperTests
{
    [Fact]
    public void TryToApplication_ShouldParseCandidateDatesStrictly()
    {
        TripPlanWriteRequestDto request = new()
        {
            Title = "Voyage été",
            DestinationTimeZoneId = "Europe/Paris",
            DateProposal = new TripDateProposalRequestDto
            {
                Kind = "Candidates",
                CandidateDates = new[] { "2027-07-08", "2027-07-10" },
            },
        };

        bool success = request.TryToApplication(out TripPlanDetailsInput? input);

        Assert.True(success);
        Assert.NotNull(input);
        Assert.Equal(TripDateProposalKind.Candidates, input.DateProposal.Kind);
        Assert.Equal(2, input.DateProposal.CandidateDates.Count);
    }

    [Fact]
    public void TryToApplication_ShouldRejectAmbiguousDateFormats()
    {
        TripPlanWriteRequestDto request = new()
        {
            Title = "Voyage été",
            DestinationTimeZoneId = "Europe/Paris",
            DateProposal = new TripDateProposalRequestDto
            {
                Kind = "Fixed",
                StartDate = "08/07/2027",
            },
        };

        Assert.False(request.TryToApplication(out TripPlanDetailsInput? _));
    }

    [Fact]
    public void TryToApplication_ShouldRejectCandidateArraysAboveTheDomainLimit()
    {
        TripPlanWriteRequestDto request = new()
        {
            Title = "Voyage été",
            DestinationTimeZoneId = "Europe/Paris",
            DateProposal = new TripDateProposalRequestDto
            {
                Kind = "Candidates",
                CandidateDates = Enumerable.Repeat(
                    "2027-07-08",
                    TripDateProposal.MaximumCandidateDates + 1).ToArray(),
            },
        };

        Assert.False(request.TryToApplication(out TripPlanDetailsInput? _));
    }
}
