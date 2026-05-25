using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LocalizedContent.Results;

namespace AmusementPark.Application.Features.LocalizedContent.Commands;

/// <summary>
/// Applique un JSON de champs localisés à une entité sélectionnée dans l'administration.
/// </summary>
public sealed record ApplyLocalizedContentJsonCommand(
    string EntityType,
    string EntityId,
    string Json) : ICommand<ApplicationResult<LocalizedContentApplyResult>>;
