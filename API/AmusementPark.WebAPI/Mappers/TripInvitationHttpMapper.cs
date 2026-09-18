using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Core.Domain.Trips;
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
            ToHttp(result.ProposedRole),
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
            ToHttp(result.ProposedRole),
            result.ExpiresAtUtc,
            result.IsTargeted,
            result.Version,
            result.CreatedAtUtc);
    }

    public static TripInvitationListDto ToHttp(this TripInvitationListResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new TripInvitationListDto(
            result.InviterDisplayName,
            result.Invitations.Select(static invitation => invitation.ToHttp()).ToArray());
    }

    public static TripInvitationPreviewDto ToHttp(this TripInvitationPreviewResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new TripInvitationPreviewDto(
            result.TripTitle,
            result.InviterDisplayName,
            ToHttp(result.ProposedRole),
            ToHttp(result.PeriodKind),
            result.StartMonth,
            result.EndMonth,
            ToHttp(result.MemberCountBand),
            result.ExpiresAtUtc,
            result.IsTargeted);
    }

    private static TripDelegatedRoleDto ToHttp(TripDelegatedRole role)
    {
        return role switch
        {
            TripDelegatedRole.Editor => TripDelegatedRoleDto.Editor,
            TripDelegatedRole.Participant => TripDelegatedRoleDto.Participant,
            TripDelegatedRole.Viewer => TripDelegatedRoleDto.Viewer,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null),
        };
    }

    private static TripInvitationPeriodKindDto ToHttp(TripInvitationPeriodKind periodKind)
    {
        return periodKind switch
        {
            TripInvitationPeriodKind.Unspecified => TripInvitationPeriodKindDto.Unspecified,
            TripInvitationPeriodKind.SingleMonth => TripInvitationPeriodKindDto.SingleMonth,
            TripInvitationPeriodKind.MonthRange => TripInvitationPeriodKindDto.MonthRange,
            _ => throw new ArgumentOutOfRangeException(nameof(periodKind), periodKind, null),
        };
    }

    private static TripInvitationMemberCountBandDto ToHttp(TripInvitationMemberCountBand band)
    {
        return band switch
        {
            TripInvitationMemberCountBand.One => TripInvitationMemberCountBandDto.One,
            TripInvitationMemberCountBand.TwoToFive => TripInvitationMemberCountBandDto.TwoToFive,
            TripInvitationMemberCountBand.SixToTen => TripInvitationMemberCountBandDto.SixToTen,
            TripInvitationMemberCountBand.ElevenToFifty => TripInvitationMemberCountBandDto.ElevenToFifty,
            _ => throw new ArgumentOutOfRangeException(nameof(band), band, null),
        };
    }
}
