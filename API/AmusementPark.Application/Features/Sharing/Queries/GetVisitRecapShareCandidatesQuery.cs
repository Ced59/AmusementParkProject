using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Results;

namespace AmusementPark.Application.Features.Sharing.Queries;

public sealed record GetVisitRecapShareCandidatesQuery(
    string OwnerUserId,
    string VisitId,
    bool IncludeMissedItems)
    : IQuery<ApplicationResult<VisitRecapShareCandidatesResult>>;
