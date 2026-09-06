using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Sharing.Models;

public sealed record PersonalRankingShareIdentityState(
    string? DisplayName,
    string? AvatarUrl,
    bool IsActivated,
    bool IsBlocked)
{
    public static PersonalRankingShareIdentityState Capture(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return new PersonalRankingShareIdentityState(
            NormalizeOptional(user.ResolvePublicDisplayName()),
            NormalizeOptional(user.AvatarUrl),
            user.IsActivated,
            user.IsBlocked);
    }

    private static string? NormalizeOptional(string? value)
    {
        string normalized = value?.Trim() ?? string.Empty;
        return normalized.Length == 0 ? null : normalized;
    }
}
