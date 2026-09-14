using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Models;
using AmusementPark.Application.Features.ParkFit.Results;
using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkFit.Queries;

/// <summary>
/// Recherche anonyme bornée sans persistance du profil.
/// </summary>
public sealed record SearchParksByFitQuery(
    DateOnly EvaluationDate,
    IReadOnlyCollection<ParkFitSearchMemberCriteria> Members,
    IReadOnlyCollection<ParkItemType> PreferredAttractionTypes,
    bool PreferIndoor,
    string? CountryCode,
    ParkFitUnknownDataPolicy UnknownDataPolicy,
    int MaximumResults)
    : IQuery<ApplicationResult<ParkFitSearchResult>>;
