using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.FactualEvents.Models;
using AmusementPark.Application.Features.FactualEvents.Ports;
using AmusementPark.Core.Domain.FactualEvents;

namespace AmusementPark.Application.Features.FactualEvents.Services;

public sealed class FactualChangeEventAdministrationService
{
    private readonly IFactualChangeEventRepository repository;
    private readonly IFactualChangeEventDistributionStateReader distributionStateReader;
    private readonly TimeProvider timeProvider;

    public FactualChangeEventAdministrationService(
        IFactualChangeEventRepository repository,
        IFactualChangeEventDistributionStateReader distributionStateReader,
        TimeProvider? timeProvider = null)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.distributionStateReader = distributionStateReader
            ?? throw new ArgumentNullException(nameof(distributionStateReader));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public Task<ApplicationResult> VerifyAsync(
        string eventId,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        return this.ChangeStatusAsync(
            eventId,
            expectedVersion,
            static (factualEvent, timestamp) => factualEvent.Verify(timestamp),
            cancellationToken);
    }

    public Task<ApplicationResult> PublishAsync(
        string eventId,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        return this.ChangeStatusAsync(
            eventId,
            expectedVersion,
            static (factualEvent, timestamp) => factualEvent.Publish(timestamp),
            cancellationToken);
    }

    public async Task<ApplicationResult> CorrectAsync(
        string eventId,
        long expectedVersion,
        string supersedingEventId,
        CancellationToken cancellationToken)
    {
        if (!FactualChangeEventId.TryParse(eventId, out FactualChangeEventId parsedEventId)
            || !FactualChangeEventId.TryParse(supersedingEventId, out FactualChangeEventId parsedSupersedingEventId)
            || expectedVersion < 1)
        {
            return ApplicationResult.Failure(FactualEventAdministrationErrors.InvalidMutation());
        }

        FactualChangeEvent? factualEvent = await this.repository.GetAsync(parsedEventId, cancellationToken);
        FactualChangeEvent? supersedingEvent = await this.repository.GetAsync(
            parsedSupersedingEventId,
            cancellationToken);
        if (factualEvent is null || supersedingEvent is null)
        {
            return ApplicationResult.Failure(FactualEventAdministrationErrors.NotFound());
        }

        if (factualEvent.Version != expectedVersion)
        {
            return ApplicationResult.Failure(FactualEventAdministrationErrors.Conflict());
        }

        if (!await this.distributionStateReader.IsInitialDistributionCompletedAsync(
                factualEvent.Id.Value,
                cancellationToken))
        {
            return ApplicationResult.Failure(FactualEventAdministrationErrors.DistributionPending());
        }

        if (supersedingEvent.Status != FactualChangeStatus.Published
            || supersedingEvent.Target != factualEvent.Target
            || !string.Equals(
                supersedingEvent.DeduplicationKey,
                factualEvent.DeduplicationKey,
                StringComparison.Ordinal)
            || supersedingEvent.Revision <= factualEvent.Revision)
        {
            return ApplicationResult.Failure(FactualEventAdministrationErrors.InvalidTransition());
        }

        return await this.ChangeStatusAsync(
            factualEvent,
            expectedVersion,
            (current, timestamp) => current.Correct(supersedingEvent.Id, timestamp),
            cancellationToken);
    }

    public Task<ApplicationResult> RetractAsync(
        string eventId,
        long expectedVersion,
        string reasonCode,
        CancellationToken cancellationToken)
    {
        string normalizedReasonCode = reasonCode?.Trim() ?? string.Empty;
        if (normalizedReasonCode.Length == 0
            || !FactualChangeRetractionReasonCodes.IsSupported(normalizedReasonCode))
        {
            return Task.FromResult(
                ApplicationResult.Failure(FactualEventAdministrationErrors.InvalidMutation()));
        }

        return this.ChangeStatusAsync(
            eventId,
            expectedVersion,
            (factualEvent, timestamp) => factualEvent.Retract(normalizedReasonCode, timestamp),
            cancellationToken,
            requireCompletedDistribution: true);
    }

    private async Task<ApplicationResult> ChangeStatusAsync(
        string eventId,
        long expectedVersion,
        Action<FactualChangeEvent, DateTime> transition,
        CancellationToken cancellationToken,
        bool requireCompletedDistribution = false)
    {
        if (!FactualChangeEventId.TryParse(eventId, out FactualChangeEventId parsedEventId)
            || expectedVersion < 1)
        {
            return ApplicationResult.Failure(FactualEventAdministrationErrors.InvalidMutation());
        }

        FactualChangeEvent? factualEvent = await this.repository.GetAsync(
            parsedEventId,
            cancellationToken);
        if (factualEvent is null)
        {
            return ApplicationResult.Failure(FactualEventAdministrationErrors.NotFound());
        }

        if (factualEvent.Version != expectedVersion)
        {
            return ApplicationResult.Failure(FactualEventAdministrationErrors.Conflict());
        }

        if (requireCompletedDistribution
            && !await this.distributionStateReader.IsInitialDistributionCompletedAsync(
                factualEvent.Id.Value,
                cancellationToken))
        {
            return ApplicationResult.Failure(FactualEventAdministrationErrors.DistributionPending());
        }

        return await this.ChangeStatusAsync(
            factualEvent,
            expectedVersion,
            transition,
            cancellationToken);
    }

    private async Task<ApplicationResult> ChangeStatusAsync(
        FactualChangeEvent factualEvent,
        long expectedVersion,
        Action<FactualChangeEvent, DateTime> transition,
        CancellationToken cancellationToken)
    {
        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        DateTime mutationAtUtc = nowUtc < factualEvent.UpdatedAtUtc
            ? factualEvent.UpdatedAtUtc
            : nowUtc;
        try
        {
            transition(factualEvent, mutationAtUtc);
        }
        catch (FactualEventValidationException)
        {
            return ApplicationResult.Failure(FactualEventAdministrationErrors.InvalidTransition());
        }

        FactualChangeEventMutationOutcome outcome = await this.repository.ReplaceAsync(
            factualEvent,
            expectedVersion,
            cancellationToken);
        return outcome == FactualChangeEventMutationOutcome.Success
            ? ApplicationResult.Success()
            : ApplicationResult.Failure(FactualEventAdministrationErrors.Conflict());
    }
}
