using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Queries;

public sealed record PreviewHistoricalSnapshotImpactQuery(
    string ParkId,
    HistoricalReviewResourceType ResourceType,
    Guid ResourceId,
    int? Year)
    : IQuery<ApplicationResult<HistoricalPublicationImpactPreviewResult>>;
