using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.CaptainCoaster.Contracts;
using AmusementPark.Application.Features.CaptainCoaster.Results;

namespace AmusementPark.Application.Features.CaptainCoaster.Commands;

/// <summary>
/// Applique des choix de résolution Captain Coaster.
/// </summary>
public sealed record ApplyCaptainCoasterChangesCommand(string SessionId, IReadOnlyCollection<CaptainCoasterDuplicateResolution> Resolutions) : ICommand<ApplicationResult<CaptainCoasterSessionResult>>;
