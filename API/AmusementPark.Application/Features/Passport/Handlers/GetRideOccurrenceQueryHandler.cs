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

public sealed class GetRideOccurrenceQueryHandler
    : IQueryHandler<GetRideOccurrenceQuery, ApplicationResult<RideOccurrenceResult>>
{
    private readonly IUserVisitRepository visitRepository;
    private readonly IRideOccurrenceRepository occurrenceRepository;
    private readonly IVisitTargetResolver targetResolver;

    public GetRideOccurrenceQueryHandler(
        IUserVisitRepository visitRepository,
        IRideOccurrenceRepository occurrenceRepository,
        IVisitTargetResolver targetResolver)
    {
        this.visitRepository = visitRepository;
        this.occurrenceRepository = occurrenceRepository;
        this.targetResolver = targetResolver;
    }

    public async Task<ApplicationResult<RideOccurrenceResult>> HandleAsync(
        GetRideOccurrenceQuery query,
        CancellationToken cancellationToken = default)
    {
        ParsedOccurrenceScope? scope = PassportRideOccurrenceHandlerSupport.ParseOccurrenceScope(
            query.UserId,
            query.VisitId,
            query.OccurrenceId);
        if (scope is null)
        {
            return ApplicationResult<RideOccurrenceResult>.Failure(
                PassportApplicationErrors.RideOccurrenceNotFound());
        }

        Visit? visit = await this.visitRepository.GetOwnedAsync(
            scope.VisitId,
            scope.UserId,
            cancellationToken);
        if (visit is null)
        {
            return ApplicationResult<RideOccurrenceResult>.Failure(
                PassportApplicationErrors.RideOccurrenceNotFound());
        }

        RideOccurrence? occurrence = await this.occurrenceRepository.GetOwnedAsync(
            scope.OccurrenceId,
            scope.VisitId,
            scope.UserId,
            cancellationToken);
        if (occurrence is null)
        {
            return ApplicationResult<RideOccurrenceResult>.Failure(
                PassportApplicationErrors.RideOccurrenceNotFound());
        }

        IReadOnlyDictionary<string, VisitTarget> targets =
            await this.targetResolver.ResolveAsync(
                new[] { occurrence.ParkItemId },
                cancellationToken);
        targets.TryGetValue(occurrence.ParkItemId, out VisitTarget? target);
        return ApplicationResult<RideOccurrenceResult>.Success(
            PassportRideOccurrenceResultFactory.Create(occurrence, target, visit.Date));
    }
}
