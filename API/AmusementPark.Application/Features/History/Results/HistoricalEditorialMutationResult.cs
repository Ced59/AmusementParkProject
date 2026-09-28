using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Results;

public sealed record HistoricalEditorialMutationResult(
    HistoricalReviewResourceType ResourceType,
    Guid ResourceId,
    int Revision,
    HistoricalEditorialWorkflowState WorkflowState,
    HistoricalPublicationState PublicationState,
    HistoricalFactState? FactState);
