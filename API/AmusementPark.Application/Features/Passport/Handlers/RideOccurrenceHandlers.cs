using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Handlers;

public sealed class AddRideOccurrencesBatchCommandHandler
    : ICommandHandler<AddRideOccurrencesBatchCommand, ApplicationResult<CreateRideOccurrencesResult>>
{
    private readonly IUserVisitRepository visitRepository;
    private readonly IRideOccurrenceRepository occurrenceRepository;
    private readonly IVisitTargetResolver targetResolver;
    private readonly IPassportClock clock;

    public AddRideOccurrencesBatchCommandHandler(
        IUserVisitRepository visitRepository,
        IRideOccurrenceRepository occurrenceRepository,
        IVisitTargetResolver targetResolver,
        IPassportClock clock)
    {
        this.visitRepository = visitRepository;
        this.occurrenceRepository = occurrenceRepository;
        this.targetResolver = targetResolver;
        this.clock = clock;
    }

    public async Task<ApplicationResult<CreateRideOccurrencesResult>> HandleAsync(
        AddRideOccurrencesBatchCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!PassportRideOccurrenceHandlerSupport.TryNormalizeRequestScope(
            command.UserId,
            command.VisitId,
            out string userId,
            out VisitId visitId))
        {
            return ApplicationResult<CreateRideOccurrencesResult>.Failure(
                PassportApplicationErrors.VisitNotFound());
        }

        string? operationId = PassportRideOccurrenceHandlerSupport.NormalizeOperationId(
            command.ClientOperationId);
        IReadOnlyList<RideOccurrenceCreationItem>? expanded = Expand(command.Items);
        if (operationId is null || expanded is null)
        {
            return ApplicationResult<CreateRideOccurrencesResult>.Failure(
                operationId is null
                    ? PassportApplicationErrors.InvalidIdempotencyKey()
                    : PassportApplicationErrors.InvalidRideOccurrenceBatch());
        }

        Visit? visit = await this.visitRepository.GetOwnedAsync(
            visitId,
            userId,
            cancellationToken);
        if (visit is null)
        {
            return ApplicationResult<CreateRideOccurrencesResult>.Failure(
                PassportApplicationErrors.VisitNotFound());
        }

        IReadOnlyDictionary<string, VisitTarget> targets = await this.targetResolver.ResolveAsync(
            expanded.Select(static item => item.ParkItemId.Trim()).Distinct(StringComparer.Ordinal).ToArray(),
            cancellationToken);
        ApplicationError? targetError = PassportRideOccurrenceHandlerSupport.ValidateTargets(
            visit,
            expanded,
            targets);
        if (targetError is not null)
        {
            return ApplicationResult<CreateRideOccurrencesResult>.Failure(targetError);
        }

        DateTime nowUtc = this.clock.UtcNow;
        List<RideOccurrence> provisionalOccurrences;
        try
        {
            provisionalOccurrences = BuildOccurrences(
                visit,
                expanded,
                targets,
                RideOccurrenceOrderPlanner.AllocateAppend(null, expanded.Count),
                nowUtc);
        }
        catch (RideOccurrenceValidationException exception)
        {
            return ApplicationResult<CreateRideOccurrencesResult>.Failure(
                PassportApplicationErrors.InvalidRideOccurrence(
                    exception.ErrorCode,
                    exception.Message));
        }
        catch (IdentifierValidationException exception)
        {
            return ApplicationResult<CreateRideOccurrencesResult>.Failure(
                PassportApplicationErrors.InvalidIdentifier(
                    exception.ErrorCode,
                    exception.Message,
                    exception.ParamName));
        }

        IdempotentRideOccurrenceCreationResult? existing =
            await this.occurrenceRepository.ResolveExistingBatchCreationAsync(
                provisionalOccurrences,
                operationId,
                cancellationToken);
        if (existing is not null)
        {
            return ToApplicationResult(existing);
        }

        long? currentMaximum = await this.occurrenceRepository.GetLastSortPositionAsync(
            visitId,
            userId,
            cancellationToken);
        IReadOnlyList<long> positions;
        try
        {
            positions = RideOccurrenceOrderPlanner.AllocateAppend(
                currentMaximum,
                expanded.Count);
        }
        catch (OverflowException)
        {
            bool normalized = await this.TryNormalizeForAppendAsync(
                visitId,
                userId,
                operationId,
                nowUtc,
                cancellationToken);
            if (!normalized)
            {
                return ApplicationResult<CreateRideOccurrencesResult>.Failure(
                    PassportApplicationErrors.RideOccurrenceConcurrencyConflict());
            }

            currentMaximum = await this.occurrenceRepository.GetLastSortPositionAsync(
                visitId,
                userId,
                cancellationToken);
            try
            {
                positions = RideOccurrenceOrderPlanner.AllocateAppend(
                    currentMaximum,
                    expanded.Count);
            }
            catch (OverflowException)
            {
                return ApplicationResult<CreateRideOccurrencesResult>.Failure(
                    PassportApplicationErrors.RideOccurrenceConcurrencyConflict());
            }
        }

        List<RideOccurrence> occurrences;
        try
        {
            occurrences = BuildOccurrences(
                visit,
                expanded,
                targets,
                positions,
                nowUtc);
        }
        catch (RideOccurrenceValidationException exception)
        {
            return ApplicationResult<CreateRideOccurrencesResult>.Failure(
                PassportApplicationErrors.InvalidRideOccurrence(
                    exception.ErrorCode,
                    exception.Message));
        }
        catch (IdentifierValidationException exception)
        {
            return ApplicationResult<CreateRideOccurrencesResult>.Failure(
                PassportApplicationErrors.InvalidIdentifier(
                    exception.ErrorCode,
                    exception.Message,
                    exception.ParamName));
        }

        IdempotentRideOccurrenceCreationResult created =
            await this.occurrenceRepository.CreateBatchIdempotentAsync(
                occurrences,
                operationId,
                cancellationToken);
        return ToApplicationResult(created);
    }

    private static List<RideOccurrence> BuildOccurrences(
        Visit visit,
        IReadOnlyList<RideOccurrenceCreationItem> items,
        IReadOnlyDictionary<string, VisitTarget> targets,
        IReadOnlyList<long> positions,
        DateTime nowUtc)
    {
        List<RideOccurrence> occurrences = new List<RideOccurrence>(items.Count);
        for (int index = 0; index < items.Count; index++)
        {
            RideOccurrenceCreationItem item = items[index];
            VisitTarget target = targets[item.ParkItemId.Trim()];
            HistoricalConsistency historicalConsistency =
                RideOccurrenceHistoricalConsistencyEvaluator.Evaluate(
                    visit.Date,
                    target.OpeningDate,
                    target.ClosingDate);
            occurrences.Add(RideOccurrence.Create(
                RideOccurrenceId.New(),
                visit,
                target.ParkItemId,
                positions[index],
                new OccurrenceMoment(item.LocalTime, item.IsApproximate),
                item.Status,
                RideLogSource.Manual,
                historicalConsistency,
                null,
                item.PrivateNote,
                nowUtc));
        }

        return occurrences;
    }

    private async Task<bool> TryNormalizeForAppendAsync(
        VisitId visitId,
        string userId,
        string clientOperationId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<RideOccurrence> occurrences;
        try
        {
            occurrences = await LoadAllAsync(
                this.occurrenceRepository,
                visitId,
                userId,
                cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return false;
        }

        if (occurrences.Count == 0)
        {
            return false;
        }

        RideOccurrenceOrderPlan plan =
            RideOccurrenceOrderPlanner.PlanNormalization(occurrences);
        Dictionary<RideOccurrenceId, RideOccurrence> byId = occurrences.ToDictionary(
            static occurrence => occurrence.Id);
        List<RideOccurrenceVersionedChange> changes = new List<RideOccurrenceVersionedChange>();
        foreach (RideOccurrenceOrderPosition position in plan.Changes)
        {
            RideOccurrence occurrence = byId[position.OccurrenceId];
            long expectedVersion = occurrence.Version;
            occurrence.MoveTo(position.SortPosition, nowUtc);
            changes.Add(new RideOccurrenceVersionedChange(occurrence, expectedVersion));
        }

        RideOccurrence last = occurrences
            .OrderBy(static occurrence => occurrence.SortPosition)
            .ThenBy(static occurrence => occurrence.CreatedAtUtc)
            .ThenBy(static occurrence => occurrence.Id.Value, StringComparer.Ordinal)
            .Last();
        RideOccurrenceReorderRequest request = new RideOccurrenceReorderRequest(
            visitId,
            userId,
            last.Id,
            changes.FirstOrDefault(change => change.Occurrence.Id == last.Id)?.ExpectedVersion
                ?? last.Version,
            null,
            RideOccurrencePlacement.Last);
        IdempotentRideOccurrenceReorderResult result =
            await this.occurrenceRepository.ReorderIdempotentAsync(
                request,
                changes,
                last,
                true,
                nowUtc,
                string.Concat("append-normalize:", clientOperationId),
                cancellationToken);
        return result.Status != IdempotentRideOccurrenceReorderStatus.Conflict;
    }

    private static async Task<IReadOnlyList<RideOccurrence>> LoadAllAsync(
        IRideOccurrenceRepository repository,
        VisitId visitId,
        string userId,
        CancellationToken cancellationToken)
    {
        List<RideOccurrence> occurrences = new List<RideOccurrence>();
        RideOccurrenceListCursor? cursor = null;
        do
        {
            RideOccurrencePage page = await repository.ListOwnedByVisitAsync(
                new RideOccurrenceListCriteria(
                    visitId,
                    userId,
                    RideOccurrenceListCriteria.MaximumLimit,
                    cursor),
                cancellationToken);
            occurrences.AddRange(page.Items);
            cursor = page.NextCursor;
            if (occurrences.Count > RideOccurrenceOrderPlanner.MaximumReorderSize)
            {
                throw new InvalidOperationException(
                    "The visit exceeds the supported bounded reorder size.");
            }
        }
        while (cursor is not null);

        return occurrences;
    }

    private static IReadOnlyList<RideOccurrenceCreationItem>? Expand(
        IReadOnlyCollection<RideOccurrenceCreationItem>? items)
    {
        if (items is null || items.Count is < 1 or > 100)
        {
            return null;
        }

        List<RideOccurrenceCreationItem> expanded = new List<RideOccurrenceCreationItem>();
        foreach (RideOccurrenceCreationItem item in items)
        {
            if (item is null
                || string.IsNullOrWhiteSpace(item.ParkItemId)
                || item.Count is < 1 or > 100
                || !Enum.IsDefined(item.Status)
                || expanded.Count + item.Count > 100)
            {
                return null;
            }

            for (int index = 0; index < item.Count; index++)
            {
                expanded.Add(item with { Count = 1 });
            }
        }

        return expanded;
    }

    private static ApplicationResult<CreateRideOccurrencesResult> ToApplicationResult(
        IdempotentRideOccurrenceCreationResult result)
    {
        if (result.Status == IdempotentRideOccurrenceCreationStatus.Conflict)
        {
            return ApplicationResult<CreateRideOccurrencesResult>.Failure(
                PassportApplicationErrors.RideOccurrenceIdempotencyConflict());
        }

        CreateRideOccurrencesResult value = new CreateRideOccurrencesResult(
            result.Occurrences.Select(PassportRideOccurrenceResultFactory.Create).ToArray(),
            result.Status == IdempotentRideOccurrenceCreationStatus.Replayed);
        return ApplicationResult<CreateRideOccurrencesResult>.Success(value);
    }
}

public sealed class UpdateRideOccurrenceCommandHandler
    : ICommandHandler<UpdateRideOccurrenceCommand, ApplicationResult<RideOccurrenceResult>>
{
    private readonly IUserVisitRepository visitRepository;
    private readonly IRideOccurrenceRepository occurrenceRepository;
    private readonly IVisitTargetResolver targetResolver;
    private readonly IPassportClock clock;

    public UpdateRideOccurrenceCommandHandler(
        IUserVisitRepository visitRepository,
        IRideOccurrenceRepository occurrenceRepository,
        IVisitTargetResolver targetResolver,
        IPassportClock clock)
    {
        this.visitRepository = visitRepository;
        this.occurrenceRepository = occurrenceRepository;
        this.targetResolver = targetResolver;
        this.clock = clock;
    }

    public async Task<ApplicationResult<RideOccurrenceResult>> HandleAsync(
        UpdateRideOccurrenceCommand command,
        CancellationToken cancellationToken = default)
    {
        ParsedOccurrenceScope? scope = PassportRideOccurrenceHandlerSupport.ParseOccurrenceScope(
            command.UserId,
            command.VisitId,
            command.OccurrenceId);
        if (scope is null || command.ExpectedVersion < 1 || !Enum.IsDefined(command.Status))
        {
            return ApplicationResult<RideOccurrenceResult>.Failure(
                PassportApplicationErrors.RideOccurrenceNotFound());
        }

        Visit? visit = await this.visitRepository.GetOwnedAsync(
            scope.VisitId,
            scope.UserId,
            cancellationToken);
        RideOccurrence? occurrence = await this.occurrenceRepository.GetOwnedAsync(
            scope.OccurrenceId,
            scope.VisitId,
            scope.UserId,
            cancellationToken);
        if (visit is null || occurrence is null)
        {
            return ApplicationResult<RideOccurrenceResult>.Failure(
                occurrence is null
                    ? PassportApplicationErrors.RideOccurrenceNotFound()
                    : PassportApplicationErrors.VisitNotFound());
        }

        if (occurrence.Version != command.ExpectedVersion)
        {
            return ApplicationResult<RideOccurrenceResult>.Failure(
                PassportApplicationErrors.RideOccurrenceConcurrencyConflict());
        }

        IReadOnlyDictionary<string, VisitTarget> targets = await this.targetResolver.ResolveAsync(
            new[] { occurrence.ParkItemId },
            cancellationToken);
        if (!targets.TryGetValue(occurrence.ParkItemId, out VisitTarget? target))
        {
            return ApplicationResult<RideOccurrenceResult>.Failure(
                PassportApplicationErrors.VisitTargetNotFound());
        }

        if (!string.Equals(target.ParkId, visit.ParkId, StringComparison.Ordinal))
        {
            return ApplicationResult<RideOccurrenceResult>.Failure(
                PassportApplicationErrors.VisitTargetParkMismatch());
        }

        if (target.Category != ParkItemCategory.Attraction)
        {
            return ApplicationResult<RideOccurrenceResult>.Failure(
                PassportApplicationErrors.VisitTargetNotAttraction());
        }

        HistoricalConsistency consistency =
            RideOccurrenceHistoricalConsistencyEvaluator.Evaluate(
                visit.Date,
                target.OpeningDate,
                target.ClosingDate);
        if (consistency == HistoricalConsistency.ConfirmedConflict
            && !command.ConfirmHistoricalConflict)
        {
            return ApplicationResult<RideOccurrenceResult>.Failure(
                PassportApplicationErrors.HistoricalConflictConfirmationRequired());
        }

        long expectedVersion = occurrence.Version;
        try
        {
            occurrence.Update(
                visit,
                new OccurrenceMoment(command.LocalTime, command.IsApproximate),
                command.Status,
                consistency,
                null,
                command.PrivateNote,
                this.clock.UtcNow);
        }
        catch (RideOccurrenceValidationException exception)
        {
            return ApplicationResult<RideOccurrenceResult>.Failure(
                PassportApplicationErrors.InvalidRideOccurrence(
                    exception.ErrorCode,
                    exception.Message));
        }

        if (occurrence.Version == expectedVersion)
        {
            return ApplicationResult<RideOccurrenceResult>.Success(
                PassportRideOccurrenceResultFactory.Create(occurrence));
        }

        bool updated = await this.occurrenceRepository.TryUpdateOwnedAsync(
            occurrence,
            expectedVersion,
            cancellationToken);
        return updated
            ? ApplicationResult<RideOccurrenceResult>.Success(
                PassportRideOccurrenceResultFactory.Create(occurrence))
            : ApplicationResult<RideOccurrenceResult>.Failure(
                PassportApplicationErrors.RideOccurrenceConcurrencyConflict());
    }
}

public sealed class DeleteRideOccurrenceCommandHandler
    : ICommandHandler<DeleteRideOccurrenceCommand, ApplicationResult<RideOccurrenceResult>>
{
    private readonly IRideOccurrenceRepository occurrenceRepository;
    private readonly IPassportClock clock;

    public DeleteRideOccurrenceCommandHandler(
        IRideOccurrenceRepository occurrenceRepository,
        IPassportClock clock)
    {
        this.occurrenceRepository = occurrenceRepository;
        this.clock = clock;
    }

    public async Task<ApplicationResult<RideOccurrenceResult>> HandleAsync(
        DeleteRideOccurrenceCommand command,
        CancellationToken cancellationToken = default)
    {
        ParsedOccurrenceScope? scope = PassportRideOccurrenceHandlerSupport.ParseOccurrenceScope(
            command.UserId,
            command.VisitId,
            command.OccurrenceId);
        if (scope is null || command.ExpectedVersion < 1)
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

        if (occurrence.Version != command.ExpectedVersion)
        {
            return ApplicationResult<RideOccurrenceResult>.Failure(
                PassportApplicationErrors.RideOccurrenceConcurrencyConflict());
        }

        long expectedVersion = occurrence.Version;
        occurrence.Delete(this.clock.UtcNow);
        bool deleted = await this.occurrenceRepository.TryUpdateOwnedAsync(
            occurrence,
            expectedVersion,
            cancellationToken);
        return deleted
            ? ApplicationResult<RideOccurrenceResult>.Success(
                PassportRideOccurrenceResultFactory.Create(occurrence))
            : ApplicationResult<RideOccurrenceResult>.Failure(
                PassportApplicationErrors.RideOccurrenceConcurrencyConflict());
    }
}

public sealed class ListRideOccurrencesQueryHandler
    : IQueryHandler<ListRideOccurrencesQuery, ApplicationResult<RideOccurrencePageResult>>
{
    private readonly IUserVisitRepository visitRepository;
    private readonly IRideOccurrenceRepository occurrenceRepository;

    public ListRideOccurrencesQueryHandler(
        IUserVisitRepository visitRepository,
        IRideOccurrenceRepository occurrenceRepository)
    {
        this.visitRepository = visitRepository;
        this.occurrenceRepository = occurrenceRepository;
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
        return ApplicationResult<RideOccurrencePageResult>.Success(
            new RideOccurrencePageResult(
                page.Items.Select(PassportRideOccurrenceResultFactory.Create).ToArray(),
                page.NextCursor));
    }
}

public sealed class ReorderRideOccurrenceCommandHandler
    : ICommandHandler<ReorderRideOccurrenceCommand, ApplicationResult<ReorderRideOccurrenceResult>>
{
    private readonly IUserVisitRepository visitRepository;
    private readonly IRideOccurrenceRepository occurrenceRepository;
    private readonly IPassportClock clock;

    public ReorderRideOccurrenceCommandHandler(
        IUserVisitRepository visitRepository,
        IRideOccurrenceRepository occurrenceRepository,
        IPassportClock clock)
    {
        this.visitRepository = visitRepository;
        this.occurrenceRepository = occurrenceRepository;
        this.clock = clock;
    }

    public async Task<ApplicationResult<ReorderRideOccurrenceResult>> HandleAsync(
        ReorderRideOccurrenceCommand command,
        CancellationToken cancellationToken = default)
    {
        ParsedOccurrenceScope? scope = PassportRideOccurrenceHandlerSupport.ParseOccurrenceScope(
            command.UserId,
            command.VisitId,
            command.OccurrenceId);
        string? operationId = PassportRideOccurrenceHandlerSupport.NormalizeOperationId(
            command.ClientOperationId);
        RideOccurrenceId? anchorId = PassportRideOccurrenceHandlerSupport.ParseOptionalOccurrenceId(
            command.AnchorOccurrenceId);
        bool anchorShapeValid = command.Placement is RideOccurrencePlacement.Before
            or RideOccurrencePlacement.After
            ? anchorId.HasValue
            : string.IsNullOrWhiteSpace(command.AnchorOccurrenceId);
        if (scope is null
            || operationId is null
            || command.ExpectedVersion < 1
            || !Enum.IsDefined(command.Placement)
            || !anchorShapeValid
            || anchorId == scope.OccurrenceId)
        {
            return ApplicationResult<ReorderRideOccurrenceResult>.Failure(
                operationId is null
                    ? PassportApplicationErrors.InvalidIdempotencyKey()
                    : PassportApplicationErrors.InvalidRideOccurrenceReorder());
        }

        RideOccurrenceReorderRequest request = new RideOccurrenceReorderRequest(
            scope.VisitId,
            scope.UserId,
            scope.OccurrenceId,
            command.ExpectedVersion,
            anchorId,
            command.Placement);
        IdempotentRideOccurrenceReorderResult? existing =
            await this.occurrenceRepository.ResolveExistingReorderAsync(
                request,
                operationId,
                cancellationToken);
        if (existing is not null)
        {
            return ToApplicationResult(existing);
        }

        Visit? visit = await this.visitRepository.GetOwnedAsync(
            scope.VisitId,
            scope.UserId,
            cancellationToken);
        if (visit is null)
        {
            return ApplicationResult<ReorderRideOccurrenceResult>.Failure(
                PassportApplicationErrors.VisitNotFound());
        }

        IReadOnlyList<RideOccurrence> occurrences = await this.LoadAllAsync(
            scope.VisitId,
            scope.UserId,
            cancellationToken);
        RideOccurrence? moved = occurrences.FirstOrDefault(
            occurrence => occurrence.Id == scope.OccurrenceId);
        if (moved is null)
        {
            return ApplicationResult<ReorderRideOccurrenceResult>.Failure(
                PassportApplicationErrors.RideOccurrenceNotFound());
        }

        if (moved.Version != command.ExpectedVersion)
        {
            return ApplicationResult<ReorderRideOccurrenceResult>.Failure(
                PassportApplicationErrors.RideOccurrenceConcurrencyConflict());
        }

        RideOccurrenceOrderPlan plan;
        try
        {
            plan = RideOccurrenceOrderPlanner.PlanMove(
                occurrences,
                scope.OccurrenceId,
                anchorId,
                command.Placement);
        }
        catch (Exception exception) when (
            exception is ArgumentException
            or KeyNotFoundException
            or OverflowException)
        {
            return ApplicationResult<ReorderRideOccurrenceResult>.Failure(
                PassportApplicationErrors.InvalidRideOccurrenceReorder());
        }

        Dictionary<RideOccurrenceId, RideOccurrence> byId = occurrences.ToDictionary(
            static occurrence => occurrence.Id);
        DateTime nowUtc = this.clock.UtcNow;
        List<RideOccurrenceVersionedChange> changes = new List<RideOccurrenceVersionedChange>();
        foreach (RideOccurrenceOrderPosition position in plan.Changes)
        {
            RideOccurrence occurrence = byId[position.OccurrenceId];
            long expectedVersion = occurrence.Version;
            occurrence.MoveTo(position.SortPosition, nowUtc);
            changes.Add(new RideOccurrenceVersionedChange(occurrence, expectedVersion));
        }

        IdempotentRideOccurrenceReorderResult result =
            await this.occurrenceRepository.ReorderIdempotentAsync(
                request,
                changes,
                byId[scope.OccurrenceId],
                plan.WasNormalized,
                nowUtc,
                operationId,
                cancellationToken);
        return ToApplicationResult(result);
    }

    private async Task<IReadOnlyList<RideOccurrence>> LoadAllAsync(
        VisitId visitId,
        string userId,
        CancellationToken cancellationToken)
    {
        List<RideOccurrence> occurrences = new List<RideOccurrence>();
        RideOccurrenceListCursor? cursor = null;
        do
        {
            RideOccurrencePage page = await this.occurrenceRepository.ListOwnedByVisitAsync(
                new RideOccurrenceListCriteria(
                    visitId,
                    userId,
                    RideOccurrenceListCriteria.MaximumLimit,
                    cursor),
                cancellationToken);
            occurrences.AddRange(page.Items);
            cursor = page.NextCursor;
            if (occurrences.Count > RideOccurrenceOrderPlanner.MaximumReorderSize)
            {
                throw new InvalidOperationException(
                    "The visit exceeds the supported bounded reorder size.");
            }
        }
        while (cursor is not null);

        return occurrences;
    }

    private static ApplicationResult<ReorderRideOccurrenceResult> ToApplicationResult(
        IdempotentRideOccurrenceReorderResult result)
    {
        if (result.Status == IdempotentRideOccurrenceReorderStatus.Conflict
            || result.Occurrence is null)
        {
            return ApplicationResult<ReorderRideOccurrenceResult>.Failure(
                PassportApplicationErrors.RideOccurrenceConcurrencyConflict());
        }

        return ApplicationResult<ReorderRideOccurrenceResult>.Success(
            new ReorderRideOccurrenceResult(
                PassportRideOccurrenceResultFactory.Create(result.Occurrence),
                result.Status == IdempotentRideOccurrenceReorderStatus.Replayed,
                result.WasNormalized));
    }
}

internal sealed record ParsedOccurrenceScope(
    string UserId,
    VisitId VisitId,
    RideOccurrenceId OccurrenceId);

internal static class PassportRideOccurrenceHandlerSupport
{
    public static bool TryNormalizeRequestScope(
        string? userId,
        string? visitIdValue,
        out string normalizedUserId,
        out VisitId visitId)
    {
        normalizedUserId = userId?.Trim() ?? string.Empty;
        visitId = default;
        if (normalizedUserId.Length == 0)
        {
            return false;
        }

        try
        {
            visitId = VisitId.Parse(visitIdValue);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public static ParsedOccurrenceScope? ParseOccurrenceScope(
        string? userId,
        string? visitIdValue,
        string? occurrenceIdValue)
    {
        if (!TryNormalizeRequestScope(
            userId,
            visitIdValue,
            out string normalizedUserId,
            out VisitId visitId))
        {
            return null;
        }

        try
        {
            return new ParsedOccurrenceScope(
                normalizedUserId,
                visitId,
                RideOccurrenceId.Parse(occurrenceIdValue));
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    public static RideOccurrenceId? ParseOptionalOccurrenceId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            return RideOccurrenceId.Parse(value);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    public static string? NormalizeOperationId(string? value)
    {
        string normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length is < 1 or > CreateVisitCommandHandler.MaximumClientOperationIdLength
            || normalized.Any(char.IsControl))
        {
            return null;
        }

        return normalized;
    }

    public static ApplicationError? ValidateTargets(
        Visit visit,
        IReadOnlyCollection<RideOccurrenceCreationItem> items,
        IReadOnlyDictionary<string, VisitTarget> targets)
    {
        foreach (RideOccurrenceCreationItem item in items)
        {
            string parkItemId = item.ParkItemId.Trim();
            if (!targets.TryGetValue(parkItemId, out VisitTarget? target))
            {
                return PassportApplicationErrors.VisitTargetNotFound();
            }

            if (!string.Equals(target.ParkId, visit.ParkId, StringComparison.Ordinal))
            {
                return PassportApplicationErrors.VisitTargetParkMismatch();
            }

            if (target.Category != ParkItemCategory.Attraction)
            {
                return PassportApplicationErrors.VisitTargetNotAttraction();
            }

            HistoricalConsistency consistency =
                RideOccurrenceHistoricalConsistencyEvaluator.Evaluate(
                    visit.Date,
                    target.OpeningDate,
                    target.ClosingDate);
            if (consistency == HistoricalConsistency.ConfirmedConflict
                && !item.ConfirmHistoricalConflict)
            {
                return PassportApplicationErrors.HistoricalConflictConfirmationRequired();
            }
        }

        return null;
    }
}
