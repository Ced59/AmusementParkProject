using AmusementPark.Core.Domain.Users;
using AmusementPark.WebAPI.Contracts.ParkDataEditorTokens;

namespace AmusementPark.WebAPI.Mappers;

internal static class ParkDataEditorTokenHttpMappers
{
    public static ParkDataEditorTokenDto ToHttp(this ParkDataEditorAccessToken token)
    {
        return new ParkDataEditorTokenDto
        {
            Id = token.Id,
            Label = token.Label,
            DisplayPrefix = token.DisplayPrefix,
            CreatedAtUtc = token.CreatedAtUtc,
            ExpiresAtUtc = token.ExpiresAtUtc,
            LastUsedAtUtc = token.LastUsedAtUtc,
            RevokedAtUtc = token.RevokedAtUtc,
            RevokedByUserId = token.RevokedByUserId,
            RevocationReason = token.RevocationReason,
            IsActive = token.IsActiveAt(DateTime.UtcNow),
        };
    }
}
