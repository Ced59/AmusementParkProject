using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.Core.Geo;

namespace AmusementPark.Application.Features.Trips.Services;

public sealed class TripProgramEvidenceBuilder
{
    private const string TravelEstimationMethod = "GeodesicEstimate";
    private readonly ParkOpeningHoursCalendarBuilder calendarBuilder;

    public TripProgramEvidenceBuilder(ParkOpeningHoursCalendarBuilder calendarBuilder)
    {
        this.calendarBuilder = calendarBuilder ?? throw new ArgumentNullException(nameof(calendarBuilder));
    }

    public TripProgramEvidenceProjection Build(
        TripProgramResult program,
        IReadOnlyDictionary<string, Park> parksById,
        IReadOnlyDictionary<string, ParkOpeningHoursSchedule> schedulesByParkId)
    {
        ArgumentNullException.ThrowIfNull(program);
        ArgumentNullException.ThrowIfNull(parksById);
        ArgumentNullException.ThrowIfNull(schedulesByParkId);
        Dictionary<string, TripParkCandidateResult> candidatesById = program.Candidates
            .ToDictionary(static candidate => candidate.CandidateId, StringComparer.Ordinal);
        List<TripProgramDayEvidenceResult> days = new List<TripProgramDayEvidenceResult>(program.Days.Count);
        List<TripProgramDayFact> facts = new List<TripProgramDayFact>(program.Days.Count);
        foreach (TripDayPlanResult day in program.Days
            .OrderBy(static day => day.LocalDate)
            .ThenBy(static day => day.ParkId, StringComparer.Ordinal))
        {
            TripProgramDayEvidenceResult evidence = this.BuildDayEvidence(day, parksById, schedulesByParkId);
            TripParkCandidateState? candidateState = candidatesById.TryGetValue(
                day.ParkCandidateId,
                out TripParkCandidateResult? candidate)
                ? candidate.State
                : null;
            ParkStatus? parkStatus = parksById.TryGetValue(day.ParkId, out Park? park)
                ? park.Status
                : null;
            days.Add(evidence);
            facts.Add(new TripProgramDayFact(
                day.DayPlanId,
                day.LocalDate,
                day.ParkId,
                candidateState,
                evidence.IsParkAvailable,
                parkStatus,
                evidence.OpeningState switch
                {
                    TripProgramOpeningState.Open => true,
                    TripProgramOpeningState.Closed => false,
                    _ => null,
                },
                evidence.OpeningHoursVerifiedAtUtc,
                day.UpdatedAtUtc));
        }

        return new TripProgramEvidenceProjection(
            days,
            facts,
            BuildTravelSegments(program.Days, parksById));
    }

    private TripProgramDayEvidenceResult BuildDayEvidence(
        TripDayPlanResult day,
        IReadOnlyDictionary<string, Park> parksById,
        IReadOnlyDictionary<string, ParkOpeningHoursSchedule> schedulesByParkId)
    {
        parksById.TryGetValue(day.ParkId, out Park? park);
        schedulesByParkId.TryGetValue(day.ParkId, out ParkOpeningHoursSchedule? schedule);
        ParkOpeningHoursDay? openingDay = schedule is null
            ? null
            : this.calendarBuilder.BuildCalendar(schedule, day.LocalDate, day.LocalDate).Days.SingleOrDefault();
        TripProgramOpeningState openingState = openingDay is null
            ? TripProgramOpeningState.Unknown
            : openingDay.IsClosed
                ? TripProgramOpeningState.Closed
                : TripProgramOpeningState.Open;
        bool isParkAvailable = park?.IsPubliclyDiscoverable() == true;
        return new TripProgramDayEvidenceResult(
            day.LocalDate,
            day.ParkId,
            isParkAvailable ? park!.Name!.Trim() : null,
            isParkAvailable,
            isParkAvailable ? park!.Status.ToString() : null,
            openingState,
            isParkAvailable ? NormalizeOptional(schedule?.SourceUrl) : null,
            isParkAvailable ? schedule?.LastVerifiedAtUtc : null,
            day.UpdatedAtUtc);
    }

    private static IReadOnlyCollection<TripProgramTravelSegmentResult> BuildTravelSegments(
        IReadOnlyCollection<TripDayPlanResult> days,
        IReadOnlyDictionary<string, Park> parksById)
    {
        TripDayPlanResult[] orderedDays = days
            .OrderBy(static day => day.LocalDate)
            .ThenBy(static day => day.ParkId, StringComparer.Ordinal)
            .ToArray();
        List<TripProgramTravelSegmentResult> segments = new List<TripProgramTravelSegmentResult>();
        for (int index = 1; index < orderedDays.Length; index++)
        {
            TripDayPlanResult from = orderedDays[index - 1];
            TripDayPlanResult to = orderedDays[index];
            if (string.Equals(from.ParkId, to.ParkId, StringComparison.Ordinal)
                || !parksById.TryGetValue(from.ParkId, out Park? fromPark)
                || !parksById.TryGetValue(to.ParkId, out Park? toPark)
                || fromPark.Position is null
                || toPark.Position is null)
            {
                continue;
            }

            double distance = Math.Round(
                GeoDistanceCalculator.CalculateKilometers(fromPark.Position, toPark.Position),
                1,
                MidpointRounding.AwayFromZero);
            segments.Add(new TripProgramTravelSegmentResult(
                from.LocalDate,
                from.ParkId,
                fromPark.IsPubliclyDiscoverable() ? fromPark.Name?.Trim() : null,
                to.LocalDate,
                to.ParkId,
                toPark.IsPubliclyDiscoverable() ? toPark.Name?.Trim() : null,
                distance,
                GeoDistanceCalculator.EstimateTravelDurationMinutes(distance),
                TravelEstimationMethod));
        }

        return segments;
    }

    private static string? NormalizeOptional(string? value)
    {
        string? normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
