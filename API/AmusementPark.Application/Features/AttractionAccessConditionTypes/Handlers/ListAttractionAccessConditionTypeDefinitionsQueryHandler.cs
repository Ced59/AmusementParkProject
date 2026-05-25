using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionAccessConditionTypes.Ports;
using AmusementPark.Application.Features.AttractionAccessConditionTypes.Queries;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.AttractionAccessConditionTypes.Handlers;

public sealed class ListAttractionAccessConditionTypeDefinitionsQueryHandler
    : IQueryHandler<ListAttractionAccessConditionTypeDefinitionsQuery, ApplicationResult<IReadOnlyCollection<AttractionAccessConditionTypeDefinition>>>
{
    private readonly IAttractionAccessConditionTypeDefinitionRepository repository;

    public ListAttractionAccessConditionTypeDefinitionsQueryHandler(IAttractionAccessConditionTypeDefinitionRepository repository)
    {
        this.repository = repository;
    }

    public async Task<ApplicationResult<IReadOnlyCollection<AttractionAccessConditionTypeDefinition>>> HandleAsync(
        ListAttractionAccessConditionTypeDefinitionsQuery query,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<AttractionAccessConditionTypeDefinition> values = await this.repository.GetAllAsync(query.IncludeInactive, cancellationToken);
        return ApplicationResult<IReadOnlyCollection<AttractionAccessConditionTypeDefinition>>.Success(values);
    }
}
