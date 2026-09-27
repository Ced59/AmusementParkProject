using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Handlers;

public sealed class EvaluateVisitRideTargetsQueryHandler
    : IQueryHandler<
        EvaluateVisitRideTargetsQuery,
        ApplicationResult<IReadOnlyCollection<VisitRideTargetEvaluationResult>>>
{
    private const int MaximumTargetCount = 100;

    private readonly IUserVisitRepository visitRepository;
    private readonly IPassportHistoricalTargetResolver targetResolver;

    public EvaluateVisitRideTargetsQueryHandler(
        IUserVisitRepository visitRepository,
        IPassportHistoricalTargetResolver targetResolver)
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

        PassportHistoricalTargetContext context =
            await this.targetResolver.ResolveAsync(visit, parkItemIds, cancellationToken);
        List<VisitRideTargetEvaluationResult> evaluations = new List<VisitRideTargetEvaluationResult>(
            parkItemIds.Length);
        foreach (string parkItemId in parkItemIds)
        {
            if (!context.Targets.TryGetValue(parkItemId, out PassportHistoricalTarget? target))
            {
                return Failure(PassportApplicationErrors.VisitTargetNotFound());
            }

            if (!string.Equals(target.ParkId, visit.ParkId, StringComparison.Ordinal))
            {
                return Failure(PassportApplicationErrors.VisitTargetParkMismatch());
            }

            if (!string.Equals(
                    target.Category,
                    ParkItemCategory.Attraction.ToString(),
                    StringComparison.OrdinalIgnoreCase))
            {
                return Failure(PassportApplicationErrors.VisitTargetNotAttraction());
            }

            evaluations.Add(new VisitRideTargetEvaluationResult(
                target.ParkItemId,
                target.Name,
                target.Category,
                target.OperationalState,
                target.HistoricalConsistency,
                target.IsHistoricalOnly,
                target.MainImageId,
                target.ZoneId,
                target.LifecycleStatus,
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
