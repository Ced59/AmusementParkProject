using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionAccessConditionTypes.Contracts;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.AttractionAccessConditionTypes.Commands;

/// <summary>
/// Crée ou met à jour un type réutilisable de condition d'accès.
/// </summary>
public sealed record UpsertAttractionAccessConditionTypeDefinitionCommand(AttractionAccessConditionTypeDefinitionWriteModel TypeDefinition)
    : ICommand<ApplicationResult<AttractionAccessConditionTypeDefinition>>;
