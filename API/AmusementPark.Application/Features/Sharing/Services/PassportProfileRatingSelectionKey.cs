using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Sharing.Services;

public static class PassportProfileRatingSelectionKey
{
    public static string Create(RatingTargetType targetType, string targetId)
    {
        string normalizedId = targetId?.Trim() ?? string.Empty;
        if (!Enum.IsDefined(targetType) || normalizedId.Length == 0)
        {
            throw new ArgumentException("A valid rating target is required.", nameof(targetId));
        }

        return string.Concat(targetType.ToString(), ":", normalizedId);
    }
}
