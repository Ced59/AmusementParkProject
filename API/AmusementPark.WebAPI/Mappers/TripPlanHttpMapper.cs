using System.Globalization;
using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.WebAPI.Contracts.Trips;

namespace AmusementPark.WebAPI.Mappers;

public static class TripPlanHttpMapper
{
    private const string DateFormat = "yyyy-MM-dd";

    public static bool TryToApplication(
        this TripPlanWriteRequestDto request,
        out TripPlanDetailsInput? input)
    {
        ArgumentNullException.ThrowIfNull(request);
        input = null;
        if (!TryMapDateProposal(request.DateProposal, out TripDateProposal? dateProposal)
            || dateProposal is null)
        {
            return false;
        }

        input = new TripPlanDetailsInput(request.Title, dateProposal, request.DestinationTimeZoneId);
        return true;
    }

    public static bool TryToApplication(
        this SetTripPlanDatesRequestDto request,
        out TripPlanDatesInput? input)
    {
        ArgumentNullException.ThrowIfNull(request);
        input = null;
        if (!TryMapDateProposal(request.DateProposal, out TripDateProposal? dateProposal)
            || dateProposal is null)
        {
            return false;
        }

        input = new TripPlanDatesInput(dateProposal, request.DestinationTimeZoneId);
        return true;
    }

    public static TripPlanDto ToHttp(this TripPlanResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new TripPlanDto
        {
            TripPlanId = result.TripPlanId,
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
            AccessScope = result.AccessScope.ToString(),
            MemberCount = result.MemberCount,
            IsOwner = result.IsOwner,
            EffectiveRole = result.EffectiveRole.ToString(),
            CanEditPlan = result.CanEditPlan,
            CanEditProgram = result.CanEditProgram,
            CanInvite = result.CanInvite,
            CanChangeRoles = result.CanChangeRoles,
            CreatedAtUtc = result.CreatedAtUtc,
            UpdatedAtUtc = result.UpdatedAtUtc,
            Version = result.Version,
        };
    }

    private static bool TryMapDateProposal(
        TripDateProposalRequestDto? request,
        out TripDateProposal? proposal)
    {
        proposal = null;
        if (request is null
            || !Enum.TryParse(request.Kind?.Trim(), true, out TripDateProposalKind kind)
            || !Enum.IsDefined(kind)
            || !TryParseOptional(request.StartDate, out DateOnly? startDate)
            || !TryParseOptional(request.EndDate, out DateOnly? endDate)
            || !TryParseMany(request.CandidateDates, out DateOnly[] candidateDates))
        {
            return false;
        }

        try
        {
            proposal = TripDateProposal.Restore(kind, startDate, endDate, candidateDates);
            return true;
        }
        catch (TripPlanValidationException)
        {
            return false;
        }
    }

    private static bool TryParseOptional(string? value, out DateOnly? parsed)
    {
        parsed = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        bool success = DateOnly.TryParseExact(
            value.Trim(),
            DateFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateOnly date);
        parsed = success ? date : null;
        return success;
    }

    private static bool TryParseMany(
        IReadOnlyCollection<string>? values,
        out DateOnly[] parsed)
    {
        parsed = Array.Empty<DateOnly>();
        if (values is null || values.Count > TripDateProposal.MaximumCandidateDates)
        {
            return false;
        }

        List<DateOnly> dates = new();
        foreach (string value in values)
        {
            if (!DateOnly.TryParseExact(
                value?.Trim(),
                DateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateOnly date))
            {
                return false;
            }

            dates.Add(date);
        }

        parsed = dates.ToArray();
        return true;
    }

    private static string? Format(DateOnly? value)
    {
        return value?.ToString(DateFormat, CultureInfo.InvariantCulture);
    }
}
