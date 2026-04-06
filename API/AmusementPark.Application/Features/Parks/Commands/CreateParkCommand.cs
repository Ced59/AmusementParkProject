using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Commands;

/// <summary>
/// Crée un nouveau parc.
/// </summary>
/// <param name="Park">Agrégat parc à créer.</param>
public sealed record CreateParkCommand(Park Park) : ICommand<ApplicationResult<Park>>;
