using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Handlers;

public sealed class ListRideOccurrencesQueryHandler
    : IQueryHandler<ListRideOccurrencesQuery, ApplicationResult<RideOccurrencePageResult>>
{
    private readonly IUserVisitRepository visitRepository;
    private readonly IRideOccurrenceRepository occurrenceRepository;
    private readonly IVisitTargetResolver targetResolver;

    public ListRideOccurrencesQueryHandler(
        IUserVisitRepository visitRepository,
        IRideOccurrenceRepository occurrenceRepository,
        IVisitTargetResolver targetResolver)
    {
        this.visitRepository = visitRepository;
        this.occurrenceRepository = occurrenceRepository;
        this.targetResolver = targetResolver;
    }

    public async Task<ApplicationResult<RideOccurrencePageResult>> HandleAsync(
        ListRideOccurrencesQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!PassportRideOccurrenceHandlerSupport.TryNormalizeRequestScope(
                query.UserId,
                query.VisitId,
                out string userId,
                out VisitId visitId)
            || query.Limit is < 1 or > RideOccurrenceListCriteria.MaximumLimit)
        {
            return ApplicationResult<RideOccurrencePageResult>.Failure(
                query.Limit is < 1 or > RideOccurrenceListCriteria.MaximumLimit
                    ? PassportApplicationErrors.InvalidRideOccurrenceListLimit()
                    : PassportApplicationErrors.VisitNotFound());
        }

        Visit? visit = await this.visitRepository.GetOwnedAsync(
            visitId,
            userId,
            cancellationToken);
        if (visit is null)
        {
            return ApplicationResult<RideOccurrencePageResult>.Failure(
                PassportApplicationErrors.VisitNotFound());
        }

        RideOccurrencePage page = await this.occurrenceRepository.ListOwnedByVisitAsync(
            new RideOccurrenceListCriteria(visitId, userId, query.Limit, query.After),
            cancellationToken);
        IReadOnlyDictionary<string, VisitTarget> targets = page.Items.Count == 0
            ? new Dictionary<string, VisitTarget>(StringComparer.Ordinal)
            : await this.targetResolver.ResolveAsync(
                page.Items
                    .Select(static occurrence => occurrence.ParkItemId)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray(),
                cancellationToken);
        return ApplicationResult<RideOccurrencePageResult>.Success(
            new RideOccurrencePageResult(
                page.Items.Select(occurrence =>
                {
                    targets.TryGetValue(occurrence.ParkItemId, out VisitTarget? target);
                    return PassportRideOccurrenceResultFactory.Create(occurrence, target, visit.Date);
                }).ToArray(),
                page.NextCursor));
    }
}
