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
            PassportRideOccurrenceResultFactory.Create(occurrence, target));
    }
}

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
                    return PassportRideOccurrenceResultFactory.Create(occurrence, target);
                }).ToArray(),
                page.NextCursor));
    }
}

public sealed class ReorderRideOccurrenceCommandHandler
    : ICommandHandler<ReorderRideOccurrenceCommand, ApplicationResult<ReorderRideOccurrenceResult>>
{
    private readonly IUserVisitRepository visitRepository;
    private readonly IRideOccurrenceRepository occurrenceRepository;
    private readonly IPassportClock clock;
    private readonly IPassportAuditPublisher? auditPublisher;
    private readonly IVisitContentMutationLeaseManager? contentMutationLeaseManager;

    internal ReorderRideOccurrenceCommandHandler(
        IUserVisitRepository visitRepository,
        IRideOccurrenceRepository occurrenceRepository,
        IPassportClock clock)
        : this(visitRepository, occurrenceRepository, clock, null!, null!)
    {
    }

    public ReorderRideOccurrenceCommandHandler(
        IUserVisitRepository visitRepository,
        IRideOccurrenceRepository occurrenceRepository,
        IPassportClock clock,
        IPassportAuditPublisher auditPublisher,
        IVisitContentMutationLeaseManager contentMutationLeaseManager)
    {
        this.visitRepository = visitRepository;
        this.occurrenceRepository = occurrenceRepository;
        this.clock = clock;
        this.auditPublisher = auditPublisher;
        this.contentMutationLeaseManager = contentMutationLeaseManager;
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
            return Failure(operationId is null
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
        Visit? visit = null;
        IVisitContentMutationLease? contentMutationLease = null;
        if (this.contentMutationLeaseManager is not null)
        {
            visit = await this.visitRepository.GetOwnedAsync(
                scope.VisitId,
                scope.UserId,
                cancellationToken);
            if (visit is null)
            {
                return Failure(PassportApplicationErrors.VisitNotFound());
            }

            ApplicationError? currentEditableError =
                PassportRideOccurrenceHandlerSupport.ValidateEditable(visit);
            if (currentEditableError is not null)
            {
                return Failure(currentEditableError);
            }

            contentMutationLease = await this.contentMutationLeaseManager.TryAcquireAsync(
                visit,
                this.clock.UtcNow,
                cancellationToken);
            if (contentMutationLease is null)
            {
                return Failure(PassportApplicationErrors.RideOccurrenceConcurrencyConflict());
            }
        }

        await using IVisitContentMutationLease? contentMutationLeaseScope =
            contentMutationLease;
        using CancellationTokenSource? leaseCancellationSource =
            PassportLeaseCancellation.Link(contentMutationLease, cancellationToken);
        CancellationToken guardedCancellationToken =
            leaseCancellationSource?.Token ?? cancellationToken;
        request = RideOccurrenceFencedPersistence.Attach(request, contentMutationLease);
        IdempotentRideOccurrenceReorderResult? existing =
            await this.occurrenceRepository.ResolveExistingReorderAsync(
                request,
                operationId,
                guardedCancellationToken);
        if (existing is not null)
        {
            return PassportContentMutationLeaseCompletion.Complete(
                contentMutationLease,
                ToApplicationResult(existing));
        }

        visit ??= await this.visitRepository.GetOwnedAsync(
            scope.VisitId,
            scope.UserId,
            guardedCancellationToken);
        if (visit is null)
        {
            return PassportContentMutationLeaseCompletion.Complete(
                contentMutationLease,
                Failure(PassportApplicationErrors.VisitNotFound()));
        }

        ApplicationError? editableError = PassportRideOccurrenceHandlerSupport.ValidateEditable(visit);
        if (editableError is not null)
        {
            return PassportContentMutationLeaseCompletion.Complete(
                contentMutationLease,
                Failure(editableError));
        }

        IReadOnlyList<RideOccurrence> occurrences;
        try
        {
            occurrences = await RideOccurrenceOrderLoader.LoadAllAsync(
                this.occurrenceRepository,
                scope.VisitId,
                scope.UserId,
                guardedCancellationToken);
        }
        catch (InvalidOperationException)
        {
            return PassportContentMutationLeaseCompletion.Complete(
                contentMutationLease,
                Failure(PassportApplicationErrors.InvalidRideOccurrenceReorder()));
        }

        RideOccurrence? moved = occurrences.FirstOrDefault(
            occurrence => occurrence.Id == scope.OccurrenceId);
        if (moved is null)
        {
            return PassportContentMutationLeaseCompletion.Complete(
                contentMutationLease,
                Failure(PassportApplicationErrors.RideOccurrenceNotFound()));
        }

        if (moved.Version != command.ExpectedVersion)
        {
            return PassportContentMutationLeaseCompletion.Complete(
                contentMutationLease,
                Failure(PassportApplicationErrors.RideOccurrenceConcurrencyConflict()));
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
            return PassportContentMutationLeaseCompletion.Complete(
                contentMutationLease,
                Failure(PassportApplicationErrors.InvalidRideOccurrenceReorder()));
        }

        Dictionary<RideOccurrenceId, RideOccurrence> byId = occurrences.ToDictionary(
            static occurrence => occurrence.Id);
        DateTime nowUtc = this.clock.UtcNow;
        List<RideOccurrenceVersionedChange> changes = new List<RideOccurrenceVersionedChange>();
        List<PassportAuditEvent> auditEvents = new List<PassportAuditEvent>();
        foreach (RideOccurrenceOrderPosition position in plan.Changes)
        {
            RideOccurrence occurrence = byId[position.OccurrenceId];
            long expectedVersion = occurrence.Version;
            long previousSortPosition = occurrence.SortPosition;
            RideOccurrenceAuditSnapshot previous =
                RideOccurrenceAuditSnapshot.Capture(occurrence);
            occurrence.MoveTo(position.SortPosition, nowUtc);
            changes.Add(new RideOccurrenceVersionedChange(
                occurrence,
                expectedVersion,
                previousSortPosition));
            if (this.auditPublisher is not null)
            {
                auditEvents.Add(PassportRideAuditEventFactory.RideOccurrenceChanged(
                    occurrence,
                    previous,
                    operationId));
            }
        }

        IdempotentRideOccurrenceReorderResult result = this.auditPublisher is null
            ? await this.occurrenceRepository.ReorderIdempotentAsync(
                request,
                changes,
                plan.Guards,
                byId[scope.OccurrenceId],
                plan.WasNormalized,
                nowUtc,
                operationId,
                null,
                guardedCancellationToken)
            : await this.occurrenceRepository.ReorderIdempotentAuditedAsync(
                request,
                changes,
                plan.Guards,
                byId[scope.OccurrenceId],
                plan.WasNormalized,
                nowUtc,
                operationId,
                null,
                auditEvents,
                guardedCancellationToken);
        if (result.Status is IdempotentRideOccurrenceReorderStatus.Applied
            or IdempotentRideOccurrenceReorderStatus.Replayed)
        {
            await PassportAuditDelivery.PublishAsync(
                this.auditPublisher,
                auditEvents,
                guardedCancellationToken);
        }

        return PassportContentMutationLeaseCompletion.Complete(
            contentMutationLease,
            ToApplicationResult(result));
    }

    private static ApplicationResult<ReorderRideOccurrenceResult> ToApplicationResult(
        IdempotentRideOccurrenceReorderResult result)
    {
        if (result.Status == IdempotentRideOccurrenceReorderStatus.IdempotencyConflict)
        {
            return Failure(PassportApplicationErrors.RideOccurrenceIdempotencyConflict());
        }

        if (result.Status == IdempotentRideOccurrenceReorderStatus.Conflict
            || result.Occurrence is null)
        {
            return Failure(PassportApplicationErrors.RideOccurrenceConcurrencyConflict());
        }

        return ApplicationResult<ReorderRideOccurrenceResult>.Success(
            new ReorderRideOccurrenceResult(
                PassportRideOccurrenceResultFactory.Create(result.Occurrence),
                result.Status == IdempotentRideOccurrenceReorderStatus.Replayed,
                result.WasNormalized));
    }

    private static ApplicationResult<ReorderRideOccurrenceResult> Failure(
        ApplicationError error)
    {
        return ApplicationResult<ReorderRideOccurrenceResult>.Failure(error);
    }
}
