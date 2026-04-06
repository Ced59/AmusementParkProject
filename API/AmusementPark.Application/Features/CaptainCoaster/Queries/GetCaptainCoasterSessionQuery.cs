using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.CaptainCoaster.Results;

namespace AmusementPark.Application.Features.CaptainCoaster.Queries;

/// <summary>
/// Récupère une session Captain Coaster.
/// </summary>
public sealed record GetCaptainCoasterSessionQuery(string SessionId) : IQuery<ApplicationResult<CaptainCoasterSessionResult>>;
