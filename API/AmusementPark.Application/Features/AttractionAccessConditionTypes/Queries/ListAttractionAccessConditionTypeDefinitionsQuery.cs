using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.AttractionAccessConditionTypes.Queries;

/// <summary>
/// Liste les types réutilisables de conditions d'accès.
/// </summary>
public sealed record ListAttractionAccessConditionTypeDefinitionsQuery(bool IncludeInactive = false)
    : IQuery<ApplicationResult<IReadOnlyCollection<AttractionAccessConditionTypeDefinition>>>;
