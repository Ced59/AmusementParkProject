using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Queries;
using AmusementPark.Application.Features.Sharing.Results;

namespace AmusementPark.Application.Features.Sharing.Handlers;

public sealed class GetVisitRecapShareCandidatesQueryHandler
    : IQueryHandler<
        GetVisitRecapShareCandidatesQuery,
        ApplicationResult<VisitRecapShareCandidatesResult>>
{
    private readonly IVisitRecapSharePreviewBuilder previewBuilder;

    public GetVisitRecapShareCandidatesQueryHandler(
        IVisitRecapSharePreviewBuilder previewBuilder)
    {
        this.previewBuilder = previewBuilder
            ?? throw new ArgumentNullException(nameof(previewBuilder));
    }

    public Task<ApplicationResult<VisitRecapShareCandidatesResult>> HandleAsync(
        GetVisitRecapShareCandidatesQuery query,
        CancellationToken cancellationToken = default)
    {
        return this.previewBuilder.GetCandidatesAsync(
            query.OwnerUserId,
            query.VisitId,
            query.IncludeMissedItems,
            cancellationToken);
    }
}
