namespace AmusementPark.Application.Features.ParkFit;

/// <summary>
/// Bornes partagées du cas d'usage public de recherche FIT.
/// </summary>
public static class ParkFitSearchLimits
{
    public const int MaximumMemberCount = 8;
    public const int MaximumPreferenceCount = 8;
    public const int MaximumResultCount = 20;
    public const int MaximumMemberKeyLength = 32;
    public const int MaximumActiveCandidateCount = 200;
    public const int MaximumCriticalSourceCountPerPark = 12;
    public static readonly TimeSpan MaximumVerificationAge = TimeSpan.FromDays(365);
}
