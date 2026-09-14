namespace AmusementPark.Application.Features.ParkFit.Models;

/// <summary>
/// Critères anonymes minimisés d'un membre du groupe.
/// </summary>
public sealed record ParkFitSearchMemberCriteria(
    string MemberKey,
    int? HeightCentimeters,
    int? MinimumAgeYears,
    int? MaximumAgeYears,
    bool? CanBeAccompanied,
    int? CompanionMinimumAgeYears,
    int? CompanionMaximumAgeYears);
