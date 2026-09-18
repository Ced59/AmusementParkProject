using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.WebAPI.Contracts.Trips;

namespace AmusementPark.WebAPI.Mappers;

public static class TripPreferenceSummaryHttpMapper
{
    public static TripPreferenceSummaryDto ToHttp(this TripPreferenceSummaryResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new TripPreferenceSummaryDto
        {
            TripPlanId = result.TripPlanId,
            TripTitle = result.TripTitle,
            PlanVersion = result.PlanVersion,
            ParticipantCount = result.ParticipantCount,
            CanDecide = result.CanDecide,
            Items = result.Items.Select(static item => new TripItemPreferenceSummaryDto
            {
                ParkId = item.ParkId,
                ParkName = item.ParkName,
                ParkItemId = item.ParkItemId,
                ParkItemName = item.ParkItemName,
                MainImageId = item.MainImageId,
                MustDoCount = item.MustDoCount,
                WantToDoCount = item.WantToDoCount,
                OptionalCount = item.OptionalCount,
                NotForMeCount = item.NotForMeCount,
                UnansweredCount = item.UnansweredCount,
                Compatibility = item.Compatibility.ToString(),
                IsCompatibilityKnown = item.IsCompatibilityKnown,
                HasIndividualConstraint = item.HasIndividualConstraint,
                IsGroupPriority = item.IsGroupPriority,
                OfficialStatus = item.OfficialStatus,
                OfficialSourceUrl = item.OfficialSourceUrl,
                OfficialStatusVerifiedAtUtc = item.OfficialStatusVerifiedAtUtc,
                Decision = item.Decision is null
                    ? null
                    : new TripItemDecisionDto
                    {
                        Status = item.Decision.Status.ToString(),
                        Reason = item.Decision.Reason,
                        DecidedByDisplayName = item.Decision.DecidedByDisplayName,
                        DecidedAtUtc = item.Decision.DecidedAtUtc,
                        Version = item.Decision.Version,
                    },
            }).ToList(),
        };
    }

    public static bool TryToApplication(
        this SetTripItemDecisionRequestDto request,
        string parkItemId,
        out TripItemDecisionInput? input)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!Enum.TryParse(request.Status?.Trim(), true, out TripItemDecisionStatus status)
            || !Enum.IsDefined(status))
        {
            input = null;
            return false;
        }

        input = new TripItemDecisionInput(
            parkItemId,
            request.ExpectedDecisionVersion,
            status,
            request.Reason);
        return true;
    }
}
