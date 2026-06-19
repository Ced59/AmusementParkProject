using AmusementPark.Core.Abstractions;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Core.Domain.Ratings;

/// <summary>
/// Note posée par un utilisateur authentifié sur une cible publique.
/// </summary>
public sealed class UserRating : AuditableEntity
{
    public string UserId { get; set; } = string.Empty;

    public RatingTargetType TargetType { get; set; }

    public string TargetId { get; set; } = string.Empty;

    public string ParkId { get; set; } = string.Empty;

    public ParkItemCategory? ParkItemCategory { get; set; }

    public ParkItemType? ParkItemType { get; set; }

    public double Value { get; set; }
}
