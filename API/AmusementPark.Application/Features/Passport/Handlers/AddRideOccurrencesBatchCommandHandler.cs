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
                NormalizePrivateNote(item.PrivateNote))).ToArray());
        IdempotentRideOccurrenceCreationResult? existing =
            await this.occurrenceRepository.ResolveExistingBatchCreationAsync(
                creationRequest,
                operationId,
                cancellationToken);
        if (existing is not null)
        {
            return ToApplicationResult(existing);
        }

        Visit? visit = await this.visitRepository.GetOwnedAsync(
            visitId,
            userId,
            cancellationToken);
        if (visit is null)
        {
            return Failure(PassportApplicationErrors.VisitNotFound());
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

        return await this.CreateWithOrderRetryAsync(
            visit,
            expanded,
            targets,
            operationId,
            cancellationToken);
    }

    private async Task<ApplicationResult<CreateRideOccurrencesResult>> CreateWithOrderRetryAsync(
        Visit visit,
        IReadOnlyList<RideOccurrenceCreationItem> items,
        IReadOnlyDictionary<string, VisitTarget> targets,
        string operationId,
        CancellationToken cancellationToken)
    {
        const int maximumAttempts = 3;
        for (int attempt = 0; attempt < maximumAttempts; attempt++)
        {
            long? currentMaximum = await this.occurrenceRepository.GetLastSortPositionAsync(
                visit.Id,
                visit.UserId,
                cancellationToken);
            IReadOnlyList<long> positions;
            try
            {
                positions = RideOccurrenceOrderPlanner.AllocateAppend(
                    currentMaximum,
                    items.Count);
            }
            catch (OverflowException)
            {
                return Failure(PassportApplicationErrors.RideOccurrenceConcurrencyConflict());
            }

            List<RideOccurrence> occurrences;
            try
            {
                occurrences = BuildOccurrences(
                    visit,
                    items,
                    targets,
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

            IdempotentRideOccurrenceCreationResult created =
                await this.occurrenceRepository.CreateBatchIdempotentAsync(
                    occurrences,
                    currentMaximum,
                    operationId,
                    cancellationToken);
            if (created.Status != IdempotentRideOccurrenceCreationStatus.ConcurrencyConflict)
            {
                return ToApplicationResult(created);
            }
        }

        return Failure(PassportApplicationErrors.RideOccurrenceConcurrencyConflict());
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
            HistoricalConsistency consistency =
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
                consistency,
                null,
                item.PrivateNote,
                nowUtc));
        }

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
            result.Occurrences.Select(PassportRideOccurrenceResultFactory.Create).ToArray(),
            result.Status == IdempotentRideOccurrenceCreationStatus.Replayed);
        return ApplicationResult<CreateRideOccurrencesResult>.Success(value);
    }

    private static ApplicationResult<CreateRideOccurrencesResult> Failure(
        ApplicationError error)
    {
        return ApplicationResult<CreateRideOccurrencesResult>.Failure(error);
    }
}
