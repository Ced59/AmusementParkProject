using AmusementPark.Core.Abstractions;

namespace AmusementPark.Core.Domain.Ratings;

/// <summary>
/// Publication volontaire et révocable des classements personnels d'un utilisateur.
/// </summary>
public sealed class UserRankingShare : AuditableEntity
{
    public string UserId { get; private set; } = string.Empty;

    public bool IsPublic { get; private set; }

    public string? ShareId { get; private set; }

    public DateTime? PublishedAtUtc { get; private set; }

    public static UserRankingShare Create(string userId, DateTime nowUtc)
    {
        string normalizedUserId = NormalizeRequired(userId, nameof(userId));
        return new UserRankingShare
        {
            UserId = normalizedUserId,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
        };
    }

    public static UserRankingShare Restore(
        string id,
        string userId,
        bool isPublic,
        string? shareId,
        DateTime? publishedAtUtc,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        return new UserRankingShare
        {
            Id = NormalizeRequired(id, nameof(id)),
            UserId = NormalizeRequired(userId, nameof(userId)),
            IsPublic = isPublic && !string.IsNullOrWhiteSpace(shareId),
            ShareId = isPublic ? NormalizeOptional(shareId) : null,
            PublishedAtUtc = isPublic ? publishedAtUtc : null,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = updatedAtUtc,
        };
    }

    public void Publish(string shareId, DateTime nowUtc)
    {
        if (IsPublic && !string.IsNullOrWhiteSpace(ShareId))
        {
            return;
        }

        ShareId = NormalizeRequired(shareId, nameof(shareId));
        IsPublic = true;
        PublishedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    public void Revoke(DateTime nowUtc)
    {
        if (!IsPublic && ShareId is null)
        {
            return;
        }

        IsPublic = false;
        ShareId = null;
        PublishedAtUtc = null;
        UpdatedAtUtc = nowUtc;
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        string normalizedValue = value?.Trim() ?? string.Empty;
        if (normalizedValue.Length == 0)
        {
            throw new ArgumentException("A non-empty value is required.", parameterName);
        }

        return normalizedValue;
    }

    private static string? NormalizeOptional(string? value)
    {
        string normalizedValue = value?.Trim() ?? string.Empty;
        return normalizedValue.Length > 0 ? normalizedValue : null;
    }
}
