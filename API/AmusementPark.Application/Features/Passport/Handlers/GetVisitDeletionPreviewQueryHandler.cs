using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Handlers;

public sealed class GetVisitDeletionPreviewQueryHandler
    : IQueryHandler<GetVisitDeletionPreviewQuery, ApplicationResult<VisitDeletionPreview>>
{
    private readonly IUserVisitRepository visitRepository;
    private readonly IVisitDeletionStore deletionStore;

    public GetVisitDeletionPreviewQueryHandler(
        IUserVisitRepository visitRepository,
        IVisitDeletionStore deletionStore)
    {
        this.visitRepository = visitRepository;
        this.deletionStore = deletionStore;
    }

    public async Task<ApplicationResult<VisitDeletionPreview>> HandleAsync(
        GetVisitDeletionPreviewQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.UserId)
            || !VisitId.TryParse(query.VisitId, out VisitId visitId))
        {
            return ApplicationResult<VisitDeletionPreview>.Failure(
                PassportApplicationErrors.VisitNotFound());
        }

        string userId = query.UserId.Trim();
        Visit? visit = await this.visitRepository.GetOwnedAsync(
            visitId,
            userId,
            cancellationToken);
        if (visit is null)
        {
            return ApplicationResult<VisitDeletionPreview>.Failure(
                PassportApplicationErrors.VisitNotFound());
        }

        VisitDeletionImpact impact = await this.deletionStore.GetImpactAsync(
            visit.Id,
            userId,
            cancellationToken);
        return ApplicationResult<VisitDeletionPreview>.Success(
            new VisitDeletionPreview(
                visit.Id.Value,
                visit.Version,
                impact.OccurrenceCount,
                impact.AssessmentCount + (visit.ParkAssessment is null ? 0 : 1),
                VisitDeletionPolicy.RetentionDays));
    }
}
