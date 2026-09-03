using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Handlers;

public sealed class AddRideOccurrencesBatchCommandHandler
    : ICommandHandler<AddRideOccurrencesBatchCommand, ApplicationResult<CreateRideOccurrencesResult>>
{
    private readonly IUserVisitRepository visitRepository;
    private readonly IRideOccurrenceRepository occurrenceRepository;
    private readonly IVisitTargetResolver targetResolver;
    private readonly RideOccurrenceAppendOrderNormalizer appendOrderNormalizer;
    private readonly IPassportClock clock;
    private readonly IPassportAuditPublisher? auditPublisher;
    private readonly IVisitContentMutationLeaseManager? contentMutationLeaseManager;

    internal AddRideOccurrencesBatchCommandHandler(
        IUserVisitRepository visitRepository,
        IRideOccurrenceRepository occurrenceRepository,
        IVisitTargetResolver targetResolver,
        RideOccurrenceAppendOrderNormalizer appendOrderNormalizer,
        IPassportClock clock)
        : this(
            visitRepository,
            occurrenceRepository,
            targetResolver,
            appendOrderNormalizer,
            clock,
            null!,
            null!)
    {
    }

    public AddRideOccurrencesBatchCommandHandler(
        IUserVisitRepository visitRepository,
        IRideOccurrenceRepository occurrenceRepository,
        IVisitTargetResolver targetResolver,
        RideOccurrenceAppendOrderNormalizer appendOrderNormalizer,
        IPassportClock clock,
        IPassportAuditPublisher auditPublisher,
        IVisitContentMutationLeaseManager contentMutationLeaseManager)
    {
        this.visitRepository = visitRepository;
        this.occurrenceRepository = occurrenceRepository;
        this.targetResolver = targetResolver;
        this.appendOrderNormalizer = appendOrderNormalizer;
        this.clock = clock;
        this.auditPublisher = auditPublisher;
        this.contentMutationLeaseManager = contentMutationLeaseManager;
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
            return Failure(PassportApplicationErrors.VisitNotFound());
        }

        string? operationId = PassportRideOccurrenceHandlerSupport.NormalizeOperationId(
            command.ClientOperationId);
        IReadOnlyList<RideOccurrenceCreationItem>? expanded = Expand(command.Items);
        if (operationId is null || expanded is null)
        {
            return Failure(operationId is null
                ? PassportApplicationErrors.InvalidIdempotencyKey()
                : PassportApplicationErrors.InvalidRideOccurrenceBatch());
        }

        RideOccurrenceCreationRequest creationRequest = new RideOccurrenceCreationRequest(
            visitId,
            userId,
            expanded.Select(static item => new RideOccurrenceCreationRequestItem(
                item.ParkItemId.Trim(),
                new OccurrenceMoment(item.LocalTime, item.IsApproximate),
                item.Status,
                RideLogSource.Manual,
                NormalizePrivateNote(item.PrivateNote),
                item.ConfirmHistoricalConflict)).ToArray());
        IdempotentRideOccurrenceCreationResult? existing =
            await this.occurrenceRepository.ResolveExistingBatchCreationAsync(
                creationRequest,
                operationId,
                cancellationToken);
        if (existing is not null)
        {
            return ToApplicationResult(existing);
        }

        RideOccurrenceCreationKeyReservationResult reservation =
            await this.occurrenceRepository.ResolveBatchCreationKeyReservationAsync(
                creationRequest,
                operationId,
                cancellationToken);
        if (reservation.Status == RideOccurrenceCreationKeyReservationStatus.Conflict)
        {
            return Failure(PassportApplicationErrors.RideOccurrenceIdempotencyConflict());
        }

        if (reservation.Status == RideOccurrenceCreationKeyReservationStatus.Finalized)
        {
            return await this.ResolveFinalizedCreationAsync(
                creationRequest,
                operationId,
                cancellationToken);
        }

        if (reservation.Status == RideOccurrenceCreationKeyReservationStatus.Replayed)
        {
            if (reservation.Preparation is null)
            {
                return Failure(PassportApplicationErrors.RideOccurrenceConcurrencyConflict());
            }

            return await this.CreateWithOrderRetryAsync(
                reservation.Preparation,
                expanded,
                creationRequest,
                operationId,
                cancellationToken);
        }

        Visit? visit = await this.visitRepository.GetOwnedAsync(
            visitId,
            userId,
            cancellationToken);
        if (visit is null)
        {
            return Failure(PassportApplicationErrors.VisitNotFound());
        }

        ApplicationError? editableError = PassportRideOccurrenceHandlerSupport.ValidateEditable(visit);
        if (editableError is not null)
        {
            return Failure(editableError);
        }

        IReadOnlyDictionary<string, VisitTarget> targets = await this.targetResolver.ResolveAsync(
            expanded.Select(static item => item.ParkItemId.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
            cancellationToken);
        ApplicationError? targetError = PassportRideOccurrenceHandlerSupport.ValidateTargets(
            visit,
            expanded,
            targets);
        if (targetError is not null)
        {
            return Failure(targetError);
        }

        RideOccurrenceCreationPreparation preparation = CreatePreparation(
            visit,
            expanded,
            targets);
        reservation =
            await this.occurrenceRepository.ReserveBatchCreationKeyAsync(
                creationRequest,
                preparation,
                operationId,
                this.clock.UtcNow,
                cancellationToken);
        if (reservation.Status == RideOccurrenceCreationKeyReservationStatus.Conflict)
        {
            return Failure(PassportApplicationErrors.RideOccurrenceIdempotencyConflict());
        }

        if (reservation.Status == RideOccurrenceCreationKeyReservationStatus.Finalized)
        {
            return await this.ResolveFinalizedCreationAsync(
                creationRequest,
                operationId,
                cancellationToken);
        }

        if (reservation.Preparation is null)
        {
            return Failure(PassportApplicationErrors.RideOccurrenceConcurrencyConflict());
        }

        return await this.CreateWithOrderRetryAsync(
            reservation.Preparation,
            expanded,
            creationRequest,
            operationId,
            cancellationToken);
    }

    private async Task<ApplicationResult<CreateRideOccurrencesResult>>
        ResolveFinalizedCreationAsync(
            RideOccurrenceCreationRequest creationRequest,
            string operationId,
            CancellationToken cancellationToken)
    {
        IdempotentRideOccurrenceCreationResult? existing =
            await this.occurrenceRepository.ResolveExistingBatchCreationAsync(
                creationRequest,
                operationId,
                cancellationToken);
        return existing is null
            ? Failure(PassportApplicationErrors.RideOccurrenceConcurrencyConflict())
            : ToApplicationResult(existing);
    }

    private async Task<ApplicationResult<CreateRideOccurrencesResult>> CreateWithOrderRetryAsync(
        RideOccurrenceCreationPreparation preparation,
        IReadOnlyList<RideOccurrenceCreationItem> items,
        RideOccurrenceCreationRequest creationRequest,
        string operationId,
        CancellationToken cancellationToken)
    {
        if (this.contentMutationLeaseManager is null)
        {
            return await this.CreateWithOrderRetryCoreAsync(
                preparation,
                items,
                creationRequest,
                operationId,
                cancellationToken);
        }

        Visit? currentVisit = await this.visitRepository.GetOwnedAsync(
            creationRequest.VisitId,
            creationRequest.UserId,
            cancellationToken);
        if (currentVisit is null)
        {
            return Failure(PassportApplicationErrors.VisitNotFound());
        }

        ApplicationError? editableError =
            PassportRideOccurrenceHandlerSupport.ValidateEditable(currentVisit);
        if (editableError is not null)
        {
            return Failure(editableError);
        }

        IVisitContentMutationLease? contentMutationLease =
            await this.contentMutationLeaseManager.TryAcquireAsync(
                currentVisit,
                this.clock.UtcNow,
                cancellationToken);
        if (contentMutationLease is null)
        {
            return Failure(PassportApplicationErrors.RideOccurrenceConcurrencyConflict());
        }

        await using (contentMutationLease)
        {
            return await this.CreateWithOrderRetryCoreAsync(
                preparation,
                items,
                creationRequest,
                operationId,
                cancellationToken);
        }
    }

    private async Task<ApplicationResult<CreateRideOccurrencesResult>>
        CreateWithOrderRetryCoreAsync(
            RideOccurrenceCreationPreparation preparation,
            IReadOnlyList<RideOccurrenceCreationItem> items,
            RideOccurrenceCreationRequest creationRequest,
            string operationId,
            CancellationToken cancellationToken)
    {
        const int maximumAttempts = 3;
        bool wasOrderNormalized = false;
        for (int attempt = 0; attempt < maximumAttempts; attempt++)
        {
            RideOccurrenceAppendState appendState =
                await this.occurrenceRepository.GetAppendStateAsync(
                    creationRequest.VisitId,
                    creationRequest.UserId,
                    operationId,
                    cancellationToken);
            wasOrderNormalized |= appendState.WasNormalizedForOperation;
            long? currentMaximum = appendState.LastSortPosition;
            IReadOnlyList<long> positions;
            try
            {
                positions = RideOccurrenceOrderPlanner.AllocateAppend(
                    currentMaximum,
                    items.Count);
            }
            catch (OverflowException)
            {
                bool normalized = await this.appendOrderNormalizer.TryNormalizeAsync(
                    CreateVisitContext(
                        creationRequest,
                        preparation,
                        this.clock.UtcNow),
                    operationId,
                    cancellationToken);
                if (!normalized)
                {
                    continue;
                }

                wasOrderNormalized = true;

                appendState = await this.occurrenceRepository.GetAppendStateAsync(
                    creationRequest.VisitId,
                    creationRequest.UserId,
                    operationId,
                    cancellationToken);
                wasOrderNormalized |= appendState.WasNormalizedForOperation;
                currentMaximum = appendState.LastSortPosition;
                try
                {
                    positions = RideOccurrenceOrderPlanner.AllocateAppend(
                        currentMaximum,
                        items.Count);
                }
                catch (OverflowException)
                {
                    continue;
                }
            }

            List<RideOccurrence> occurrences;
            try
            {
                occurrences = BuildOccurrences(
                    creationRequest,
                    preparation,
                    items,
                    positions,
                    this.clock.UtcNow);
            }
            catch (RideOccurrenceValidationException exception)
            {
                return Failure(PassportApplicationErrors.InvalidRideOccurrence(
                    exception.ErrorCode,
                    exception.Message));
            }
            catch (IdentifierValidationException exception)
            {
                return Failure(PassportApplicationErrors.InvalidIdentifier(
                    exception.ErrorCode,
                    exception.Message,
                    exception.ParamName));
            }

            IReadOnlyCollection<PassportAuditEvent>? auditEvents =
                this.auditPublisher is null
                    ? null
                    : occurrences
                        .Select(occurrence => PassportRideAuditEventFactory.RideOccurrenceAdded(
                            occurrence,
                            operationId))
                        .ToArray();
            IdempotentRideOccurrenceCreationResult created = auditEvents is null
                ? await this.occurrenceRepository.CreateBatchIdempotentAsync(
                    creationRequest,
                    occurrences,
                    currentMaximum,
                    wasOrderNormalized,
                    operationId,
                    cancellationToken)
                : await this.occurrenceRepository.CreateBatchIdempotentAuditedAsync(
                    creationRequest,
                    occurrences,
                    currentMaximum,
                    wasOrderNormalized,
                    operationId,
                    auditEvents,
                    cancellationToken);
            if (created.Status != IdempotentRideOccurrenceCreationStatus.ConcurrencyConflict)
            {
                if (created.Status is IdempotentRideOccurrenceCreationStatus.Created
                    or IdempotentRideOccurrenceCreationStatus.Replayed)
                {
                    IReadOnlyCollection<PassportAuditEvent> persistedEvents =
                        created.Occurrences
                            .Select(occurrence =>
                                PassportRideAuditEventFactory.RideOccurrenceAdded(
                                    occurrence,
                                    operationId))
                            .ToArray();
                    await PassportAuditDelivery.PublishAsync(
                        this.auditPublisher,
                        persistedEvents,
                        cancellationToken);
                }

                return ToApplicationResult(created);
            }
        }

        return Failure(PassportApplicationErrors.RideOccurrenceConcurrencyConflict());
    }

    private static List<RideOccurrence> BuildOccurrences(
        RideOccurrenceCreationRequest creationRequest,
        RideOccurrenceCreationPreparation preparation,
        IReadOnlyList<RideOccurrenceCreationItem> items,
        IReadOnlyList<long> positions,
        DateTime nowUtc)
    {
        Visit visit = CreateVisitContext(creationRequest, preparation, nowUtc);
        List<RideOccurrence> occurrences = new List<RideOccurrence>(items.Count);
        for (int index = 0; index < items.Count; index++)
        {
            RideOccurrenceCreationItem item = items[index];
            occurrences.Add(RideOccurrence.Create(
                RideOccurrenceId.New(),
                visit,
                item.ParkItemId.Trim(),
                positions[index],
                new OccurrenceMoment(item.LocalTime, item.IsApproximate),
                item.Status,
                RideLogSource.Manual,
                preparation.HistoricalConsistencies[index],
                null,
                item.PrivateNote,
                nowUtc));
        }

        return occurrences;
    }

    private static RideOccurrenceCreationPreparation CreatePreparation(
        Visit visit,
        IReadOnlyList<RideOccurrenceCreationItem> items,
        IReadOnlyDictionary<string, VisitTarget> targets)
    {
        return new RideOccurrenceCreationPreparation(
            visit.ParkId,
            visit.Date,
            visit.TimeZoneId,
            visit.ServiceDayConvention,
            items.Select(item =>
                RideOccurrenceHistoricalConsistencyEvaluator.Evaluate(
                    visit.Date,
                    targets[item.ParkItemId.Trim()].OpeningDate,
                    targets[item.ParkItemId.Trim()].ClosingDate))
                .ToArray());
    }

    private static Visit CreateVisitContext(
        RideOccurrenceCreationRequest request,
        RideOccurrenceCreationPreparation preparation,
        DateTime nowUtc)
    {
        return Visit.Create(
            request.VisitId,
            request.UserId,
            preparation.ParkId,
            preparation.VisitDate,
            preparation.TimeZoneId,
            preparation.ServiceDayConvention,
            null,
            null,
            nowUtc);
    }

    private static IReadOnlyList<RideOccurrenceCreationItem>? Expand(
        IReadOnlyCollection<RideOccurrenceCreationItem?>? items)
    {
        if (items is null || items.Count is < 1 or > 100)
        {
            return null;
        }

        List<RideOccurrenceCreationItem> expanded = new List<RideOccurrenceCreationItem>();
        foreach (RideOccurrenceCreationItem? item in items)
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

    private static string? NormalizePrivateNote(string? value)
    {
        string normalized = value?.Trim() ?? string.Empty;
        return normalized.Length == 0 ? null : normalized;
    }

    private static ApplicationResult<CreateRideOccurrencesResult> ToApplicationResult(
        IdempotentRideOccurrenceCreationResult result)
    {
        if (result.Status == IdempotentRideOccurrenceCreationStatus.Conflict)
        {
            return Failure(PassportApplicationErrors.RideOccurrenceIdempotencyConflict());
        }

        if (result.Status == IdempotentRideOccurrenceCreationStatus.ConcurrencyConflict)
        {
            return Failure(PassportApplicationErrors.RideOccurrenceConcurrencyConflict());
        }

        CreateRideOccurrencesResult value = new CreateRideOccurrencesResult(
            result.Occurrences.Select(static occurrence =>
                PassportRideOccurrenceResultFactory.Create(occurrence)).ToArray(),
            result.Status == IdempotentRideOccurrenceCreationStatus.Replayed,
            result.WasNormalized);
        return ApplicationResult<CreateRideOccurrencesResult>.Success(value);
    }

    private static ApplicationResult<CreateRideOccurrencesResult> Failure(
        ApplicationError error)
    {
        return ApplicationResult<CreateRideOccurrencesResult>.Failure(error);
    }
}
