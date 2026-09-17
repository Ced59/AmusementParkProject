using System.Globalization;
using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.WebAPI.Contracts.Trips;

namespace AmusementPark.WebAPI.Mappers;

public static class TripProgramHttpMapper
{
    private const string DateFormat = "yyyy-MM-dd";
    private const string TimeFormat = "HH:mm";

    public static bool TryToApplication(
        this AddTripParkCandidateRequestDto request,
        out TripParkCandidateInput? input)
    {
        ArgumentNullException.ThrowIfNull(request);
        input = null;
        if (!TryParseDates(request.CandidateDates, out DateOnly[] dates)
            || !TryParseEnum(request.Source, out TripParkCandidateSource source))
        {
            return false;
        }

        input = new TripParkCandidateInput(request.ParkId, dates, source, request.CollectiveNote);
        return true;
    }

    public static bool TryToApplication(
        this UpdateTripParkCandidateRequestDto request,
        out TripParkCandidateDetailsInput? input)
    {
        ArgumentNullException.ThrowIfNull(request);
        input = null;
        if (!TryParseDates(request.CandidateDates, out DateOnly[] dates))
        {
            return false;
        }

        input = new TripParkCandidateDetailsInput(dates, request.CollectiveNote);
        return true;
    }

    public static bool TryToApplication(
        this PutTripDayPlanRequestDto request,
        out TripDayPlanInput? input)
    {
        ArgumentNullException.ThrowIfNull(request);
        input = null;
        IReadOnlyCollection<TripDayBlockRequestDto>? requestedBlocks = request.Blocks;
        if (!TryParseOptionalTime(request.DesiredArrivalTime, out TimeOnly? arrival)
            || requestedBlocks is null
            || requestedBlocks.Count > TripDayPlan.MaximumBlocks
            || requestedBlocks.Any(static block => block is null))
        {
            return false;
        }

        List<TripDayBlockInput> blocks = new(requestedBlocks.Count);
        foreach (TripDayBlockRequestDto block in requestedBlocks)
        {
            if (!TripDayBlockId.TryParse(block.BlockId, out _)
                || !TryParseEnum(block.Type, out TripDayBlockType type)
                || !TryParseOptionalTime(block.LocalTime, out TimeOnly? localTime))
            {
                return false;
            }

            blocks.Add(new TripDayBlockInput(
                block.BlockId,
                type,
                block.Title,
                block.Details,
                localTime,
                block.SortPosition));
        }

        input = new TripDayPlanInput(
            request.ParkCandidateId,
            arrival,
            request.GroupNote,
            blocks);
        return true;
    }

    public static TripProgramDto ToHttp(this TripProgramResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new TripProgramDto
        {
            Candidates = result.Candidates.Select(static candidate => candidate.ToHttp()).ToArray(),
            Days = result.Days.Select(static day => day.ToHttp()).ToArray(),
        };
    }

    public static TripParkCandidateDto ToHttp(this TripParkCandidateResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new TripParkCandidateDto
        {
            CandidateId = result.CandidateId,
            ParkId = result.ParkId,
            ParkName = result.ParkName,
            CandidateDates = result.CandidateDates.Select(FormatDate).ToArray(),
            Source = result.Source.ToString(),
            State = result.State.ToString(),
            CollectiveNote = result.CollectiveNote,
            FitSnapshot = result.FitSnapshot is null
                ? null
                : new TripFitRecommendationSnapshotDto
                {
                    MethodVersion = result.FitSnapshot.MethodVersion,
                    Explanation = result.FitSnapshot.Explanation,
                    CalculatedAtUtc = result.FitSnapshot.CalculatedAtUtc,
                },
            SortPosition = result.SortPosition,
            Version = result.Version,
            CreatedAtUtc = result.CreatedAtUtc,
            UpdatedAtUtc = result.UpdatedAtUtc,
        };
    }

    public static TripDayPlanDto ToHttp(this TripDayPlanResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new TripDayPlanDto
        {
            DayPlanId = result.DayPlanId,
            LocalDate = FormatDate(result.LocalDate),
            ParkCandidateId = result.ParkCandidateId,
            ParkId = result.ParkId,
            ParkName = result.ParkName,
            DesiredArrivalTime = result.DesiredArrivalTime?.ToString(TimeFormat, CultureInfo.InvariantCulture),
            GroupNote = result.GroupNote,
            Blocks = result.Blocks.Select(static block => new TripDayBlockDto
            {
                BlockId = block.BlockId,
                Type = block.Type.ToString(),
                Title = block.Title,
                Details = block.Details,
                LocalTime = block.LocalTime?.ToString(TimeFormat, CultureInfo.InvariantCulture),
                SortPosition = block.SortPosition,
            }).ToArray(),
            Version = result.Version,
            CreatedAtUtc = result.CreatedAtUtc,
            UpdatedAtUtc = result.UpdatedAtUtc,
        };
    }

    public static bool TryParseDate(string? value, out DateOnly date)
    {
        return DateOnly.TryParseExact(
            value?.Trim(),
            DateFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);
    }

    public static bool TryParseEnum<TEnum>(string? value, out TEnum parsed)
        where TEnum : struct, Enum
    {
        return Enum.TryParse(value?.Trim(), true, out parsed) && Enum.IsDefined(parsed);
    }

    private static bool TryParseDates(
        IReadOnlyCollection<string>? values,
        out DateOnly[] dates)
    {
        dates = Array.Empty<DateOnly>();
        if (values is null || values.Count > TripDateProposal.MaximumCandidateDates)
        {
            return false;
        }

        List<DateOnly> parsed = new(values.Count);
        foreach (string value in values)
        {
            if (!TryParseDate(value, out DateOnly date))
            {
                return false;
            }

            parsed.Add(date);
        }

        dates = parsed.ToArray();
        return true;
    }

    private static bool TryParseOptionalTime(string? value, out TimeOnly? time)
    {
        time = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        bool success = TimeOnly.TryParseExact(
            value.Trim(),
            TimeFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out TimeOnly parsed);
        time = success ? parsed : null;
        return success;
    }

    private static string FormatDate(DateOnly date)
    {
        return date.ToString(DateFormat, CultureInfo.InvariantCulture);
    }
}
