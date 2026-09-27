using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Handlers;

public sealed class ListVisitHistoricalRideTargetsQueryHandler
    : IQueryHandler<
        ListVisitHistoricalRideTargetsQuery,
        ApplicationResult<VisitHistoricalRideTargetPageResult>>
{
    public const int MaximumPageSize = 50;

    private readonly IUserVisitRepository visitRepository;
    private readonly IPassportHistoricalTargetResolver targetResolver;

    public ListVisitHistoricalRideTargetsQueryHandler(
        IUserVisitRepository visitRepository,
        IPassportHistoricalTargetResolver targetResolver)
    {
        this.visitRepository = visitRepository;
        this.targetResolver = targetResolver;
    }

    public async Task<ApplicationResult<VisitHistoricalRideTargetPageResult>> HandleAsync(
        ListVisitHistoricalRideTargetsQuery query,
        CancellationToken cancellationToken = default)
    {
        string userId = query.UserId?.Trim() ?? string.Empty;
        if (userId.Length == 0
            || query.Page < 1
            || query.PageSize is < 1 or > MaximumPageSize
            || !Enum.IsDefined(query.Scope))
        {
            return Failure(PassportApplicationErrors.InvalidRideOccurrenceListLimit());
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

        Visit? visit = await this.visitRepository.GetOwnedAsync(visitId, userId, cancellationToken);
        if (visit is null)
        {
            return Failure(PassportApplicationErrors.VisitNotFound());
        }

        PassportHistoricalTargetContext context =
            await this.targetResolver.ResolveAllAsync(visit, cancellationToken);
        PassportHistoricalTarget[] allTargets = context.Targets.Values.ToArray();
        int knownOpenCount = allTargets.Count(static target =>
            target.OperationalState == HistoricalOperationalState.KnownOpen);
        int possiblyOpenCount = allTargets.Count(static target =>
            target.OperationalState == HistoricalOperationalState.PossiblyOpen);
        string search = query.Search?.Trim() ?? string.Empty;
        string zoneId = query.ZoneId?.Trim() ?? string.Empty;
        PassportHistoricalTarget[] filtered = allTargets
            .Where(target => MatchesScope(target, query.Scope)
                && (search.Length == 0
                    || target.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                && (zoneId.Length == 0
                    || string.Equals(target.ZoneId, zoneId, StringComparison.Ordinal)))
            .OrderBy(static target => AvailabilityOrder(target.OperationalState))
            .ThenBy(static target => target.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static target => target.ParkItemId, StringComparer.Ordinal)
            .ToArray();
        int totalPages = filtered.Length == 0
            ? 1
            : (int)Math.Ceiling(filtered.Length / (double)query.PageSize);
        PassportHistoricalTarget[] pageTargets = filtered
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToArray();
        IReadOnlyDictionary<string, string> imageIds =
            await this.targetResolver.ResolveMainImageIdsAsync(
                pageTargets.Select(static target => target.ParkItemId).ToArray(),
                cancellationToken);
        VisitRideTargetEvaluationResult[] items = pageTargets
            .Select(target => target with
            {
                MainImageId = imageIds.GetValueOrDefault(target.ParkItemId),
            })
            .Select(ToResult)
            .ToArray();

        return ApplicationResult<VisitHistoricalRideTargetPageResult>.Success(
            new VisitHistoricalRideTargetPageResult(
                items,
                query.Page,
                query.PageSize,
                filtered.Length,
                totalPages,
                knownOpenCount,
                possiblyOpenCount,
                allTargets.Length,
                context.CoverageStatus,
                context.CoveragePercent,
                context.MethodologyVersion));
    }

    private static bool MatchesScope(
        PassportHistoricalTarget target,
        VisitHistoricalTargetScope scope)
    {
        return scope switch
        {
            VisitHistoricalTargetScope.KnownOpen =>
                target.OperationalState == HistoricalOperationalState.KnownOpen,
            VisitHistoricalTargetScope.PossiblyOpen =>
                target.OperationalState == HistoricalOperationalState.PossiblyOpen,
            VisitHistoricalTargetScope.AllHistory => true,
            _ => false,
        };
    }

    private static int AvailabilityOrder(HistoricalOperationalState state)
    {
        return state switch
        {
            HistoricalOperationalState.KnownOpen => 0,
            HistoricalOperationalState.PossiblyOpen => 1,
            HistoricalOperationalState.Unknown => 2,
            HistoricalOperationalState.KnownClosed => 3,
            _ => 4,
        };
    }

    private static VisitRideTargetEvaluationResult ToResult(PassportHistoricalTarget target)
    {
        return new VisitRideTargetEvaluationResult(
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
            target.ClosingDate);
    }

    private static ApplicationResult<VisitHistoricalRideTargetPageResult> Failure(
        ApplicationError error)
    {
        return ApplicationResult<VisitHistoricalRideTargetPageResult>.Failure(error);
    }
}
