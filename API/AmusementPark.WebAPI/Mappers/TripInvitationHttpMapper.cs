using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.WebAPI.Contracts.Trips;

namespace AmusementPark.WebAPI.Mappers;

public static class TripInvitationHttpMapper
{
    public static TripInvitationCreationDto ToHttp(this TripInvitationCreationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new TripInvitationCreationDto(
            result.InvitationId,
            result.Token,
            result.ProposedRole,
            result.ExpiresAtUtc,
            result.IsTargeted,
            result.WasReplayed);
    }

    public static TripInvitationSummaryDto ToHttp(this TripInvitationSummaryResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new TripInvitationSummaryDto(
            result.InvitationId,
            result.TokenHint,
            result.ProposedRole,
            result.ExpiresAtUtc,
            result.IsTargeted,
            result.Version,
            result.CreatedAtUtc);
    }

    public static TripInvitationPreviewDto ToHttp(this TripInvitationPreviewResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new TripInvitationPreviewDto(
            result.TripTitle,
            result.InviterDisplayName,
            result.ProposedRole,
            result.PeriodKind,
            result.StartMonth,
            result.EndMonth,
            result.MemberCountBand,
            result.ExpiresAtUtc,
            result.IsTargeted);
    }
}
