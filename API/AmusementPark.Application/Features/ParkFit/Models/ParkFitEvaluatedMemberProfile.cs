using AmusementPark.Core.Domain.ParkFit;

namespace AmusementPark.Application.Features.ParkFit.Models;

/// <summary>
/// Associe une clé éphémère au profil Core construit après validation.
/// </summary>
public sealed record ParkFitEvaluatedMemberProfile(
    string MemberKey,
    ParkFitMemberProfile Profile);
