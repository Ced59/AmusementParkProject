using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Handlers;

public sealed class EvaluateVisitRideTargetsQueryHandler
    : IQueryHandler<
        EvaluateVisitRideTargetsQuery,
        ApplicationResult<IReadOnlyCollection<VisitRideTargetEvaluationResult>>>
{
    private const int MaximumTargetCount = 100;

    private readonly IUserVisitRepository visitRepository;
    private readonly IVisitTargetResolver targetResolver;

    public EvaluateVisitRideTargetsQueryHandler(
        IUserVisitRepository visitRepository,
        IVisitTargetResolver targetResolver)
    {
        this.visitRepository = visitRepository;
        this.targetResolver = targetResolver;
    }

    public async Task<ApplicationResult<IReadOnlyCollection<VisitRideTargetEvaluationResult>>> HandleAsync(
        EvaluateVisitRideTargetsQuery query,
        CancellationToken cancellationToken = default)
    {
        string userId = query.UserId?.Trim() ?? string.Empty;
        string[] parkItemIds = query.ParkItemIds?
            .Select(static value => value?.Trim() ?? string.Empty)
            .Distinct(StringComparer.Ordinal)
            .ToArray() ?? Array.Empty<string>();
        if (userId.Length == 0
            || parkItemIds.Length is < 1 or > MaximumTargetCount
            || parkItemIds.Any(static value => value.Length == 0))
        {
            return Failure(PassportApplicationErrors.InvalidRideOccurrenceBatch());
        }

        VisitId visitId;
        try
        {
            visitId = VisitId.Parse(query.VisitId);
        }
        catch (ArgumentException)
        {
            return Failure(PassportApplicationErrors.VisitNotFound());
        }

        Visit? visit = await this.visitRepository.GetOwnedAsync(
            visitId,
            userId,
            cancellationToken);
        if (visit is null)
        {
            return Failure(PassportApplicationErrors.VisitNotFound());
        }

        IReadOnlyDictionary<string, VisitTarget> targets =
            await this.targetResolver.ResolveAsync(parkItemIds, cancellationToken);
        List<VisitRideTargetEvaluationResult> evaluations = new List<VisitRideTargetEvaluationResult>(
            parkItemIds.Length);
        foreach (string parkItemId in parkItemIds)
        {
            ApplicationError? targetError = PassportRideOccurrenceHandlerSupport.ValidateTargetIdentity(
                visit.ParkId,
                parkItemId,
                targets,
                out VisitTarget? target);
            if (targetError is not null || target is null)
            {
                return Failure(targetError ?? PassportApplicationErrors.VisitTargetNotFound());
            }

            if (!target.IsVisible)
            {
                return Failure(PassportApplicationErrors.VisitTargetNotFound());
            }

            HistoricalConsistency consistency =
                RideOccurrenceHistoricalConsistencyEvaluator.Evaluate(
                    visit.Date,
                    target.OpeningDate,
                    target.ClosingDate);
            evaluations.Add(new VisitRideTargetEvaluationResult(
                target.ParkItemId,
                consistency,
                target.OpeningDate,
                target.ClosingDate));
        }

        return ApplicationResult<IReadOnlyCollection<VisitRideTargetEvaluationResult>>.Success(
            evaluations);
    }

    private static ApplicationResult<IReadOnlyCollection<VisitRideTargetEvaluationResult>> Failure(
        ApplicationError error)
    {
        return ApplicationResult<IReadOnlyCollection<VisitRideTargetEvaluationResult>>.Failure(error);
    }
}
