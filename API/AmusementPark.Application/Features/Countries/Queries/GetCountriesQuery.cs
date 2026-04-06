using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Countries;

namespace AmusementPark.Application.Features.Countries.Queries;

/// <summary>
/// Récupère la liste des pays connus du référentiel métier.
/// </summary>
/// <param name="LanguageCode">Langue préférée éventuelle.</param>
public sealed record GetCountriesQuery(string? LanguageCode) : IQuery<ApplicationResult<IReadOnlyCollection<Country>>>;
