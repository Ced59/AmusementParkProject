namespace AmusementPark.Core.Domain.Sharing;

/// <summary>
/// Cycle de vie d'une publication, séparé de sa visibilité.
/// </summary>
public enum SharePublicationStatus
{
    Draft = 1,
    Published = 2,
    NeedsReview = 3,
    Revoked = 4,
}
