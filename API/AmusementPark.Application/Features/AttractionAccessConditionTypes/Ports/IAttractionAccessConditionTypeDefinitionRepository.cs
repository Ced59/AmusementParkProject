using AmusementPark.Application.Features.AttractionAccessConditionTypes.Contracts;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.AttractionAccessConditionTypes.Ports;

/// <summary>
/// Port de persistance du catalogue des types de conditions d'accès.
/// </summary>
public interface IAttractionAccessConditionTypeDefinitionRepository
{
    Task<IReadOnlyCollection<AttractionAccessConditionTypeDefinition>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken);
    Task<AttractionAccessConditionTypeDefinition?> GetByKeyAsync(string key, CancellationToken cancellationToken);
    Task<AttractionAccessConditionTypeDefinition> UpsertAsync(AttractionAccessConditionTypeDefinitionWriteModel model, CancellationToken cancellationToken);
}
