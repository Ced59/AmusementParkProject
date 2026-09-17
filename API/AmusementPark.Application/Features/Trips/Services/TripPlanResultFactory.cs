using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Services;

internal static class TripPlanResultFactory
{
    public static TripPlanResult ToResult(TripPlan trip, string currentUserId)
    {
        ArgumentNullException.ThrowIfNull(trip);
        return new TripPlanResult(
            trip.Id.Value,
            trip.Title,
            new TripDateProposalResult(
                trip.DateProposal.Kind,
                trip.DateProposal.StartDate,
                trip.DateProposal.EndDate,
                trip.DateProposal.CandidateDates),
            trip.DestinationTimeZoneId,
            trip.Status,
            trip.AccessScope,
            trip.Members.Count(member => member.State == TripMembershipState.Active),
            string.Equals(trip.OwnerUserId, currentUserId, StringComparison.Ordinal),
            trip.CreatedAtUtc,
            trip.UpdatedAtUtc,
            trip.Version);
    }
}
