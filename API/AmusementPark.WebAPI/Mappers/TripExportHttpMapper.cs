using System.Globalization;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.WebAPI.Contracts.Trips;

namespace AmusementPark.WebAPI.Mappers;

public static class TripExportHttpMapper
{
    private const string DateFormat = "yyyy-MM-dd";
    private const string TimeFormat = "HH:mm";

    public static TripExportDto ToHttp(this TripExportResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new TripExportDto
        {
            SchemaVersion = result.SchemaVersion,
            Title = result.Title,
            DateProposal = new TripDateProposalDto
            {
                Kind = result.DateProposal.Kind.ToString(),
                StartDate = Format(result.DateProposal.StartDate),
                EndDate = Format(result.DateProposal.EndDate),
                CandidateDates = result.DateProposal.CandidateDates
                    .Select(static date => Format(date)!)
                    .ToArray(),
            },
            DestinationTimeZoneId = result.DestinationTimeZoneId,
            Status = result.Status.ToString(),
            GeneratedAtUtc = result.GeneratedAtUtc,
            CandidateParks = result.CandidateParks.Select(ToHttp).ToArray(),
            Days = result.Days.Select(ToHttp).ToArray(),
            CollectiveDecisions = result.CollectiveDecisions.Select(ToHttp).ToArray(),
        };
    }

    private static TripExportCandidateDto ToHttp(TripExportCandidateResult result)
    {
        return new TripExportCandidateDto
        {
            ParkName = result.ParkName,
            IsParkAvailable = result.IsParkAvailable,
            CandidateDates = result.CandidateDates
                .Select(static date => Format(date)!)
                .ToArray(),
            State = result.State.ToString(),
            CollectiveNote = result.CollectiveNote,
        };
    }

    private static TripExportDayDto ToHttp(TripExportDayResult result)
    {
        return new TripExportDayDto
        {
            LocalDate = Format(result.LocalDate)!,
            ParkName = result.ParkName,
            IsParkAvailable = result.IsParkAvailable,
            DesiredArrivalTime = Format(result.DesiredArrivalTime),
            GroupNote = result.GroupNote,
            Blocks = result.Blocks.Select(ToHttp).ToArray(),
        };
    }

    private static TripExportDayBlockDto ToHttp(TripExportDayBlockResult result)
    {
        return new TripExportDayBlockDto
        {
            Type = result.Type.ToString(),
            Title = result.Title,
            Details = result.Details,
            LocalTime = Format(result.LocalTime),
        };
    }

    private static TripExportDecisionDto ToHttp(TripExportDecisionResult result)
    {
        return new TripExportDecisionDto
        {
            ParkName = result.ParkName,
            ParkItemName = result.ParkItemName,
            IsParkItemAvailable = result.IsParkItemAvailable,
            Status = result.Status.ToString(),
            Reason = result.Reason,
            DecidedAtUtc = result.DecidedAtUtc,
        };
    }

    private static string? Format(DateOnly? value)
    {
        return value?.ToString(DateFormat, CultureInfo.InvariantCulture);
    }

    private static string? Format(TimeOnly? value)
    {
        return value?.ToString(TimeFormat, CultureInfo.InvariantCulture);
    }
}
