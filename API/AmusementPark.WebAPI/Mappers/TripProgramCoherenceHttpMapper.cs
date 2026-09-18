using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.WebAPI.Contracts.Trips;

namespace AmusementPark.WebAPI.Mappers;

public static class TripProgramCoherenceHttpMapper
{
    public static TripProgramCoherenceDto ToHttp(this TripProgramCoherenceResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new TripProgramCoherenceDto
        {
            TripPlanId = result.TripPlanId,
            TripTitle = result.TripTitle,
            PlanVersion = result.PlanVersion,
            EvaluatedAtUtc = result.EvaluatedAtUtc,
            CriticalCount = result.CriticalCount,
            AttentionCount = result.AttentionCount,
            InformationCount = result.InformationCount,
            Days = result.Days.Select(static day => new TripProgramDayEvidenceDto
            {
                LocalDate = day.LocalDate,
                ParkId = day.ParkId,
                ParkName = day.ParkName,
                IsParkAvailable = day.IsParkAvailable,
                ParkStatus = day.ParkStatus,
                OpeningState = day.OpeningState.ToString(),
                OpeningHoursSourceUrl = day.OpeningHoursSourceUrl,
                OpeningHoursVerifiedAtUtc = day.OpeningHoursVerifiedAtUtc,
                DayPlanUpdatedAtUtc = day.DayPlanUpdatedAtUtc,
            }).ToArray(),
            TravelSegments = result.TravelSegments.Select(static segment => new TripProgramTravelSegmentDto
            {
                FromDate = segment.FromDate,
                FromParkId = segment.FromParkId,
                FromParkName = segment.FromParkName,
                ToDate = segment.ToDate,
                ToParkId = segment.ToParkId,
                ToParkName = segment.ToParkName,
                DistanceKilometers = segment.DistanceKilometers,
                EstimatedTravelDurationMinutes = segment.EstimatedTravelDurationMinutes,
                EstimationMethod = segment.EstimationMethod,
            }).ToArray(),
            Issues = result.Issues.Select(static issue => new TripProgramCoherenceIssueDto
            {
                Code = issue.Code.ToString(),
                Severity = issue.Severity.ToString(),
                LocalDate = issue.LocalDate,
                ParkId = issue.ParkId,
                ParkName = issue.ParkName,
                ParkItemId = issue.ParkItemId,
                ParkItemName = issue.ParkItemName,
                OfficialStatus = issue.OfficialStatus,
                OfficialSourceUrl = issue.OfficialSourceUrl,
                OfficialVerifiedAtUtc = issue.OfficialVerifiedAtUtc,
            }).ToArray(),
        };
    }
}
