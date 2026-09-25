using System.Text.Json;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.WebAPI.Contracts.Trips;
using AmusementPark.WebAPI.Mappers;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Mappers;

public sealed class TripExportHttpMapperTests
{
    [Fact]
    public void ToHttp_ShouldUsePortableValuesWithoutTechnicalIdentifiers()
    {
        TripExportResult result = new(
            "trip-plan-export-v1",
            "Voyage test",
            new TripDateProposalResult(
                TripDateProposalKind.Fixed,
                new DateOnly(2027, 8, 20),
                new DateOnly(2027, 8, 20),
                Array.Empty<DateOnly>()),
            "Europe/Paris",
            TripPlanStatus.Draft,
            new DateTime(2027, 8, 12, 9, 30, 0, DateTimeKind.Utc),
            new[]
            {
                new TripExportCandidateResult(
                    "Phantasialand",
                    true,
                    new[] { new DateOnly(2027, 8, 20) },
                    TripParkCandidateState.Selected,
                    "Choix du groupe"),
            },
            new[]
            {
                new TripExportDayResult(
                    new DateOnly(2027, 8, 20),
                    "Phantasialand",
                    true,
                    new TimeOnly(9, 30),
                    null,
                    Array.Empty<TripExportDayBlockResult>()),
            },
            Array.Empty<TripExportDecisionResult>());

        TripExportDto dto = result.ToHttp();

        Assert.Equal("2027-08-20", dto.DateProposal.StartDate);
        Assert.Equal("09:30", Assert.Single(dto.Days).DesiredArrivalTime);
        Assert.Equal("Selected", Assert.Single(dto.CandidateParks).State);
        string json = JsonSerializer.Serialize(dto);
        Assert.DoesNotContain("tripPlanId", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("parkId", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("userId", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("member", json, StringComparison.OrdinalIgnoreCase);
    }
}
