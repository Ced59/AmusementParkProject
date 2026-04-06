using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.CaptainCoaster.Results;

namespace AmusementPark.Application.Features.CaptainCoaster.Queries;

/// <summary>
/// Récupère les paramètres Captain Coaster.
/// </summary>
public sealed record GetCaptainCoasterSettingsQuery : IQuery<ApplicationResult<CaptainCoasterSettingsResult>>;
