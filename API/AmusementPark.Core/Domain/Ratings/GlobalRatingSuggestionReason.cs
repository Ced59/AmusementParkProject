namespace AmusementPark.Core.Domain.Ratings;

/// <summary>
/// Explique pourquoi des observations privées invitent à revoir une note globale.
/// </summary>
public enum GlobalRatingSuggestionReason
{
    RecentExperiencesLower = 1,
    RecentExperiencesHigher = 2,
}
