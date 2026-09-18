using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;
using AmusementPark.Core.Domain.Parks;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Trips;

public sealed class TripProgramEvidenceBuilderTests
{
    [Fact]
    public void Build_WhenParkIsUnavailable_ShouldHideEveryOfficialMetadataField()
    {
        DateTime nowUtc = new DateTime(2027, 4, 1, 10, 0, 0, DateTimeKind.Utc);
        DateOnly visitDate = new DateOnly(2027, 5, 3);
        Park hiddenPark = new Park
        {
            Id = "park-hidden",
            Name = "Parc interne",
            IsVisible = false,
            Status = ParkStatus.Operating,
        };
        ParkOpeningHoursSchedule schedule = new ParkOpeningHoursSchedule
        {
            ParkId = hiddenPark.Id,
            TimeZoneId = "Europe/Paris",
            SourceUrl = "https://internal.example/official-hours",
            LastVerifiedAtUtc = nowUtc,
        };
        TripDayPlanResult day = new TripDayPlanResult(
            "day-1",
            visitDate,
            "candidate-1",
            hiddenPark.Id,
            null,
            false,
            null,
            null,
            Array.Empty<TripDayBlockResult>(),
            1,
            nowUtc,
            nowUtc);
        TripProgramResult program = new TripProgramResult(
            Array.Empty<TripParkCandidateResult>(),
            new[] { day });

        TripProgramEvidenceProjection projection = new TripProgramEvidenceBuilder(
            new ParkOpeningHoursCalendarBuilder()).Build(
                program,
                new Dictionary<string, Park>(StringComparer.Ordinal) { [hiddenPark.Id] = hiddenPark },
                new Dictionary<string, ParkOpeningHoursSchedule>(StringComparer.Ordinal)
                {
                    [hiddenPark.Id] = schedule,
                });

        TripProgramDayEvidenceResult evidence = Assert.Single(projection.Days);
        Assert.False(evidence.IsParkAvailable);
        Assert.Null(evidence.ParkName);
        Assert.Null(evidence.ParkStatus);
        Assert.Null(evidence.OpeningHoursSourceUrl);
        Assert.Null(evidence.OpeningHoursVerifiedAtUtc);
    }
}
