using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.CaptainCoaster.Results;

namespace AmusementPark.Application.Features.CaptainCoaster.Commands;

/// <summary>
/// Met à jour les paramètres Captain Coaster.
/// </summary>
public sealed record UpdateCaptainCoasterSettingsCommand(CaptainCoasterSettingsResult Settings) : ICommand<ApplicationResult<CaptainCoasterSettingsResult>>;
